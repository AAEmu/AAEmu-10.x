import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# remnant initial buffs / recognition
for t in ("npc_initial_buffs", "npc_passbuffs", "np_buffs"):
    try:
        cols = [r[1] for r in c.execute(f"pragma table_info({t})")]
        print(t, cols)
    except Exception as e:
        print("no", t)
# find tables with buff and npc
for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%buff%' AND name LIKE '%npc%'"):
    print("tbl", r[0])
for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND (name LIKE 'npc_%buff%' OR name LIKE '%initial_buff%')"):
    print("tbl2", r[0])
# force_target on remnant
print("remnant", dict(c.execute("SELECT id,name,force_target_me_on_attack,aggression,faction_id FROM npcs WHERE id=17990").fetchone()))
# tags 3272
print("tag3272", dict(c.execute("SELECT * FROM tags WHERE id=3272").fetchone()))
print("tag345", dict(c.execute("SELECT * FROM tags WHERE id=345").fetchone()))
print("tag2822", dict(c.execute("SELECT * FROM tags WHERE id=2822").fetchone()))
print("tag361", dict(c.execute("SELECT * FROM tags WHERE id=361").fetchone()))
print("tag3576", dict(c.execute("SELECT * FROM tags WHERE id=3576").fetchone()))
print("tag1662", dict(c.execute("SELECT * FROM tags WHERE id=1662").fetchone()))
# wave spawn effects use_summoner for tower 72 related
# buff 21093 triggers spawn
for r in c.execute(
    """
    SELECT bt.id, e.actual_type, e.actual_id, s.spawner_id, s.use_summoner_aggro_target, s.life_time
    FROM buff_triggers bt
    JOIN effects e ON e.id=bt.effect_id
    LEFT JOIN npc_spawner_spawn_effects s ON e.actual_type='NpcSpawnerSpawnEffect' AND s.id=e.actual_id
    WHERE bt.buff_id=21093
    """
):
    print("21093", dict(r))
