# -*- coding: utf-8 -*-
import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row

print("npc_spawners cols", [r[1] for r in c.execute("pragma table_info(npc_spawners)")])
for t in c.execute(
    "select name from sqlite_master where type='table' and name like '%spawner%'"
):
    print("table", t[0])

for sid in (20009, 20010, 20011, 20239, 20235):
    row = c.execute("select * from npc_spawners where id=?", (sid,)).fetchone()
    print("spawner", sid, dict(row) if row else None)

# common member tables
for tname in ("npc_spawner_npcs", "npc_spawners_npcs", "spawner_npcs"):
    try:
        cols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
        print(tname, cols)
        for sid in (20009, 20010, 20011):
            print(
                " ",
                sid,
                [dict(r) for r in c.execute(f"select * from {tname} where npc_spawner_id=?", (sid,))],
            )
    except Exception as e:
        print(tname, e)
