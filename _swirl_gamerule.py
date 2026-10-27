# game_rule_events + battlefield link to activate skills; also search skill 31928 pattern.
import sqlite3

c = sqlite3.connect("file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row

SKILLS = [27841, 27842, 27813, 47196, 27672, 27910, 31928, 31935]

print("=== game_rule_sets ===")
cols = [x[1] for x in c.execute("pragma table_info(game_rule_sets)")]
print(cols)
# Golden plains battlefield uses game_rule_set_id from battle_fields
for bf in c.execute("select id, zone_key, game_rule_set_id, option from battle_fields"):
    print("bf", dict(bf))

print("\n=== game_rule_events for golden plains rule sets ===")
# rule sets used by open-world BFs — find which have param = our skills
for r in c.execute("select * from game_rule_events"):
    d = dict(r)
    if d["param1"] in SKILLS or d["param2"] in SKILLS:
        print("HIT", d)

print("\n=== distinct kind_id on game_rule_events ===")
for r in c.execute(
    "select kind_id, count(*) n from game_rule_events group by kind_id order by kind_id"
):
    print(dict(r))

print("\n=== sample events per kind (first 2) ===")
for (kid,) in c.execute("select distinct kind_id from game_rule_events order by kind_id"):
    rows = c.execute(
        "select * from game_rule_events where kind_id=? limit 2", (kid,)
    ).fetchall()
    print(f"kind {kid}:", [dict(r) for r in rows])

print("\n=== conditions table ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like '%rule%cond%'"
):
    print(name, [x[1] for x in c.execute(f"pragma table_info({name})")])

# Also enum / const for kind
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and ("
    "name like '%game_rule%' or name like '%enum%rule%')"
):
    print("tbl", name, [x[1] for x in c.execute(f"pragma table_info({name})")])

print("\n=== skills 31928/31935 (Diamond Shores keeper) refs as skill_id ===")
for s in (31928, 31935):
    print("skill", s)
    for (name,) in c.execute("select name from sqlite_master where type='table'"):
        cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
        for col in cols:
            if "skill" not in col.lower():
                continue
            try:
                n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
            except Exception:
                continue
            if n:
                print(f"  {name}.{col} n={n}")
                print("  ", dict(c.execute(f"select * from [{name}] where [{col}]=? limit 1", (s,)).fetchone()))

print("\n=== target_type / selection enums for activate skills ===")
# target_type_id 0, selection 1 — what are they?
for t in ("enum_skill_target_types", "skill_target_types", "enum_target_types"):
    try:
        print(t, [dict(r) for r in c.execute(f"select * from {t} limit 20")])
    except Exception as e:
        print(t, e)

# category_id 4 vs 8
for t in ("skill_categories", "enum_skill_categories"):
    try:
        print(t, [dict(r) for r in c.execute(f"select * from {t}")])
    except Exception as e:
        print(t, e)

print("\n=== doodad closed phase timers for 8583/8594/16739/8532 ===")
for almighty in (8583, 8594, 16739, 8532, 8351):
    print(f"\nalmighty {almighty}")
    for g in c.execute(
        "select id, doodad_func_group_kind_id, model, comment from doodad_func_groups "
        "where doodad_almighty_id=?",
        (almighty,),
    ):
        print("  group", dict(g))
        for f in c.execute(
            "select id, actual_func_type, actual_func_id, next_phase from doodad_funcs "
            "where doodad_func_group_id=?",
            (g["id"],),
        ):
            print("    func", dict(f))
            if f["actual_func_type"] == "DoodadFuncTimer":
                t = c.execute(
                    "select * from doodad_func_timers where id=?", (f["actual_func_id"],)
                ).fetchone()
                print("      timer", dict(t) if t else None)
            if f["actual_func_type"] == "DoodadFuncSkillHit":
                h = c.execute(
                    "select * from doodad_func_skill_hits where id=?", (f["actual_func_id"],)
                ).fetchone()
                print("      hit", dict(h) if h else None)

print("\n=== game_schedule_doodads for battle portals ===")
for did in (8583, 8594, 16739, 8532, 8351, 8592, 8593, 8595, 8596):
    for r in c.execute("select * from game_schedule_doodads where doodad_id=?", (did,)):
        gs = c.execute("select * from game_schedules where id=?", (r["game_schedule_id"],)).fetchone()
        print(dict(r), "->", dict(gs) if gs else None)
