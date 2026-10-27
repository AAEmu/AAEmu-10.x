import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("np_skills cols", [r[1] for r in c.execute("PRAGMA table_info(np_skills)")])
print("doodad_func_skill_hits cols", [r[1] for r in c.execute("PRAGMA table_info(doodad_func_skill_hits)")])
# find rows mentioning 17996
for table in ["np_skills","npc_mount_skills","tagged_npcs","npc_ai_params"]:
    cols=[r[1] for r in c.execute(f"PRAGMA table_info({table})")]
    print(table, cols[:12])
# search skill 36659/36661 owners
for sql,title in [
("SELECT * FROM np_skills WHERE skill_id IN (36659,36661) LIMIT 30","np_skills by skill"),
("SELECT * FROM buff_effects WHERE id IN (23219,23220)","buff_effects strip"),
("SELECT id,name FROM buffs WHERE id IN (SELECT buff_id FROM buff_effects WHERE id IN (23219,23220))","buff names"),
("SELECT * FROM special_effects WHERE id IN (63035,63587,63589,63590,63591,63592)","special for 21093"),
("SELECT id,name,actual_type,actual_id FROM effects WHERE id IN (63035,63587,63589)","effects 21093"),
]:
    print("===", title)
    try:
        print(json.dumps([dict(r) for r in c.execute(sql)], indent=2, default=str)[:2500])
    except Exception as e:
        print(e)
# 12312 skill hits
print("=== 12312 skill hits")
print(json.dumps([dict(r) for r in c.execute("""
SELECT dfg.id AS group_id, df.actual_func_type, df.actual_func_id, dsh.*
FROM doodad_func_groups dfg
JOIN doodad_funcs df ON df.doodad_func_group_id=dfg.id
LEFT JOIN doodad_func_skill_hits dsh ON dsh.id=df.actual_func_id AND df.actual_func_type LIKE '%SkillHit%'
WHERE dfg.doodad_almighty_id=12312 ORDER BY dfg.id, df.id
""")], indent=2, default=str)[:3500])
# OnSpawn skill for 17996 - check npc_skills alternative / unit_reqs / plots
print("=== npc skills alt")
for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%npc%skill%'"):
    print(r[0])
