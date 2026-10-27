import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("doodad_funcs cols", [r[1] for r in c.execute("pragma table_info(doodad_funcs)")])
for r in c.execute(
    """
    SELECT f.id,f.doodad_func_group_id,f.actual_func_type,f.actual_func_id,f.next_phase
    FROM doodad_funcs f
    WHERE f.doodad_func_group_id IN (35999,36000,36001,36149)
    ORDER BY f.doodad_func_group_id,f.id
    """
):
    print("func", dict(r))
print("phase_funcs")
for r in c.execute(
    "SELECT * FROM doodad_phase_funcs WHERE doodad_func_group_id IN (35999,36000,36001,36149)"
):
    print("pf", dict(r))
print("skill_hit 4890", dict(c.execute("SELECT * FROM doodad_func_skill_hits WHERE id=4890").fetchone()))
for r in c.execute(
    """
    SELECT f.id,f.doodad_func_group_id,f.actual_func_type,f.actual_func_id,f.next_phase
    FROM doodad_funcs f
    WHERE f.actual_func_type='DoodadFuncSkillHit' AND f.actual_func_id=4890
    """
):
    print("attached", dict(r))
for r in c.execute(
    """
    SELECT f.id,f.doodad_func_group_id,f.actual_func_type,f.actual_func_id,f.next_phase,h.skill_id
    FROM doodad_funcs f
    LEFT JOIN doodad_func_skill_hits h
      ON f.actual_func_type='DoodadFuncSkillHit' AND h.id=f.actual_func_id
    WHERE f.doodad_func_group_id IN (35999,36000,36001,36149)
    """
):
    print("func+hit", dict(r))
# kind_id meanings
print("kinds", [dict(r) for r in c.execute("SELECT * FROM enum_doodad_func_group_kinds")])
