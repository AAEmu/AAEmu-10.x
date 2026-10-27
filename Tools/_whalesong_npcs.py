import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# npcs of interest + skills
for nid in (17788,17789,17790,17992,17996,17916,17856,17858,17859,18042,18044):
    n=c.execute("SELECT id,name,npc_ai_param_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print("NPC", dict(n) if n else nid)
    for s in c.execute("SELECT * FROM np_skills WHERE owner_id=? OR npc_id=?", (nid,nid)):
        print("  np_skill", dict(s))
# schema np_skills
print("np_skills cols", [r[1] for r in c.execute("PRAGMA table_info(np_skills)")])
for s in c.execute("SELECT * FROM np_skills WHERE owner_id IN (17788,17789,17790,17992,17996,17916,17856,17858,17859) OR npc_id IN (17788,17789,17790,17992,17996,17916,17856,17858,17859)"):
    print(dict(s))
# initial buffs
print("--- initial buffs")
for b in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id IN (17788,17789,17790,17992,17996,17916)"):
    print(dict(b))
# skills 36659, 36329
for sid in (36659,36329,21408):
    sk=c.execute("SELECT id,name,ability_id FROM skills WHERE id=?", (sid,)).fetchone()
    print("skill", dict(sk) if sk else sid)
# related towers targeting same conflict / whalesong
print("--- towers with target near 20011 or name whalesong")
for r in c.execute("SELECT id,name,target_npc_spawner_id,first_wave_after,force_end_time FROM tower_defs WHERE target_npc_spawner_id BETWEEN 20000 AND 20350 OR id BETWEEN 70 AND 85"):
    print(dict(r))
