"""Inventory the client's slave.xml placements against the server's hand-written slave_spawns.json.

usage: python -I -X utf8 slaves_inv.py <world>... | --all [--json rows.json]

AAEMU_CLIENT_WORLDS must point at the client's unpacked game/worlds folder; --all takes every world folder in
it. rows.json maps each inspected world to its placements and is the input of slaves_gen.py.
"""
import json, math, re, sqlite3, sys, glob, os, collections

# A hand-written row of the same slave this close (metres) to a placement already spawns it. Anything further
# off is another object: 5 m used to hide three Blueglass cannons that stood 3-5 m from their official spots.
COVERED_RADIUS = 0.5

GAME = os.environ.get("AAEMU_CLIENT_WORLDS")
DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "AAEmu.Game", "Data")
REPO = os.path.join(DATA, "Worlds")
DB = os.environ.get("AAEMU_SERVER_DB", os.path.join(DATA, "compact.sqlite3")).replace("\\", "/")

args = sys.argv[1:]
out = None
if "--json" in args:
    at = args.index("--json")
    if at + 1 >= len(args):
        sys.exit(__doc__)
    out = args[at + 1]
    del args[at:at + 2]
every = args == ["--all"]
if not GAME or not args or (not every and any(a.startswith("-") for a in args)):
    sys.exit(__doc__)
worlds = sorted(w for w in os.listdir(GAME) if os.path.isfile(f"{GAME}/{w}/world.xml")) if every else args

db = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
slaves = {r[0]: r[1:] for r in db.execute(
    "select id, name, slave_kind_id, mountable, faction_id, respawn, model_id from slaves")}
kinds = dict(db.execute("select id, name from enum_slave_kind")) if db.execute(
    "select count(*) from sqlite_master where name='enum_slave_kind'").fetchone()[0] else {}

def load_existing(path):
    if not os.path.exists(path):
        return []
    text = open(path, encoding="utf-8").read()
    text = re.sub(r"//[^\r\n]*", "", text)
    return json.loads(text)

def inventory(world):
    wx = open(f"{GAME}/{world}/world.xml", encoding="utf-8", errors="replace").read()
    origins = {}
    for m in re.finditer(r'<Zone\b[^>]*>', wx):
        tag = m.group(0)
        zid = re.search(r'\bid="(\d+)"', tag)
        ox = re.search(r'originX="(-?\d+)"', tag)
        oy = re.search(r'originY="(-?\d+)"', tag)
        nm = re.search(r'name="([^"]*)"', tag)
        if zid and ox and oy:
            origins[int(zid.group(1))] = (int(ox.group(1)), int(oy.group(1)), nm.group(1) if nm else "")

    existing = load_existing(f"{REPO}/{world}/slave_spawns.json")
    rows = []
    for path in sorted(glob.glob(f"{GAME}/{world}/level_design/zone/*/editor/slave.xml")):
        zone = int(path.replace("\\", "/").split("/")[-3])
        ox, oy, zname = origins.get(zone, (None, None, "?"))
        for m in re.finditer(r'<Object\b[^>]*Type="Slave"[^>]*/?>', open(path, encoding="utf-8", errors="replace").read()):
            tag = m.group(0)
            pos = [float(v) for v in re.search(r'Pos="([^"]+)"', tag).group(1).split(",")]
            rm = re.search(r'Rotate="([^"]+)"', tag)
            rot = [float(v) for v in rm.group(1).split(",")] if rm else [1.0, 0.0, 0.0, 0.0]
            st = int(re.search(r'SlaveType="(\d+)"', tag).group(1))
            name = re.search(r'Name="([^"]*)"', tag)
            layer = re.search(r'Layer="([^"]*)"', tag)
            if ox is None:
                print(f"!! {world}: zone {zone} has no origin in world.xml", file=sys.stderr)
                continue
            x, y, z = ox * 1024 + pos[0], oy * 1024 + pos[1], pos[2]
            yaw = math.degrees(2 * math.atan2(rot[3], rot[0]))
            yaw = (yaw + 180) % 360 - 180
            covered = any(e["UnitId"] == st and math.hypot(e["Position"]["X"] - x, e["Position"]["Y"] - y) < COVERED_RADIUS
                          for e in existing)
            s = slaves.get(st)
            rows.append(dict(zone=zone, zname=zname, slave=st, name=s[0] if s else None, kind=s[1] if s else None,
                             mountable=s[2] if s else None, faction=s[3] if s else None, respawn=s[4] if s else None,
                             x=round(x, 1), y=round(y, 1), z=round(z, 1), yaw=round(yaw, 1), covered=covered,
                             layer=layer.group(1) if layer else "", objname=name.group(1) if name else ""))

    if rows or not every:
        by = collections.defaultdict(list)
        for r in rows:
            by[(r["slave"])].append(r)
        print(f"{world}: {len(rows)} placements, {sum(r['covered'] for r in rows)} already spawned, existing file rows {len(existing)}")
        for st, rs in sorted(by.items()):
            r = rs[0]
            zones = sorted({x["zone"] for x in rs})
            print(f"slave {st:5} kind {r['kind']} ({kinds.get(r['kind'], '')}) mount={r['mountable']} fac={r['faction']} respawn={r['respawn']} "
                  f"n={len(rs)} covered={sum(x['covered'] for x in rs)} zones={zones} layers={sorted({x['layer'] for x in rs})} name={r['name']}")
    return rows

result = {world: inventory(world) for world in worlds}
if out:
    json.dump(result, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
