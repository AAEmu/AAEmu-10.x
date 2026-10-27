import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
print("npc_spawners 16913", c.execute("select * from npc_spawners where id=16913").fetchone())
tabs = [
    r[0]
    for r in c.execute(
        "select name from sqlite_master where type='table' and name like 'npc_spawner%'"
    )
]
print("tabs", tabs)
for t in tabs:
    cols = [x[1] for x in c.execute(f"pragma table_info({t})")]
    print(t, cols)
    for col in cols:
        if not col.endswith("_id"):
            continue
        try:
            rows = c.execute(
                f"select * from {t} where {col} in (15156,15157,16913) limit 8"
            ).fetchall()
        except Exception:
            continue
        if rows:
            print(" ", col, "->", rows)
c.close()
