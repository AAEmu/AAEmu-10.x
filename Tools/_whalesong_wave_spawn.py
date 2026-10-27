import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# special effects for skill use / strip
for sid in (29238,29247,29812):
    r=c.execute("SELECT * FROM special_effects WHERE id=?", (sid,)).fetchone()
    print("special", dict(r) if r else sid)
# buff 21093 duration
b=c.execute("SELECT id,name,duration,tick FROM buffs WHERE id=21093").fetchone()
print("buff21093", dict(b) if b else None)
# sample of NpcSpawnerSpawnEffect from 21093 - which spawners
print("npc_spawner_spawn_effects cols", [r[1] for r in c.execute("PRAGMA table_info(npc_spawner_spawn_effects)")])
# effects actual_id is the npc_spawner_spawn_effects id
for actual in (5250,5251,5320,5383,5423,5529,5541):
    r=c.execute("SELECT * FROM npc_spawner_spawn_effects WHERE id=?", (actual,)).fetchone()
    print("spawn_eff", dict(r) if r else actual)
    if r:
        sp=c.execute("SELECT id,name FROM npc_spawners WHERE id=?", (r["spawner_id"],)).fetchone()
        mem=c.execute("SELECT member_id FROM npc_spawner_npcs WHERE npc_spawner_id=?", (r["spawner_id"],)).fetchall()
        print("  spawner", dict(sp) if sp else None, "members", [m[0] for m in mem])
# skill_use_condition 5 = OnSpawn
print("enum skill_use", [dict(r) for r in c.execute("SELECT * FROM enum_skill_use_conditions")])
