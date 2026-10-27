import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
print("devote cols", [r[1] for r in c.execute("pragma table_info(doodad_func_devotes)")])
for i in (135, 136, 137, 138):
    print(i, c.execute("select * from doodad_func_devotes where id=?", (i,)).fetchone())

print("skillhit cols", [r[1] for r in c.execute("pragma table_info(doodad_func_skill_hits)")])
for i in (3668, 3669, 3670, 3671):
    print(i, c.execute("select * from doodad_func_skill_hits where id=?", (i,)).fetchone())

for sid in (26120, 26119, 31648, 22023, 29833, 31935):
    print("skill", sid, c.execute("select id from skills where id=?", (sid,)).fetchone())
    loc = c.execute(
        "select en_us, ko from localized_texts where tbl_name='skills' and tbl_column_name='name' and idx=?",
        (sid,),
    ).fetchone()
    print("  loc", loc)

# how many json spawns of each
import json, pathlib
p = pathlib.Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json")
data = json.loads(p.read_text(encoding="utf-8"))
from collections import Counter
cnt = Counter()
pos = []
for e in data:
    uid = e.get("UnitId") or e.get("unitId")
    if uid in (6605, 8033, 8034, 10520, 10521, 10522, 10523):
        cnt[uid] += 1
        pos.append(e)
print("json counts", dict(cnt))
for e in pos:
    p_ = e.get("Position") or {}
    print(e.get("Id"), e.get("UnitId"), p_.get("X"), p_.get("Y"), p_.get("Z"))
