#!/usr/bin/env python3
# make_ruletile_asset.py
import argparse
import pathlib
import re
import sys
import uuid
import json
from collections import Counter

# ------------ regex helpers ------------
GUID32 = r"[0-9a-fA-F]{32}"
GUID_RE = re.compile(r"(?m)^guid:\s*(" + GUID32 + r")\s*$")
SPRITE_REF_RE = re.compile(r"\{fileID:\s*(-?\d+),\s*guid:\s*(" + GUID32 + r"),\s*type:\s*3\}")
NAME_LINE_RE = re.compile(r"(^\s*-\s*name:\s*)([^\n]+)", re.MULTILINE)

# ------------ io utils ------------
def read_text(p: pathlib.Path) -> str:
    try:
        return p.read_text(encoding="utf-8")
    except Exception as e:
        print(f"Read error {p}: {e}", file=sys.stderr); sys.exit(1)

def write_text(p: pathlib.Path, text: str):
    p.parent.mkdir(parents=True, exist_ok=True)
    try:
        p.write_text(text, encoding="utf-8")
    except Exception as e:
        print(f"Write error {p}: {e}", file=sys.stderr); sys.exit(1)

def is_valid_guid(g: str) -> bool:
    return bool(re.fullmatch(GUID32, g))

def guess_meta_path(p: pathlib.Path) -> pathlib.Path:
    p = p.expanduser().resolve()
    if p.suffix == ".meta" and p.exists():
        return p
    cand = p.with_suffix(p.suffix + ".meta")
    if cand.exists():
        return cand
    cand2 = pathlib.Path(str(p) + ".meta")
    if cand2.exists():
        return cand2
    print(f"Error: Could not find .meta for {p}", file=sys.stderr); sys.exit(1)

# ------------ meta copy (splice) ------------
_GUID_LINE_RE = re.compile(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")

def _rename_table_key_line(line: str, oldp: str, newp: str) -> str:
    # Matches: "    SomeName: 12345"
    m = re.match(r"(\s+)([^:\s][^:]*)(:)", line)
    if not m:
        return line
    key = m.group(2)
    if key.startswith(oldp):
        key = newp + key[len(oldp):]
        return f"{m.group(1)}{key}{m.group(3)}{line[m.end():]}"
    return line

def copy_meta_preserve_guid(src_meta_text: str, dst_meta_text: str,
                            replace_prefix: tuple[str, str] | None = None) -> str:
    # Keep destination GUID
    m = _GUID_LINE_RE.search(dst_meta_text)
    dst_guid = m.group(1) if m else None

    # Start from source meta wholesale
    out = src_meta_text

    # Optional rename of sprite names + nameFileIdTable keys
    if replace_prefix:
        oldp, newp = replace_prefix
        # rename 'name: …' entries inside spriteSheet
        out = re.sub(r"(^\s*name:\s*)([^\n]+)",
                     lambda mm: f"{mm.group(1)}{mm.group(2).replace(oldp, newp, 1)}",
                     out, flags=re.MULTILINE)
        # rename keys in nameFileIdTable
        out = "".join(_rename_table_key_line(ln, oldp, newp) for ln in out.splitlines(True))

    # Restore destination GUID to avoid GUID collisions
    if dst_guid:
        if _GUID_LINE_RE.search(out):
            out = _GUID_LINE_RE.sub(f"guid: {dst_guid}", out, count=1)
        else:
            out = f"guid: {dst_guid}\n" + out

    return out

# ------------ parse name/id maps from .meta ------------
def parse_namefileid_table(meta_text: str):
    """
    Returns (name->id, id->name). Prefer nameFileIdTable; fallback to spriteSheet list.
    """
    name_to_id, id_to_name = {}, {}

    # 1) nameFileIdTable (most robust for multi-sprite sheets)
    nft_match = re.search(r"(?ms)^(\s*)nameFileIdTable:\s*\n(.*?)(?=^\S|\Z)", meta_text)
    if nft_match:
        block = nft_match.group(2)
        # lines like "  SomeName: 12345"
        for line in block.splitlines():
            m = re.match(r"\s*([^:\s][^:]*?)\s*:\s*(-?\d+)\s*$", line)
            if m:
                name = m.group(1).strip()
                fid = int(m.group(2))
                name_to_id[name] = fid
                id_to_name[fid] = name
        if name_to_id:
            return name_to_id, id_to_name

    # 2) Fallback: look inside spriteSheet sprites for "- name: X ... internalID: Y"
    sheet_match = re.search(r"(?ms)^\s*spriteSheet:\s*\n(.*?)(?=^\S|\Z)", meta_text)
    if sheet_match:
        block = sheet_match.group(1)
        item_re = re.compile(r"-\s*serializedVersion:\s*\d+\s*\n(?:\s+.+\n)*?\s*name:\s*([^\n]+)\n(?:\s+.+\n)*?\s*internalID:\s*(-?\d+)",
                             re.MULTILINE)
        for m in item_re.finditer(block):
            name = m.group(1).strip()
            fid = int(m.group(2))
            name_to_id[name] = fid
            id_to_name[fid] = name
        if name_to_id:
            return name_to_id, id_to_name

    # 3) Last-ditch: broad scan
    for m in re.finditer(r"name:\s*([^\n]+)\n\s*internalID:\s*(-?\d+)", meta_text):
        name = m.group(1).strip()
        fid = int(m.group(2))
        name_to_id[name] = fid
        id_to_name[fid] = name

    return name_to_id, id_to_name

# ------------ RuleTile YAML helpers ------------
def infer_sprite_guid_from_yaml(yaml_text: str) -> str:
    guids = []
    for line in yaml_text.splitlines():
        if "m_Script:" in line:  # skip the script ref
            continue
        m = re.search(r"guid:\s*(" + GUID32 + r")", line, re.IGNORECASE)
        if m:
            guids.append(m.group(1).lower())
    if not guids:
        return ""
    return Counter(guids).most_common(1)[0][0]

def replace_asset_name(yaml_text: str, new_name: str) -> str:
    return re.sub(r"(?m)^(\s*m_Name:\s*).*$", rf"\1{new_name}", yaml_text)

def remap_sprite_refs(yaml_text: str, old_guid: str, new_guid: str,
                      old_id_to_name: dict, new_name_to_id: dict) -> str:
    def _sub(match):
        old_file_id = int(match.group(1))
        match_guid = match.group(2).lower()
        if match_guid != old_guid:
            return match.group(0)
        name = old_id_to_name.get(old_file_id)
        if name is None:
            # keep fileID but swap guid
            return f"{{fileID: {old_file_id}, guid: {new_guid}, type: 3}}"
        new_id = new_name_to_id.get(name)
        if new_id is None:
            return f"{{fileID: {old_file_id}, guid: {new_guid}, type: 3}}"
        return f"{{fileID: {new_id}, guid: {new_guid}, type: 3}}"
    return SPRITE_REF_RE.sub(_sub, yaml_text)

# ------------ main ------------
def main():
    ap = argparse.ArgumentParser(
        description="Create/retarget a Unity RuleTile .asset and (optionally) splice PNG slicing by copying a .meta (keeping GUID)."
    )
    # Core tile inputs
    ap.add_argument("--template", required=True, help="Path to source RuleTile .asset template")
    ap.add_argument("--out", required=True, help="Directory OR full .asset path. If ends with .asset, treated as a file path.")
    ap.add_argument("--name", required=True, help="New m_Name for the RuleTile")

    # NEW sheet: guid OR path (prefer path so we can parse .meta)
    gnew = ap.add_mutually_exclusive_group(required=True)
    gnew.add_argument("--sprite-guid", help="NEW sprite sheet GUID (32 hex)")
    gnew.add_argument("--sprite-path", help="Path to NEW sprite .png (or its .meta) for GUID and table")

    # OLD sheet path (to get name->fileID table) or explicit guid
    ap.add_argument("--old-sprite-path", help="Path to OLD sprite .png (or its .meta) for name->fileID map")
    ap.add_argument("--old-sprite-guid", help="OLD sprite sheet GUID in the template (32 hex)")

    # Output asset.meta GUID (optional)
    ap.add_argument("--asset-guid", help="GUID for new .asset.meta (32 hex). If omitted, random is generated.")

    # Splice step: copy entire src PNG.meta onto NEW PNG.meta (keeping NEW GUID)
    ap.add_argument("--splice-from", help="Path to PNG (or .meta) to copy its entire .meta into NEW sprite's .meta (GUID preserved)")
    ap.add_argument("--splice-replace-prefix", nargs=2, metavar=("OLD_PREFIX", "NEW_PREFIX"),
                    help="Optional: rename sprite names and nameFileIdTable keys (OLD_PREFIX -> NEW_PREFIX) during splice")

    args = ap.parse_args()

    # --- read template
    template_path = pathlib.Path(args.template).expanduser().resolve()
    yaml_text = read_text(template_path)

    # --- determine old sprite GUID
    if args.old_sprite_guid:
        old_guid = args.old_sprite_guid.lower()
        if not is_valid_guid(old_guid):
            print("Error: --old-sprite-guid must be 32 hex.", file=sys.stderr); sys.exit(1)
    else:
        old_guid = infer_sprite_guid_from_yaml(yaml_text)
        if not old_guid:
            print("Error: Could not infer old sprite GUID from template YAML; pass --old-sprite-guid.", file=sys.stderr); sys.exit(1)

    # --- NEW sprite GUID + meta path
    new_meta = None
    if args.sprite_guid:
        new_guid = args.sprite_guid.lower()
        if not is_valid_guid(new_guid):
            print("Error: --sprite-guid must be 32 hex.", file=sys.stderr); sys.exit(1)
        # if path also provided (rare), we can parse table; otherwise table may be missing
        if args.sprite_path:
            new_meta = guess_meta_path(pathlib.Path(args.sprite_path))
    else:
        sprite_path = pathlib.Path(args.sprite_path).expanduser().resolve()
        new_meta = guess_meta_path(sprite_path)
        new_guid_m = GUID_RE.search(read_text(new_meta))
        if not new_guid_m:
            print(f"Error: Could not read GUID from {new_meta}", file=sys.stderr); sys.exit(1)
        new_guid = new_guid_m.group(1).lower()

    # --- optional splice (copy meta from another PNG, keeping NEW GUID)
    if args.splice_from:
        if not args.sprite_path:
            print("Error: --splice-from requires --sprite-path", file=sys.stderr); sys.exit(1)
        src_meta = guess_meta_path(pathlib.Path(args.splice_from))
        dst_meta = guess_meta_path(pathlib.Path(args.sprite_path))
        sp = tuple(args.splice_replace_prefix) if args.splice_replace_prefix else None
        new_dst_text = copy_meta_preserve_guid(read_text(src_meta), read_text(dst_meta), replace_prefix=sp)
        write_text(dst_meta, new_dst_text)
        print(f"Meta copied (GUID preserved) → {dst_meta}")
        print("If Unity shows stale data, right-click the target PNG → Reimport.")
        # refresh in-memory new_meta text after splice
        new_meta = dst_meta

    # --- OLD and NEW sprite name/id maps
    old_name_to_id, old_id_to_name = {}, {}
    new_name_to_id, new_id_to_name = {}, {}

    if args.old_sprite_path:
        old_meta = guess_meta_path(pathlib.Path(args.old_sprite_path))
        old_name_to_id, old_id_to_name = parse_namefileid_table(read_text(old_meta))
    else:
        print("Warning: --old-sprite-path not supplied; fileIDs will not be remapped by name.", file=sys.stderr)

    if new_meta:
        new_name_to_id, new_id_to_name = parse_namefileid_table(read_text(new_meta))

    # --- transform RuleTile yaml
    out_yaml = replace_asset_name(yaml_text, args.name)

    if old_id_to_name and new_name_to_id:
        out_yaml = remap_sprite_refs(out_yaml, old_guid, new_guid, old_id_to_name, new_name_to_id)
        print(f"Remapped fileIDs by sprite name where possible. OLD sprites parsed: {len(old_id_to_name)}; NEW parsed: {len(new_name_to_id)}")
    else:
        # fallback: only swap GUIDs (avoid m_Script lines)
        lines = []
        for line in out_yaml.splitlines():
            if "m_Script:" in line:
                lines.append(line)
            else:
                lines.append(re.sub(rf"guid:\s*{old_guid}", f"guid: {new_guid}", line, flags=re.IGNORECASE))
        out_yaml = "\n".join(lines) + ("\n" if not out_yaml.endswith("\n") else "")
        print("FileID remap skipped (name tables missing); performed GUID swap only.")

    # --- output paths
    out_path = pathlib.Path(args.out).expanduser().resolve()
    safe_filename = "".join(c for c in args.name if c not in "\\/:*?\"<>|").strip() or "NewAsset"
    if out_path.suffix.lower() == ".asset":
        asset_path = out_path
    else:
        asset_path = out_path / f"{safe_filename}.asset"
    meta_out_path = pathlib.Path(str(asset_path) + ".meta")

    # --- asset meta guid
    asset_guid = (args.asset_guid.lower() if args.asset_guid else uuid.uuid4().hex)
    if not is_valid_guid(asset_guid):
        print("Error: --asset-guid must be 32 hex.", file=sys.stderr); sys.exit(1)

    # --- write files
    write_text(asset_path, out_yaml)
    write_text(meta_out_path, (
        "fileFormatVersion: 2\n"
        f"guid: {asset_guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ))

    print("\nDone.")
    print(f"  RuleTile : {asset_path}")
    print(f"  Meta     : {meta_out_path}")
    print(f"  Old GUID : {old_guid}")
    print(f"  New GUID : {new_guid}")
    if old_id_to_name and new_name_to_id:
        print("  FileIDs  : remapped by sprite name.")
    else:
        print("  FileIDs  : NOT remapped (name tables missing).")
    if args.splice_from:
        print("  Splice   : copied entire PNG .meta (GUID preserved). Reimport in Unity if needed.")

if __name__ == "__main__":
    main()
