import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("prog cols", [r[1] for r in c.execute("PRAGMA table_info(tower_def_progs)")])
for r in c.execute("SELECT * FROM tower_def_progs WHERE tower_def_id=72 ORDER BY id"):
    print(dict(r))
print("--- spawn targets")
for r in c.execute("SELECT * FROM tower_def_prog_spawn_targets WHERE tower_def_prog_id IN (SELECT id FROM tower_def_progs WHERE tower_def_id=72) ORDER BY tower_def_prog_id, id"):
    print(dict(r))
print("--- kill targets")
for r in c.execute("SELECT * FROM tower_def_prog_kill_targets WHERE tower_def_prog_id IN (SELECT id FROM tower_def_progs WHERE tower_def_id=72) ORDER BY tower_def_prog_id, id"):
    print(dict(r))
# wave npc spawners - search whalesong / 고래
print("--- wave-ish spawners named")
for r in c.execute("SELECT id,name,activation_state FROM npc_spawners WHERE name LIKE '%고래%' OR comment LIKE '%고래%' OR name LIKE '%Whalesong%' OR name LIKE '%타워 디펜스%' LIMIT 80"):
    print(dict(r))
