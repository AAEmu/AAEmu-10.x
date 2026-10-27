# Link tower_defs 18/36/95 to activate skills / doodads.
import sqlite3

c = sqlite3.connect("file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
TOWERS = [18, 19, 20, 36, 37, 95]
SKILLS = {27841, 27842, 27813, 47196, 27672, 27910}

print("=== tower_def_progs ===")
for tid in TOWERS:
    for r in c.execute("select * from tower_def_progs where tower_def_id=?", (tid,)):
        print(dict(r))

print("\n=== tower_def_prog_spawn_targets ===")
for tid in TOWERS:
    for r in c.execute(
        "select t.*, p.tower_def_id from tower_def_prog_spawn_targets t "
        "join tower_def_progs p on p.id=t.tower_def_prog_id where p.tower_def_id=?",
        (tid,),
    ):
        print(dict(r))

print("\n=== tower_def_map_events ===")
for tid in TOWERS:
    for r in c.execute("select * from tower_def_map_events where tower_def_id=?", (tid,)):
        d = dict(r)
        print(d)
        # resolve target
        if d["map_target_type"] and "Doodad" in str(d["map_target_type"]):
            print("  doodad target", d["map_target_id"])
        if d["map_resource_id"]:
            print("  resource", d["map_resource_id"])

print("\n=== npcs / skills on tower target spawners ===")
for tid in TOWERS:
    td = c.execute("select * from tower_defs where id=?", (tid,)).fetchone()
    print("tower", tid, dict(td) if td else None)
    sp = td["target_npc_spawner_id"] if td else None
    if not sp:
        continue
    # spawner template
    try:
        for r in c.execute("select * from npc_spawners where id=?", (sp,)):
            print("  spawner", dict(r))
    except Exception as e:
        print("  npc_spawners", e)

# Search tower_def tables for skill ids
print("\n=== any tower_def* column = activate skills ===")
for (name,) in c.execute(
    "select name from sqlite_master where type='table' and name like 'tower_def%'"
):
    cols = [x[1] for x in c.execute(f"pragma table_info({name})")]
    for col in cols:
        for s in SKILLS:
            try:
                n = c.execute(f"select count(*) from [{name}] where [{col}]=?", (s,)).fetchone()[0]
            except Exception:
                continue
            if n:
                print(f"HIT {name}.{col}={s} n={n}")
                print(dict(c.execute(f"select * from [{name}] where [{col}]=? limit 1", (s,)).fetchone()))

# npcs that cast skills named activate - check npc_skills in Server compact if different
print("\n=== Server compact np*skill tables ===")
try:
    s = sqlite3.connect("file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
    s.row_factory = sqlite3.Row
    for (name,) in s.execute(
        "select name from sqlite_master where type='table' and name like '%skill%'"
    ):
        if "npc" in name.lower() or name.startswith("np_"):
            print(name, [x[1] for x in s.execute(f"pragma table_info({name})")])
            for col in [x[1] for x in s.execute(f"pragma table_info({name})")]:
                if "skill" not in col.lower():
                    continue
                for sid in SKILLS:
                    n = s.execute(f"select count(*) from [{name}] where [{col}]=?", (sid,)).fetchone()[0]
                    if n:
                        print(f"  HIT {name}.{col}={sid}", dict(s.execute(f"select * from [{name}] where [{col}]=? limit 1", (sid,)).fetchone()))
except Exception as e:
    print("server db", e)

# How does World start tower 36? Schedules?
print("\n=== game_schedule links to tower / abyssal ===")
# schedule 301/303/305 are active Abyssal Attack weekdays
for sid in (301, 303, 305, 299, 300):
    for t in ("game_schedule_doodads", "game_schedule_spawners", "game_schedule_quests"):
        n = c.execute(f"select count(*) from {t} where game_schedule_id=?", (sid,)).fetchone()[0]
        if n:
            print(f"{t} sched {sid} n={n}")
            for r in c.execute(f"select * from {t} where game_schedule_id=? limit 5", (sid,)):
                print(" ", dict(r))
