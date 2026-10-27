import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("np_skills cols", [r[1] for r in c.execute("PRAGMA table_info(np_skills)")])
for nid in (17788,17789,17790,17992,17996,17916,17856,17858,17859,18042):
    n=c.execute("SELECT id,name FROM npcs WHERE id=?", (nid,)).fetchone()
    print("NPC", dict(n) if n else nid)
    for s in c.execute("SELECT * FROM np_skills WHERE owner_id=?", (nid,)):
        print("  ", dict(s))
print("--- initial buffs")
print("cols", [r[1] for r in c.execute("PRAGMA table_info(npc_initial_buffs)")])
for b in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id IN (17788,17789,17790,17992,17996,17916)"):
    print(dict(b))
# buff 21093 triggers
print("--- buff 21093")
for t in ("buff_triggers","buff_tick_effects","buff_unit_modifiers"):
    try:
        cols=[r[1] for r in c.execute(f"PRAGMA table_info({t})")]
        print(t, cols)
        for r in c.execute(f"SELECT * FROM {t} WHERE buff_id=21093 OR owner_id=21093 LIMIT 20"):
            print(" ", dict(r))
    except Exception as e:
        print(t, e)
# related tower_defs 70-85
for r in c.execute("SELECT id,name,target_npc_spawner_id,first_wave_after FROM tower_defs WHERE id BETWEEN 68 AND 90"):
    print("tower", dict(r))
