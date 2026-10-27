import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")

print("=== skill 36659 full targeting ===")
cols = [x[1] for x in c.execute("pragma table_info(skills)")]
row = dict(zip(cols, c.execute("select * from skills where id=36659").fetchone()))
for k in sorted(row):
    if row[k] not in (None, 0, "", "f") and k not in ("desc",):
        print(f"  {k}={row[k]}")

print("=== skill 36661 ===")
print(c.execute("select id,name,target_type_id,target_area_radius,target_area_count from skills where id=36661").fetchone())
for se in c.execute("select effect_id from skill_effects where skill_id=36661"):
    print(" se", se, c.execute("select * from effects where id=?", (se[0],)).fetchone())

print("=== who casts 36659 via OnSpawn / np_skill ===")
for t in c.execute(
    "select name from sqlite_master where type='table' and (name like '%npc%skill%' or name like '%np_skill%' or name like '%spawn%skill%')"
):
    print("table", t[0])

for tname in ("npc_initial_buffs", "np_skills", "npc_skills", "unit_scripts"):
    try:
        cols = [x[1] for x in c.execute(f"pragma table_info({tname})")]
        print(tname, cols[:15])
    except Exception as e:
        print(tname, e)

# skills that SkillHit 12312
print("=== doodad_func_skill_hits on 12312 ===")
for r in c.execute(
    """
    select df.doodad_func_group_id, df.next_phase, h.skill_id, s.name
    from doodad_funcs df
    join doodad_func_groups g on g.id=df.doodad_func_group_id
    join doodad_func_skill_hits h on h.id=df.actual_func_id
    join skills s on s.id=h.skill_id
    where g.doodad_almighty_id=12312 and df.actual_func_type='DoodadFuncSkillHit'
    """
):
    print(r)

print("=== npc 17996 / 17789 / 17790 ===")
for nid in (17996, 17789, 17790, 17990):
    print(nid, c.execute("select id,name from npcs where id=?", (nid,)).fetchone())

# buff 21093 on which npc?
print("=== buff 21093 applied how ===")
print(c.execute("select id,name,duration from buffs where id=21093").fetchone())
# npc_initial_buffs
try:
    for r in c.execute("select * from npc_initial_buffs where buff_id=21093"):
        print("initial", r)
except Exception as e:
    print(e)

# special skill use from spawn
print("=== special_effects SkillUse 36659 triggers ===")
for s in c.execute(
    "select id, special_effect_type_id, value1,value2,value3,value4 from special_effects where value1=36659 and special_effect_type_id=33"
):
    print(s)
    for e in c.execute(
        "select id from effects where actual_type='SpecialEffect' and actual_id=?", (s[0],)
    ):
        for bt in c.execute(
            "select id, buff_id, event_id from buff_triggers where effect_id=?", (e[0],)
        ):
            print("  bt", bt, c.execute("select name from buffs where id=?", (bt[1],)).fetchone())
        for se in c.execute("select skill_id from skill_effects where effect_id=?", (e[0],)):
            print("  skill_eff", se)
