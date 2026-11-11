"""Write slave_spawns_official.json for every world in rows.json (made by slaves_inv.py). Placements the
hand-written slave_spawns.json already covers are skipped. A file that ends up with no placements is removed,
so a stale copy cannot spawn its rows a second time. Boats go to slave_spawns_official_boats.json and only
with --boats; without it the boats files are left alone.

usage: python -I -X utf8 slaves_gen.py <rows.json> [--boats]
"""
import json, os, sqlite3, sys

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "AAEmu.Game", "Data")
REPO = os.path.join(DATA, "Worlds")
DB = os.environ.get("AAEMU_SERVER_DB", os.path.join(DATA, "compact.sqlite3")).replace("\\", "/")
BOAT_KINDS = {1, 2, 3, 4, 9, 10}  # big/small sailing ship, speedboat, boat, fishboat, merchant ship

args = sys.argv[1:]
boats = "--boats" in args
if boats:
    args.remove("--boats")
if len(args) != 1 or args[0].startswith("-"):
    sys.exit(__doc__)
worlds = json.load(open(args[0], encoding="utf-8"))
db = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
en = dict(db.execute("select idx, en_us from localized_texts where tbl_name='slaves' and tbl_column_name='name'"))
kinds = dict(db.execute("select id, slave_kind_id from slaves"))

def entry(r):
    name = en.get(r["slave"]) or r["name"]
    return (
        "    {\n"
        f'        "UnitId": {r["slave"]}, // {name}; client level_design zone {r["zone"]} ({r["zname"]}) slave.xml {r["objname"]}\n'
        '        "Position": {\n'
        f'            "X": {r["x"]},\n'
        f'            "Y": {r["y"]},\n'
        f'            "Z": {r["z"]},\n'
        f'            "Yaw": {r["yaw"]}\n'
        "        }\n"
        "    }"
    )

def write(path, items, what):
    if not items:
        # The World loads every slave_spawns*.json, so a file left over from an earlier run would spawn again.
        if os.path.exists(path):
            os.remove(path)
            print(f"{path}: no entries, removed")
        return
    header = (f"// Generated from the client's level_design/zone/*/editor/slave.xml ({what}).\r\n"
              "// Placements already in the hand-written slave_spawns.json are left out. Yaw = 2*atan2(Rotate[3], Rotate[0]).\r\n")
    body = "[\n" + ",\n".join(entry(r) for r in items) + "\n]\n"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w", encoding="utf-8", newline="").write(header + body.replace("\n", "\r\n"))
    print(f"{path}: {len(items)} entries")

for world, rows in sorted(worlds.items()):
    todo = []
    for r in sorted((r for r in rows if not r["covered"]), key=lambda r: (r["zone"], r["slave"], r["x"], r["y"])):
        if r["slave"] in kinds:
            todo.append(r)
        else:
            print(f"!! {world}: zone {r['zone']} {r['objname']} names slave {r['slave']}, which the server DB lacks; skipped",
                  file=sys.stderr)
    folder = os.path.normpath(os.path.join(REPO, world))
    write(os.path.join(folder, "slave_spawns_official.json"), [r for r in todo if kinds[r["slave"]] not in BOAT_KINDS],
          "cannons, siege weapons, wagons, turrets")
    if boats:
        write(os.path.join(folder, "slave_spawns_official_boats.json"), [r for r in todo if kinds[r["slave"]] in BOAT_KINDS],
              "boats; delete this file to spawn none")
