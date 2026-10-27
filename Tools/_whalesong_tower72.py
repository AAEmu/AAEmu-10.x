import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("tower_def_progs cols", [r[1] for r in c.execute("PRAGMA table_info(tower_def_progs)")])
print("progs 72", json.dumps([dict(r) for r in c.execute("SELECT * FROM tower_def_progs WHERE tower_def_id=72 ORDER BY id")], indent=2, default=str)[:4000])
print("prog spawn targets cols", [r[1] for r in c.execute("PRAGMA table_info(tower_def_prog_spawn_targets)")])
print("=== spawn targets for 72 progs")
progs=[r[0] for r in c.execute("SELECT id FROM tower_def_progs WHERE tower_def_id=72")]
for r in c.execute(f"SELECT * FROM tower_def_prog_spawn_targets WHERE tower_def_prog_id IN ({','.join(map(str,progs))}) ORDER BY id"):
    print(dict(r))
# npc spawners 20011, and who is 17996/17789
print("=== npc_spawners catalog for 20010/20011 and residual")
for r in c.execute("SELECT id, npc_id, unit_id FROM npc_spawners WHERE id IN (20010,20011) OR npc_id IN (17789,17996,17990,18008) LIMIT 40"):
    print(dict(r))
# maybe npc_spawner_npcs
print("tables", [r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE name LIKE '%spawner%' AND type='table'")])
