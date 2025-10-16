import argparse
import json
import pathlib
import re
import sys

# ---------- helpers ----------
def read_text(p: pathlib.Path) -> str:
    try:
        t = p.read_text(encoding="utf-8")
        return t.replace("\r\n", "\n").replace("\r", "\n")
    except Exception as e:
        print(f"Read error {p}: {e}", file=sys.stderr); sys.exit(1)

def write_text(p: pathlib.Path, text: str):
    try:
        p.write_text(text, encoding="utf-8")
    except Exception as e:
        print(f"Write error {p}: {e}", file=sys.stderr); sys.exit(1)

def guess_meta_path(p: pathlib.Path) -> pathlib.Path:
    p = p.expanduser().resolve()
    if p.suffix == ".meta" and p.exists(): return p
    cand = p.with_suffix(p.suffix + ".meta")
    if cand.exists(): return cand
    cand2 = pathlib.Path(str(p) + ".meta")
    if cand2.exists(): return cand2
    print(f"Error: Could not find .meta for {p}", file=sys.stderr); sys.exit(1)

# Top-level TextureImporter block
TI_RE = re.compile(r"(?ms)^TextureImporter:\n.*\Z")

# Generic “two-space top-level” block extractor:
#   e.g.  "  spriteSheet:\n  ..." until the next "  <key>:" or end
def make_block_re(key: str) -> re.Pattern:
    return re.compile(rf"(?ms)^  {re.escape(key)}:\n.*?(?=^  [A-Za-z_]+\:|\Z)")

SPRITESHEET_RE = make_block_re("spriteSheet")
NAMEFILEID_RE = make_block_re("nameFileIdTable")
SPRITE_CUSTOM_META_RE = make_block_re("spriteCustomMetadata")

# Name line inside spriteSheet sprites
NAME_LINE_RE = re.compile(r"(^\s*-\s*name:\s*)([^\n]+)", re.MULTILINE)

# nameFileIdTable entries like "  nameFileIdTable:\n    Foo_1: 123\n    Bar: -1\n"
TABLE_PAIR_RE = re.compile(r"^(\s{4})(.+?):\s*(-?\d+)\s*$", re.MULTILINE)

GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.IGNORECASE | re.MULTILINE)

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

def default_rename_func(add_prefix="", replace_from="", replace_to="", name_map=None):
    name_map = name_map or {}
    def f(name: str) -> str:
        if name in name_map:
            return name_map[name]
        if replace_from:
            if name.startswith(replace_from):
                name = replace_to + name[len(replace_from):]
        if add_prefix:
            name = add_prefix + name
        return name
    return f

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

def main():
    ap = argparse.ArgumentParser(
        description="Splice Unity PNG .meta sprite slicing from a SOURCE into a DESTINATION, preserving dest GUID."
    )
    ap.add_argument("--src", required=True, help="Source PNG (or .meta) to copy sprite slicing FROM")
    ap.add_argument("--dst", required=True, help="Destination PNG (or .meta) to write sprite slicing TO")
    # Renaming options (applied to names in spriteSheet + nameFileIdTable)
    ap.add_argument("--replace-prefix", nargs=2, metavar=("OLD","NEW"),
                    help="Replace leading name prefix OLD with NEW (e.g., 'GrassRuleTiles_' 'YellowDebugPath_').")
    ap.add_argument("--add-prefix", default="", help="Add this prefix to every sprite name (applied after replace-prefix).")
    ap.add_argument("--name-map", help="JSON file mapping exact oldName -> newName (applied before prefix logic).")
    args = ap.parse_args()

    src_meta = guess_meta_path(pathlib.Path(args.src))
    dst_meta = guess_meta_path(pathlib.Path(args.dst))

    src_text = read_text(src_meta)
    dst_text = read_text(dst_meta)

    # Keep destination GUID
    mg = GUID_RE.search(dst_text)
    if not mg:
        print("Error: destination .meta missing guid.", file=sys.stderr); sys.exit(1)
    dst_guid = mg.group(1)

    # Pull blocks from source
    ss = SPRITESHEET_RE.search(src_text)
    nft = NAMEFILEID_RE.search(src_text)
    scm = SPRITE_CUSTOM_META_RE.search(src_text)  # optional

    if not ss and not nft:
        print("Error: source .meta has no spriteSheet or nameFileIdTable to copy.", file=sys.stderr); sys.exit(1)

    src_ss_block = ss.group(0) if ss else None
    src_nft_block = nft.group(0) if nft else None
    src_scm_block = scm.group(0) if scm else None

    # Build rename function
    name_map = {}
    if args.name_map:
        try:
            name_map = json.loads(pathlib.Path(args.name_map).read_text(encoding="utf-8"))
        except Exception as e:
            print(f"Failed to read --name-map: {e}", file=sys.stderr); sys.exit(1)

    rp_old, rp_new = ("", "")
    if args.replace_prefix:
        rp_old, rp_new = args.replace_prefix

    rename_func = default_rename_func(add_prefix=args.add_prefix,
                                      replace_from=rp_old, replace_to=rp_new,
                                      name_map=name_map)

    # Apply renames
    if src_ss_block:
        src_ss_block = apply_name_mapping_to_spriteSheet(src_ss_block, rename_func)
    if src_nft_block:
        src_nft_block = apply_name_mapping_to_nameFileIdTable(src_nft_block, rename_func)

    # Replace/insert into destination
    out = dst_text
    if src_ss_block:
        out = replace_or_insert_block(out, src_ss_block, "spriteSheet")
    if src_scm_block:
        out = replace_or_insert_block(out, src_scm_block, "spriteCustomMetadata")  # harmless if dest Unity version ignores it
    if src_nft_block:
        out = replace_or_insert_block(out, src_nft_block, "nameFileIdTable")

    # Restore destination guid (in case any block swap touched it—not expected, but safe)
    out = re.sub(GUID_RE, f"guid: {dst_guid}", out, count=1)

    write_text(dst_meta, out)

    # Quick stats for sanity
    def count_names(block: str | None) -> int:
        if not block: return 0
        return len(NAME_LINE_RE.findall(block))
    print("Splice complete.")
    print(f"  Source: {src_meta}")
    print(f"  Target: {dst_meta}  (GUID preserved: {dst_guid})")
    print(f"  spriteSheet sprites: {count_names(src_ss_block)}")
    print(f"  nameFileIdTable entries: {len(TABLE_PAIR_RE.findall(src_nft_block or ''))}")

if __name__ == "__main__":
    main()
