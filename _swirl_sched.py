import sqlite3

c = sqlite3.connect("file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
skills = [27841, 27842, 27813, 47196, 27672, 27910, 31928]

print("=== doodad_funcs.func_skill_id ===")
for s in skills:
    for r in c.execute(
        "select id, doodad_func_group_id, actual_func_type, next_phase, func_skill_id "
        "from doodad_funcs where func_skill_id=?",
        (s,),
    ):
        print(dict(r))

print("=== tagged_skills ===")
for s in skills:
    for r in c.execute("select * from tagged_skills where skill_id=?", (s,)):
        print(dict(r))

print("=== schedules by name keywords ===")
q = (
    "select id, name, st_year, st_month, st_day, ed_year, ed_month, ed_day, "
    "start_time, end_time from game_schedules where "
    "lower(name) like '%golden%' or lower(name) like '%abyssal%' or "
    "lower(name) like '%battle%' or lower(name) like '%haven%' or "
    "lower(name) like '%kraken%' or name like '%심연%' or name like '%황금%' or "
    "name like '%이프니르%' or name like '%심연의%' limit 50"
)
for r in c.execute(q):
    print(dict(r))

print("=== tower_defs keywords ===")
for r in c.execute(
    "select id, name, tod, target_npc_spawner_id from tower_defs where "
    "lower(name) like '%golden%' or lower(name) like '%abyssal%' or "
    "lower(name) like '%haven%' or name like '%심연%' or name like '%황금%' limit 20"
):
    print(dict(r))

# SpecialEffect type 33 parent skill for 27910
print("=== who casts 27910 via SpecialEffect 18318 ===")
for r in c.execute(
    "select sf.skill_id, e.id as effect_id from skill_effects sf "
    "join effects e on e.id=sf.effect_id "
    "where e.actual_type='SpecialEffect' and e.actual_id=18318"
):
    print(dict(r))
    sid = r["skill_id"]
    n = c.execute(
        "select en_us from localized_texts where tbl_name='skills' and "
        "tbl_column_name='name' and idx=?",
        (sid,),
    ).fetchone()
    print("  parent name", n[0] if n else None)
