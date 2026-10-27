import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
print("cols", [r[1] for r in c.execute("pragma table_info(item_spawn_doodads)")])
for tid in (3484, 10712):
    print(tid, c.execute("select * from item_spawn_doodads where doodad_id=?", (tid,)).fetchall()[:5])
    print("count", c.execute("select count(*) from item_spawn_doodads where doodad_id=?", (tid,)).fetchone())
print("sample", c.execute("select * from item_spawn_doodads limit 5").fetchall())
rows = c.execute(
    "select id, group_id from doodad_almighties where model like ?",
    ("%ndeco_watercase%",),
).fetchall()
print("watercase", rows)
