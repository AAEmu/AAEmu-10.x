import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for sid in (29238, 29812):
    for e in c.execute("SELECT id,actual_type,actual_id FROM effects WHERE actual_id=? AND actual_type LIKE '%Special%'", (sid,)):
        eid=e[0]
        print("special", sid, "effect", eid)
        print("  buff_triggers", [dict(r) for r in c.execute("SELECT id,buff_id,event_id,effect_id FROM buff_triggers WHERE effect_id=?", (eid,))])
        print("  buff names", [dict(r) for r in c.execute("SELECT id,name FROM buffs WHERE id IN (SELECT buff_id FROM buff_triggers WHERE effect_id=?)", (eid,))])
# tower 72 first wave spawners
print("tower 72", [dict(r) for r in c.execute("SELECT id,name,first_wave,target_npc_spawner_id FROM tower_defs WHERE id=72")])
print("progs", [dict(r) for r in c.execute("SELECT * FROM tower_def_progs WHERE tower_def_id=72")])
