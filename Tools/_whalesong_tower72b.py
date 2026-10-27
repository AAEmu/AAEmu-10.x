import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("npc_spawners cols", [r[1] for r in c.execute("PRAGMA table_info(npc_spawners)")])
for sid in (20009,20010,20011,20235,20239):
    row=c.execute("SELECT * FROM npc_spawners WHERE id=?", (sid,)).fetchone()
    print("spawner", sid, dict(row) if row else None)
# link tables
for t in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%npc_spawner%'"):
    print("table", t[0], [r[1] for r in c.execute(f"PRAGMA table_info({t[0]})")])
# npc_spawner_npcs or similar
for tname in ["npc_spawner_npcs","npc_spawner_members","spawner_npcs"]:
    try:
        print(tname, [dict(r) for r in c.execute(f"SELECT * FROM {tname} WHERE npc_spawner_id IN (20009,20010,20011,20235,20239) LIMIT 30")])
    except Exception as e:
        print(tname, e)
# search owner
print("=== members for those spawners")
for r in c.execute("SELECT * FROM npc_spawner_npcs WHERE owner_id IN (20009,20010,20011,20235,20239) OR npc_spawner_id IN (20009,20010,20011,20235,20239)"):
    print(dict(r))
