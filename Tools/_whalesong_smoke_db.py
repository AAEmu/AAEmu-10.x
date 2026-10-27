import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
def dump(title, sql, args=()):
    print("===", title)
    try:
        rows = [dict(r) for r in c.execute(sql, args)]
        print(json.dumps(rows, indent=2, default=str)[:4000])
    except Exception as e:
        print("ERR", e)
dump("np_skills 17996", "SELECT * FROM np_skills WHERE npc_id=17996")
dump("npc_initial_buffs 17996", "SELECT * FROM npc_initial_buffs WHERE npc_id=17996")
dump("skills", "SELECT id, name, cooldown_time, target_area_count FROM skills WHERE id IN (36659,36661)")
dump("skill_effects", """SELECT ske.skill_id, e.actual_type, e.actual_id, ske.weight, ske.chance
FROM skill_effects ske JOIN effects e ON e.id=ske.effect_id
WHERE ske.skill_id IN (36659,36661) ORDER BY ske.skill_id, ske.id""")
dump("12312", """SELECT dfg.id AS group_id, df.actual_func_type, df.actual_func_id,
dsh.skill_id AS hit_skill, dsh.next_phase AS hit_next
FROM doodad_func_groups dfg
JOIN doodad_funcs df ON df.doodad_func_group_id=dfg.id
LEFT JOIN doodad_func_skill_hits dsh ON dsh.id=df.actual_func_id AND df.actual_func_type LIKE '%SkillHit%'
WHERE dfg.doodad_almighty_id=12312 ORDER BY dfg.id, df.id""")
dump("buff_triggers 21093", "SELECT * FROM buff_triggers WHERE buff_id=21093")
dump("npcs", "SELECT id, name FROM npcs WHERE id IN (17789,17996)")
