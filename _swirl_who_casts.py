# Who casts Activate Worldgate skills?
import sqlite3
from pathlib import Path

c = sqlite3.connect(
    "file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True
)
c.row_factory = sqlite3.Row
SKILLS = [27841, 27842, 27813, 47196, 27672, 27910]

# special_effect_type for CastSkill — dump type 33 and any with our skills
print("=== special_effects type 33 sample + our skills ===")
for r in c.execute(
    "select * from special_effects where special_effect_type_id=33 "
    "and value1 in (27841,27842,27813,47196,27672,27910)"
):
    print(dict(r))

# Which effects point at those special_effects?
print("\n=== effects whose actual is SpecialEffect with CastSkill of our skills ===")
for r in c.execute(
    "select e.id, e.actual_id, e.actual_type, se.value1 "
    "from effects e "
    "join special_effects se on se.id=e.actual_id "
    "where e.actual_type='SpecialEffect' and se.value1 in (27841,27842,27813,47196,27672,27910)"
):
    print(dict(r))

# Reverse: which skills apply those effects?
print("\n=== parent skills that SpecialEffect-cast our activate skills ===")
for r in c.execute(
    """
    select se.value1 as cast_skill, se.id as se_id, e.id as effect_id, sf.skill_id as parent_skill
    from special_effects se
    join effects e on e.actual_id=se.id and e.actual_type='SpecialEffect'
    join skill_effects sf on sf.effect_id=e.id
    where se.value1 in (27841,27842,27813,47196,27672,27910)
    """
):
    print(dict(r))

# Also try ALL special_effect types whose value1 is our skill
print("\n=== any special_effect value1=our skill ===")
for s in SKILLS:
    for r in c.execute("select * from special_effects where value1=?", (s,)):
        print(dict(r))
        # parent effect + skill
        for e in c.execute(
            "select id, actual_type from effects where actual_id=? and actual_type like '%Special%'",
            (r["id"],),
        ):
            print("  effect", dict(e))
            for sf in c.execute(
                "select skill_id from skill_effects where effect_id=?", (e["id"],)
            ):
                print("  cast by skill", sf["skill_id"])

# Plots
print("\n=== plot tables mentioning skills ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like '%plot%'"
):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    skillish = [x for x in cols if "skill" in x.lower()]
    if not skillish:
        continue
    print(name, skillish)
    for col in skillish:
        for s in SKILLS:
            n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
            if n:
                print(f"  {col}={s} n={n}")
                print("  eg", dict(c.execute(f"select * from [{name}] where [{col}]=? limit 1", (s,)).fetchone()))

# Schedules / tower / game_schedule
print("\n=== schedule / tower / event tables with skill cols ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and ("
    "name like '%schedule%' or name like '%tower%' or name like '%event%' "
    "or name like '%world_message%' or name like '%zone_event%')"
):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    skillish = [x for x in cols if "skill" in x.lower()]
    if skillish:
        print(name, skillish)
        for col in skillish:
            for s in SKILLS:
                try:
                    n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
                except Exception:
                    continue
                if n:
                    print(f"  HIT {col}={s} n={n}")

# ai_commands
print("\n=== ai_commands / ai_events ===")
for name in ("ai_commands", "ai_events", "ai_command_sets"):
    try:
        cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    except Exception:
        continue
    print(name, cols)
    for col in cols:
        if "skill" not in col.lower() and col not in ("value1", "value2", "param1", "param2"):
            continue
        for s in SKILLS:
            try:
                rows = c.execute(f"select * from [{name}] where [{col}]=? limit 2", (s,)).fetchall()
            except Exception:
                continue
            if rows:
                print(f"  {name}.{col}={s}", [dict(r) for r in rows])

# skills row itself — owner / category / ability
print("\n=== skills rows ===")
scols = [x[1] for x in c.execute("pragma table_info(skills)")]
print("sample cols", scols[:30])
for s in SKILLS:
    r = c.execute(
        "select id, ability_id, category_id, casting_time, cooldown_time, target_type_id, "
        "target_selection_id, target_area_radius, show from skills where id=?",
        (s,),
    ).fetchone()
    if r:
        print(dict(r))
    name = c.execute(
        "select en_us from localized_texts where tbl_name='skills' and tbl_column_name='name' and idx=?",
        (s,),
    ).fetchone()
    if name:
        print("  name", name[0])

# npcs that cast on spawn — plot_only / on_spawn_skills style
print("\n=== tables with on_spawn / start_skill / cast ===")
for (name,) in c.execute("select name from sqlite_master where type='table'"):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    hits = [x for x in cols if any(k in x.lower() for k in ("on_spawn", "start_skill", "cast_skill", "use_skill"))]
    if hits:
        print(name, hits)

# Check buffs that fire skills on apply (buff_skills empty earlier)
# Check item use skills
print("\n=== item_skills / item use ===")
for name in ("item_proc_bindings", "item_procs", "item_skills", "items"):
    try:
        cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    except Exception:
        continue
    skillish = [x for x in cols if "skill" in x.lower()]
    if not skillish:
        continue
    print(name, skillish)
    for col in skillish:
        for s in SKILLS:
            try:
                n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
            except Exception:
                continue
            if n:
                print(f"  HIT {col}={s} n={n}")

# game_schedules linked to skills via game_schedule_doodads / quests?
print("\n=== game_schedule tables ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like 'game_schedule%'"
):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    print(name, cols)

# Search localized for Activate Golden / Abyssal to get quest/npc ids
print("\n=== loc names for activate skills parents ===")
# skills that apply effect_id equal to OUR skill ids was a false positive (id collision)
# Check effects id 27841 type
for eid in SKILLS:
    r = c.execute("select * from effects where id=?", (eid,)).fetchone()
    if r:
        print("effects id coinciding with skill", dict(r))

# Cast from SpawnEffect / Transfer / etc — look for skill id in ALL int columns of tower_def_progs
print("\n=== tower_def* skill refs ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like 'tower_def%'"
):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    print(name, cols)
    for col in cols:
        for s in SKILLS:
            try:
                n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
            except Exception:
                continue
            if n:
                print(f"  HIT {name}.{col}={s} n={n}")

# Check schedule_items / schedule skills
print("\n=== brute: any table.column = 27841 except skill_effects/doodad_func ===")
skip = {
    "skill_effects",
    "doodad_func_skill_hits",
    "skills",
    "localized_texts",
    "skill_req_skills",
    "tagged_skills",
}
for (name,) in c.execute("select name from sqlite_master where type='table'"):
    if name in skip:
        continue
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    for col in cols:
        try:
            n = c.execute(f"select count(*) from [{name}] where [{col}]=27841").fetchone()[0]
        except Exception:
            continue
        if n and n < 100:
            print(f"{name}.{col} n={n}")
            print(" ", dict(c.execute(f"select * from [{name}] where [{col}]=27841 limit 1").fetchone()))
