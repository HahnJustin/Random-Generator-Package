import argparse
import json
import pathlib
import re
import sys
import uuid
from collections import Counter

# =========================
# ===== Regex helpers =====
# =========================

GUID32 = r"[0-9a-f]{32}"
GUID_RE = re.compile(r"^guid:\s*(" + GUID32 + r")\s*$", re.IGNORECASE | re.MULTILINE)
# Sprite reference inside RuleTile YAML: {fileID: <int>, guid: <guid>, type: 3}
SPRITE_REF_RE = re.compile(r"\{fileID:\s*(-?\d+),\s*guid:\s*(" + GUID32 + r"),\s*type:\s*3\}", re.IGNORECASE)

# Top-level TextureImporter block and sub-blocks
def make_block_re(key: str) -> re.Pattern:
    # Two-space top-level key under TextureImporter:
    return re.compile(rf"(?ms)^  {re.escape(key)}:\n.*?(?=^  [A-Za-z_]+\:|\Z)")

TI_RE = re.compile(r"(?ms)^TextureImporter:\n.*\Z")
SPRITESHEET_RE = make_block_re("spriteSheet")
NAMEFILEID_RE = make_block_re("nameFileIdTable")
SPRITE_CUSTOM_META_RE = make_block_re("spriteCustomMetadata")

# name: lines inside spriteSheet.sprites list
NAME_LINE_RE = re.compile(r"(^\s*-\s*name:\s*)([^\n]+)", re.MULTILINE)
# nameFileIdTable entries (indented pairs)
TABLE_PAIR_RE = re.compile(r"^(\s{4})(.+?):\s*(-?\d+)\s*$", re.MULTILINE)

# =====================
# ===== IO utils  =====
# =====================

def read_text(p: pathlib.Path) -> str:
    try:
        t = p.read_text(encoding="utf-8")
        return t.replace("\r\n", "\n").replace("\r", "\n")
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
    if p.suffix == ".meta" and p.exists(): return p
    cand = p.with_suffix(p.suffix + ".meta")
    if cand.exists(): return cand
    cand2 = pathlib.Path(str(p) + ".meta")
    if cand2.exists(): return cand2
    print(f"Error: Could not find .meta for {p}", file=sys.stderr); sys.exit(1)

# =====================================
# ===== Sprite name/ID table parse =====
# =====================================

def parse_nameFileIdTable(meta_text: str):
    """Returns (name->id, id->name) from nameFileIdTable if present."""
    m = NAMEFILEID_RE.search(meta_text)
    if not m:
        return {}, {}
    table_block = m.group(0)
    name_to_id, id_to_name = {}, {}
    for mm in TABLE_PAIR_RE.finditer(table_block):
        name = mm.group(2).strip()
        fid = int(mm.group(3))
        name_to_id[name] = fid
        id_to_name[fid] = name
    return name_to_id, id_to_name

def parse_spriteSheet(meta_text: str):
    """Returns (name->id, id->name) by scanning spriteSheet.sprites list (fallback)."""
    m = SPRITESHEET_RE.search(meta_text)
    if not m:
        return {}, {}
    block = m.group(0)
    # Find each sprite item and its internalID
    item_re = re.compile(
        r"-\s*serializedVersion:\s*2\s*\n(?:\s+.+\n)*?\s+name:\s*([^\n]+)\n(?:\s+.+\n)*?\s+internalID:\s*(-?\d+)",
        re.MULTILINE
    )
    name_to_id, id_to_name = {}, {}
    for mm in item_re.finditer(block):
        name = mm.group(1).strip()
        fid = int(mm.group(2))
        name_to_id[name] = fid
        id_to_name[fid] = name
    return name_to_id, id_to_name

# =================================
# ===== Splice/rename helpers  =====
# =================================

def replace_or_insert_block(dst_text: str, new_block: str, key: str) -> str:
    re_block = make_block_re(key)
    if re_block.search(dst_text):
        return re_block.sub(new_block.rstrip("\n") + "\n", dst_text, count=1)
    # Insert right after "TextureImporter:\n"
    m = re.search(r"(?m)^TextureImporter:\n", dst_text)
    if not m:
        print("Error: destination .meta missing TextureImporter root.", file=sys.stderr); sys.exit(1)
    insert_pos = m.end()
    return dst_text[:insert_pos] + new_block.rstrip("\n") + "\n" + dst_text[insert_pos:]

def default_rename_func(add_prefix="", replace_from="", replace_to="", name_map=None):
    name_map = name_map or {}
    def f(name: str) -> str:
        if name in name_map:
            return name_map[name]
        if replace_from and name.startswith(replace_from):
            name = replace_to + name[len(replace_from):]
        if add_prefix:
            name = add_prefix + name
        return name
    return f

def apply_name_mapping_to_spriteSheet(block: str, rename_func) -> str:
    def sub(m):
        head, name = m.group(1), m.group(2).strip()
        return f"{head}{rename_func(name)}"
    return NAME_LINE_RE.sub(sub, block)

def apply_name_mapping_to_nameFileIdTable(block: str, rename_func) -> str:
    def sub(m):
        indent, name, fid = m.group(1), m.group(2).strip(), m.group(3)
        return f"{indent}{rename_func(name)}: {fid}"
    return TABLE_PAIR_RE.sub(sub, block)

def splice_png_meta(src_meta_text: str, dst_meta_text: str, rename_func):
    # Extract from source
    ss = SPRITESHEET_RE.search(src_meta_text)
    nft = NAMEFILEID_RE.search(src_meta_text)
    scm = SPRITE_CUSTOM_META_RE.search(src_meta_text)  # optional

    if not ss and not nft:
        print("Error: source .meta has no spriteSheet or nameFileIdTable to copy.", file=sys.stderr); sys.exit(1)

    ss_block = ss.group(0) if ss else None
    nft_block = nft.group(0) if nft else None
    scm_block = scm.group(0) if scm else None

    # Apply renames
    if ss_block:
        ss_block = apply_name_mapping_to_spriteSheet(ss_block, rename_func)
    if nft_block:
        nft_block = apply_name_mapping_to_nameFileIdTable(nft_block, rename_func)

    # Preserve destination GUID
    mg = GUID_RE.search(dst_meta_text)
    if not mg:
        print("Error: destination .meta missing guid.", file=sys.stderr); sys.exit(1)
    dst_guid = mg.group(1)

    out = dst_meta_text
    if ss_block:
        out = replace_or_insert_block(out, ss_block, "spriteSheet")
    if scm_block:
        out = replace_or_insert_block(out, scm_block, "spriteCustomMetadata")
    if nft_block:
        out = replace_or_insert_block(out, nft_block, "nameFileIdTable")

    # Ensure GUID intact
    out = re.sub(GUID_RE, f"guid: {dst_guid}", out, count=1)

    return out

# =================================
# ===== RuleTile YAML helpers  =====
# =================================

def infer_sprite_guid_from_yaml(yaml_text: str) -> str:
    guids = []
    for line in yaml_text.splitlines():
        if "m_Script:" in line:  # avoid script reference
            continue
        m = re.search(GUID32, line, re.IGNORECASE)
        if m:
            guids.append(m.group(0).lower())
    if not guids:
        return ""
    # Most common GUID is typically the sprite sheet GUID referenced everywhere
    return Counter(guids).most_common(1)[0][0]

def replace_asset_name(yaml_text: str, new_name: str) -> str:
    return re.sub(r"(?m)^(\s*m_Name:\s*).*$", rf"\1{new_name}", yaml_text)

def remap_sprite_refs(yaml_text: str, old_guid: str, new_guid: str,
                      old_id_to_name: dict, new_name_to_id: dict,
                      transform_name_for_new):
    def _sub(match):
        old_file_id = int(match.group(1))
        match_guid = match.group(2).lower()
        if match_guid != old_guid:
            return match.group(0)
        old_name = old_id_to_name.get(old_file_id)
        if old_name is None:
            # No name available → just swap GUID, keep fileID
            return f"{{fileID: {old_file_id}, guid: {new_guid}, type: 3}}"
        # Apply same rename logic used during splice, so old_name maps to the new sheet
        new_lookup_name = transform_name_for_new(old_name)
        new_id = new_name_to_id.get(new_lookup_name)
        if new_id is None:
            # Name not found on new sheet → keep old fileID, only swap GUID
            return f"{{fileID: {old_file_id}, guid: {new_guid}, type: 3}}"
        return f"{{fileID: {new_id}, guid: {new_guid}, type: 3}}"
    return SPRITE_REF_RE.sub(_sub, yaml_text)

# ==================
# =====  Main  =====
# ==================

def main():
    ap = argparse.ArgumentParser(
        description="Create/retarget a Unity RuleTile .asset. (Optionally) splice sprite slicing into the target PNG's .meta, rename sprites, then remap RuleTile by name."
    )

    # Core tile args
    ap.add_argument("--template", required=True, help="Path to source RuleTile .asset template")
    ap.add_argument("--out", required=True, help="Directory OR full .asset path. If ends with .asset, treated as a file path.")
    ap.add_argument("--name", required=True, help="New m_Name for the RuleTile")

    # NEW sprite sheet: path required (to read .meta & GUID)
    gnew = ap.add_mutually_exclusive_group(required=True)
    gnew.add_argument("--sprite-guid", help="NEW sprite sheet GUID (32 hex)")
    gnew.add_argument("--sprite-path", help="Path to NEW sprite asset (.png/.asset) or its .meta")

    # OLD sprite sheet path for name->fileID mapping
    ap.add_argument("--old-sprite-path", required=True, help="Path to OLD sprite asset (.png/.asset) or its .meta")
    ap.add_argument("--old-sprite-guid", help="OLD sprite sheet GUID in the template (32 hex). If omitted, inferred from template YAML.")

    # Asset meta GUID
    ap.add_argument("--asset-guid", help="GUID for new .asset.meta (32 hex). If omitted, random is generated.")

    # ===== Optional splice step baked in =====
    ap.add_argument("--splice-from", help="Source PNG (or .meta) to copy sprite slicing FROM into the NEW sprite's .meta")
    ap.add_argument("--splice-replace-prefix", nargs=2, metavar=("OLD","NEW"),
                    help="Replace leading name prefix OLD with NEW (e.g., 'GrassRuleTiles_' 'YellowDebugPath_').")
    ap.add_argument("--splice-add-prefix", default="", help="Add this prefix to every sprite name (applied after replace-prefix).")
    ap.add_argument("--splice-name-map", help="JSON file mapping exact oldName -> newName (applied before prefix logic).")

    args = ap.parse_args()

    # ---- Load & prep template
    template_path = pathlib.Path(args.template).expanduser().resolve()
    yaml_text = read_text(template_path)

    # ---- Determine OLD guid
    if args.old_sprite_guid:
        old_guid = args.old_sprite_guid.lower()
        if not is_valid_guid(old_guid):
            print("Error: --old-sprite-guid must be 32 hex.", file=sys.stderr); sys.exit(1)
    else:
        old_guid = infer_sprite_guid_from_yaml(yaml_text)
        if not old_guid:
            print("Error: Could not infer old sprite GUID from template YAML; pass --old-sprite-guid.", file=sys.stderr); sys.exit(1)

    # ---- NEW guid + meta path
    new_meta = None
    if args.sprite_guid:
        new_guid = args.sprite_guid.lower()
        if not is_valid_guid(new_guid):
            print("Error: --sprite-guid must be 32 hex.", file=sys.stderr); sys.exit(1)
        if args.sprite_path:
            new_meta = guess_meta_path(pathlib.Path(args.sprite_path))
    else:
        sprite_path = pathlib.Path(args.sprite_path).expanduser().resolve()
        new_meta = guess_meta_path(sprite_path)
        m = GUID_RE.search(read_text(new_meta))
        if not m:
            print("Error: Could not read guid from NEW sprite .meta.", file=sys.stderr); sys.exit(1)
        new_guid = m.group(1).lower()

    # ---- Prepare rename function (used by splice AND name mapping)
    name_map = {}
    if args.splice_name_map:
        try:
            name_map = json.loads(pathlib.Path(args.splice_name_map).read_text(encoding="utf-8"))
        except Exception as e:
            print(f"Failed to read --splice-name-map: {e}", file=sys.stderr); sys.exit(1)

    rp_old, rp_new = ("", "")
    if args.splice_replace_prefix:
        rp_old, rp_new = args.splice_replace_prefix

    rename_func = default_rename_func(
        add_prefix=args.splice_add_prefix,
        replace_from=rp_old, replace_to=rp_new,
        name_map=name_map
    )

    # ---- Optional splice into NEW PNG's .meta
    if args.splice_from:
        src_meta = guess_meta_path(pathlib.Path(args.splice_from))
        if not new_meta:
            if not args.sprite_path:
                print("Error: --sprite-path is required when splicing.", file=sys.stderr); sys.exit(1)
            new_meta = guess_meta_path(pathlib.Path(args.sprite_path))
        src_text = read_text(src_meta)
        dst_text = read_text(new_meta)
        spliced = splice_png_meta(src_text, dst_text, rename_func)
        write_text(new_meta, spliced)
        print(f"Spliced slicing into: {new_meta}")
        # Use spliced text for parsing new name table
        new_meta_text = spliced
    else:
        if not new_meta:
            if not args.sprite_path:
                print("Error: Need --sprite-path to locate NEW .meta.", file=sys.stderr); sys.exit(1)
            new_meta = guess_meta_path(pathlib.Path(args.sprite_path))
        new_meta_text = read_text(new_meta)

    # ---- OLD and NEW name/id maps
    old_meta = guess_meta_path(pathlib.Path(args.old_sprite_path).expanduser().resolve())
    old_meta_text = read_text(old_meta)

    # Prefer nameFileIdTable; fall back to spriteSheet
    old_name_to_id, old_id_to_name = parse_nameFileIdTable(old_meta_text)
    if not old_name_to_id:
        old_name_to_id, old_id_to_name = parse_spriteSheet(old_meta_text)

    new_name_to_id, new_id_to_name = parse_nameFileIdTable(new_meta_text)
    if not new_name_to_id:
        new_name_to_id, new_id_to_name = parse_spriteSheet(new_meta_text)

    print(f"OLD sprites parsed: {len(old_name_to_id)}; NEW sprites parsed: {len(new_name_to_id)}")

    # ---- Replace asset name
    out_yaml = replace_asset_name(yaml_text, args.name)

    # ---- Remap sprite refs
    if old_id_to_name and new_name_to_id:
        transform_name_for_new = rename_func  # same transform used during splice
        out_yaml = remap_sprite_refs(out_yaml, old_guid, new_guid,
                                     old_id_to_name, new_name_to_id,
                                     transform_name_for_new)
        remap_note = "FileIDs remapped by sprite name (with rename rules)."
    else:
        # Fallback: swap guid where applicable (avoid m_Script lines)
        lines = []
        for line in out_yaml.splitlines():
            if "m_Script:" in line:
                lines.append(line)
            else:
                lines.append(re.sub(rf"guid:\s*{old_guid}", f"guid: {new_guid}", line))
        out_yaml = "\n".join(lines) + ("\n" if not out_yaml.endswith("\n") else "")
        remap_note = "FileIDs NOT remapped (name tables missing) — GUID swapped only."

    # ---- Output paths
    out_path = pathlib.Path(args.out).expanduser().resolve()
    safe_filename = "".join(c for c in args.name if c not in "\\/:*?\"<>|").strip() or "NewAsset"
    if out_path.suffix.lower() == ".asset":
        asset_path = out_path
    else:
        asset_path = out_path / f"{safe_filename}.asset"
    meta_out_path = pathlib.Path(str(asset_path) + ".meta")

    # ---- Asset meta guid
    asset_guid = (args.asset_guid.lower() if args.asset_guid else uuid.uuid4().hex)
    if not is_valid_guid(asset_guid):
        print("Error: --asset-guid must be 32 hex.", file=sys.stderr); sys.exit(1)

    # ---- Write files
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
    print(f"  RuleTile: {asset_path}")
    print(f"  Meta    : {meta_out_path}")
    print(f"  Old sheet GUID: {old_guid}")
    print(f"  New sheet GUID: {new_guid}")
    print(f"  {remap_note}")
    if args.splice_from:
        print("  PNG .meta slicing spliced (spriteSheet / nameFileIdTable / spriteCustomMetadata).")
        print("  If Unity shows stale data, right-click the target PNG → Reimport.")

if __name__ == "__main__":
    main()
