import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("enum_skill_use_condition", [dict(r) for r in c.execute("SELECT * FROM enum_skill_use_condition")])
print("kinds", [dict(r) for r in c.execute("SELECT * FROM enum_skill_use_condition_kinds")])
# 17789 skills
print("17789 np_skills", [dict(r) for r in c.execute("SELECT * FROM np_skills WHERE owner_id=17789")])
print("17789 initial buffs", [dict(r) for r in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id=17789")])
# Does 36659 have SkillHit path requiring AoE? check skill target type
print("36659 full", dict(c.execute("SELECT * FROM skills WHERE id=36659").fetchone()))
# skill_target / casting
cols=[r[1] for r in c.execute("PRAGMA table_info(skills)")]
interesting=[x for x in cols if any(k in x.lower() for k in ("target","area","cast","channel","first","aoe","effect"))]
print("interesting cols", interesting)
row=dict(c.execute("SELECT * FROM skills WHERE id=36659").fetchone())
print({k:row[k] for k in interesting if k in row})
