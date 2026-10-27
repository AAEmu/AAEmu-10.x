import json
import pathlib
import re
import sqlite3
from collections import Counter

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
root = pathlib.Path(r"G:\AAchina\client\game\worlds\main_world\level_design\cells")
g = set()
for f in root.glob("*/doodad.g"):
    for m in re.finditer(r"\btype\s+(\d+)", f.read_text(encoding="utf-8", errors="ignore")):
        g.add(int(m.group(1)))
print("g templates", len(g))

data = json.loads(
    pathlib.Path(
        r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
    ).read_text(encoding="utf-8")
)
j = Counter()
for e in data:
    uid = e.get("UnitId") or e.get("unitId")
    if uid:
        j[uid] += 1
print("json templates", len(j), "rows", sum(j.values()))
only_j = [t for t in j if t not in g]
print("only-json templates", len(only_j), "rows", sum(j[t] for t in only_j))

groups = Counter()
for t in only_j:
    row = c.execute("select group_id from doodad_almighties where id=?", (t,)).fetchone()
    gid = row[0] if row else -1
    groups[gid] += j[t]

print("only-json rows by group_id:")
for gid, n in groups.most_common(25):
    gname = (
        c.execute("select name from doodad_groups where id=?", (gid,)).fetchone()
        if gid >= 0
        else None
    )
    print(f"  {gid} {gname}: {n}")

sched = {r[0] for r in c.execute("select distinct doodad_id from game_schedule_doodads")}
only_j_sched = [t for t in only_j if t in sched]
print(
    "only-json also scheduled templates",
    len(only_j_sched),
    "rows",
    sum(j[t] for t in only_j_sched),
)

for want in (11, 13, 24, 25, 39, 43, 69, 2, 3, 4, 5):
    ids = []
    for t in only_j:
        row = c.execute("select group_id from doodad_almighties where id=?", (t,)).fetchone()
        if row and row[0] == want:
            ids.append(t)
    print(f"group {want} only-json tpl {len(ids)} rows {sum(j[t] for t in ids)}")
    if ids and want in (24, 11, 43, 13, 25):
        for t in ids[:10]:
            nm = c.execute(
                "select en_us from localized_texts where tbl_name='doodad_almighties' "
                "and tbl_column_name='name' and idx=?",
                (t,),
            ).fetchone()
            print("   ", t, j[t], nm)
