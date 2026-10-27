import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("special value1=36659", [dict(r) for r in c.execute("SELECT * FROM special_effects WHERE value1=36659")])
print("special value1=36661", [dict(r) for r in c.execute("SELECT * FROM special_effects WHERE value1=36661")])
# enum special effect types 33 34 41
print("enum se", [dict(r) for r in c.execute("SELECT * FROM enum_special_effects WHERE id IN (33,34,35,37,41)")])
# buff_triggers that fire skill 36659 via effect chain
for se in c.execute("SELECT id FROM special_effects WHERE value1=36659"):
    sid=se[0]
    for e in c.execute("SELECT id FROM effects WHERE actual_type LIKE '%Special%' AND actual_id=?", (sid,)):
        eid=e[0]
        print("effect", eid, "from special", sid)
        print("buff_triggers", [dict(r) for r in c.execute("SELECT * FROM buff_triggers WHERE effect_id=?", (eid,))])
# SkillUseScheduleRules?
