import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for nid in (17772,17773,17779,17780,17990):
    n=c.execute("SELECT id,name,aggression,absolute_aggression,attack_start_range_scale,sight_range_scale,npc_tendency_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(n))
for bid in (21363,21378,21394,21393):
    b=c.execute("SELECT id,name,duration,buff_kind_id FROM buffs WHERE id=?", (bid,)).fetchone()
    print("buff", dict(b) if b else bid)
    for r in c.execute("SELECT id,event_id,effect_id FROM buff_triggers WHERE buff_id=? LIMIT 10", (bid,)):
        print("  trig", dict(r))
# buff modifiers / tags
print("--- buff tags")
for t in ("tagged_buffs","buff_modifiers","buff_unit_modifiers"):
    cols=[r[1] for r in c.execute(f"PRAGMA table_info({t})")]
    print(t, cols[:12])
for bid in (21363,21378):
    for r in c.execute("SELECT * FROM tagged_buffs WHERE buff_id=? OR tag_id=?", (bid,bid)):
        print("tagged", dict(r))
# NpcSpawnerSpawnEffect use_summoner_aggro for wave spawners from 21093
# already know UseSummonerAggroTarget=False on those effects
# check ai_file for wave
for nid in (17772,17779):
    n=c.execute("SELECT id,ai_file_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print("ai_file", dict(n))
    af=c.execute("SELECT * FROM ai_files WHERE id=?", (n["ai_file_id"],)).fetchone() if n else None
    print("  ", dict(af) if af else None)
