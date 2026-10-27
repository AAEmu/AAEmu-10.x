import json
import sqlite3
from pathlib import Path

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
print("group 72", c.execute("select id, name from doodad_groups where id=72").fetchone())
print("group 69", c.execute("select id, name from doodad_groups where id=69").fetchone())
for r in c.execute("select id, name from doodad_groups where id between 65 and 80"):
    print(" ", r)
print(
    "5290 fields",
    c.execute(
        "select id, group_id, system_doodad, place_area_kind_id, mgmt_spawn, client_doodad "
        "from doodad_almighties where id=5290"
    ).fetchone(),
)
print(
    "sched 5290",
    c.execute("select count(*) from game_schedule_doodads where doodad_id=5290").fetchone()[0],
)

# json: find UnitId 5290 near Marianople plaza (~11179, 12054)
p = Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json")
# also map-doodad-pr copy if present
for cand in [
    p,
    Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"),
]:
    if not cand.exists():
        print("missing json", cand)
        continue
    print("scanning", cand, "size", cand.stat().st_size)
    with cand.open(encoding="utf-8") as f:
        data = json.load(f)
    hits = [d for d in data if d.get("UnitId") == 5290]
    print("  UnitId 5290 count", len(hits))
    near = []
    for d in hits:
        pos = d.get("Position") or {}
        x, y = float(pos.get("X", 0)), float(pos.get("Y", 0))
        if abs(x - 11179) < 80 and abs(y - 12054) < 80:
            near.append((d.get("Id"), x, y, pos.get("Z")))
    print("  near plaza", near[:10], "total_near", len(near))

# would MapDoodadPlant plant this from .g? group 72 != 47
print("rules would plant group 72: YES (only skip unused 47)")

out = Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\_lookup_5290.out.txt")
out.write_text("done\n", encoding="utf-8")
