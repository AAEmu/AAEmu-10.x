import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("=== all np_skills for 17996")
print(json.dumps([dict(r) for r in c.execute("SELECT * FROM np_skills WHERE owner_id=17996")], indent=2))
print("=== skill_use_conditions 5")
try:
    print([dict(r) for r in c.execute("SELECT * FROM skill_use_conditions WHERE id=5")])
except Exception as e:
    print(e)
    print([r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE name LIKE '%skill_use%'")])
# buff 21393 / 21411 on 17996 - what do they do?
print("=== buffs 21393 21411")
print(json.dumps([dict(r) for r in c.execute("SELECT id,name,duration,tick FROM buffs WHERE id IN (21393,21411)")], indent=2, default=str))
print("=== buff_triggers for 21393 21411")
print(json.dumps([dict(r) for r in c.execute("SELECT * FROM buff_triggers WHERE buff_id IN (21393,21411)")], indent=2, default=str)[:3000])
# effects linked - skill use
print("=== special effects type SkillUse for these buffs")
# find effect ids from triggers then special
trs=list(c.execute("SELECT effect_id FROM buff_triggers WHERE buff_id IN (21393,21411)"))
print("trigger effect ids", [t[0] for t in trs])
for eid in [t[0] for t in trs]:
    e=dict(c.execute("SELECT id,actual_type,actual_id FROM effects WHERE id=?", (eid,)).fetchone() or {})
    print("effect", e)
    if e.get("actual_type") and "Special" in e["actual_type"]:
        se=dict(c.execute("SELECT * FROM special_effects WHERE id=?", (e["actual_id"],)).fetchone() or {})
        print("  special", se)
# how does next_phase get set for SkillHit - doodad_funcs next_phase column?
print("=== doodad_funcs cols", [r[1] for r in c.execute("PRAGMA table_info(doodad_funcs)")])
print("=== 35999 funcs full")
print(json.dumps([dict(r) for r in c.execute("SELECT * FROM doodad_funcs WHERE doodad_func_group_id=35999")], indent=2, default=str))
print("=== phase funcs 35999")
print([r[1] for r in c.execute("PRAGMA table_info(doodad_phase_funcs)")])
print(json.dumps([dict(r) for r in c.execute("SELECT * FROM doodad_phase_funcs WHERE doodad_func_group_id=35999")], indent=2, default=str)[:2000])
