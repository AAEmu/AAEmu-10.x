# Trace who casts Activate Worldgate skills (27841/27842/27813/47196).
import sqlite3
from pathlib import Path

# Client compact is authoritative for skill/effect tables the Game copy may omit.
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
c = sqlite3.connect(f"file:{DB.as_posix()}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
skills = [27841, 27842, 27813, 47196, 27672, 27910]

print("=== skill_effects for activate skills ===")
eids = set()
for s in skills:
    for r in c.execute("select * from skill_effects where skill_id=?", (s,)):
        print(dict(r))
        eids.add(r["effect_id"])

print("effect_ids", sorted(eids))
cols = [x[1] for x in c.execute("pragma table_info(effects)")]
print("effects cols", cols)
for e in sorted(eids):
    r = c.execute("select * from effects where id=?", (e,)).fetchone()
    if r:
        print("effect", dict(r))

print("\n=== special_effects referencing skill ids ===")
try:
    for r in c.execute(
        "select * from special_effects where value1 in (27841,27842,27813,47196,27672,27910) "
        "or value2 in (27841,27842,27813,47196,27672,27910) "
        "or value3 in (27841,27842,27813,47196,27672,27910)"
    ):
        print(dict(r))
except Exception as ex:
    print(ex)

print("\n=== buff_skills / sphere / default / tagged ===")
for table in ("buff_skills", "sphere_skills", "default_skills", "tagged_skills", "skill_products"):
    for s in skills:
        for r in c.execute(f"select * from {table} where skill_id=?", (s,)):
            print(table, dict(r))

print("\n=== game_rule_events skill hits ===")
gcols = [x[1] for x in c.execute("pragma table_info(game_rule_events)")]
print("cols", gcols)
for col in gcols:
    for s in skills:
        try:
            rows = c.execute(f"select * from game_rule_events where {col}=? limit 5", (s,)).fetchall()
            if rows:
                print(f"gre.{col}={s}", [dict(r) for r in rows])
        except Exception:
            pass

print("\n=== ALL tables any skill-ish col = 27841/27842/27813 ===")
want = set(skills)
for (name,) in c.execute("select name from sqlite_master where type='table'"):
    try:
        tcols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    except Exception:
        continue
    for col in tcols:
        cl = col.lower()
        if not (
            "skill" in cl
            or col in ("value1", "value2", "value3", "value4", "param1", "param2", "param3")
            or cl.endswith("_id") and "effect" in cl
        ):
            continue
        for s in want:
            try:
                rows = c.execute(f"select * from [{name}] where [{col}]=? limit 3", (s,)).fetchall()
            except Exception:
                continue
            if rows:
                print(f"{name}.{col}={s} n~", len(rows), dict(rows[0]))

print("\n=== effects that CAST these skills (special CastSkill?) ===")
# effects often have actual_id pointing to special_effects / buff / etc
for e in sorted(eids):
    r = c.execute("select * from effects where id=?", (e,)).fetchone()
    if not r:
        continue
    d = dict(r)
    print("effect row", d)
    # try actual_type + actual_id style
    for k in d:
        if k in ("id",):
            continue
        v = d[k]
        if isinstance(v, int) and v > 0:
            # look up special_effects
            pass
    aid = d.get("actual_id") or d.get("effect_id")
    atype = d.get("actual_type") or d.get("effect_type") or d.get("type")
    print("  type/id", atype, aid)

print("\n=== skill_effects that APPLY effect which may cast ===")
# Reverse: which skills have effects that are SpecialEffect CastSkill pointing at 27841?
# First find special_effects with type that casts skill
try:
    scols = [x[1] for x in c.execute("pragma table_info(special_effects)")]
    print("special_effects cols", scols)
    # sample types for our value1 skills
    for r in c.execute(
        "select distinct special_effect_type_id from special_effects "
        "where value1 in (27841,27842,27813,47196) limit 20"
    ):
        print("type", dict(r))
except Exception as ex:
    print(ex)

print("\n=== doodad_func_skill_hits for our skills (listener side) ===")
for s in skills:
    for r in c.execute("select * from doodad_func_skill_hits where skill_id=?", (s,)):
        print(dict(r))
        hid = r["id"]
        # find doodad_funcs that use this
        for fr in c.execute(
            "select * from doodad_funcs where actual_func_type like '%SkillHit%' and actual_func_id=?",
            (hid,),
        ):
            print("  func", dict(fr))
            gid = fr["doodad_func_group_id"]
            for g in c.execute("select * from doodad_func_groups where id=?", (gid,)):
                print("  group", dict(g))

print("\n=== npc skills tables (any name) ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like '%npc%skill%'"
):
    print(name)
    print([x[1] for x in c.execute(f"pragma table_info({name})")])

# npcs that cast via unit skills?
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and (name like '%np_skill%' or name like '%npc_skill%' or name='skills_npcs')"
):
    print("found", name)
