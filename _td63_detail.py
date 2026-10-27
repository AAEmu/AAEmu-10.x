import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)

print("=== tower 63 ===")
for k, v in zip(
    [x[1] for x in c.execute("pragma table_info(tower_defs)")],
    c.execute("select * from tower_defs where id=63").fetchone(),
):
    if v not in (None, "", 0, 0.0, "f"):
        print(f"  {k}={v}")

print("\n=== progs ===")
progs = c.execute(
    "select id, msg, cond_to_next_time from tower_def_progs where tower_def_id=63 order by id"
).fetchall()
print(progs)
for pid, msg, t in progs:
    spawns = c.execute(
        "select id, spawn_target_type, spawn_target_id from tower_def_prog_spawn_targets where tower_def_prog_id=?",
        (pid,),
    ).fetchall()
    kills = c.execute(
        "select id, kill_target_type, kill_target_id, kill_count from tower_def_prog_kill_targets where tower_def_prog_id=?",
        (pid,),
    ).fetchall()
    alive = c.execute(
        "select * from tower_def_prog_alive_targets where tower_def_prog_id=?",
        (pid,),
    ).fetchall()
    print(f"  prog {pid} msg={msg!r} t={t}")
    print("    spawn", spawns)
    print("    kill", kills)
    print("    alive", alive)

# spawner 18946
print("\n=== spawner 18946 ===")
print(c.execute("select * from npc_spawners where id=18946").fetchone())
members = c.execute(
    "select * from npc_spawner_npcs where npc_spawner_id=18946"
).fetchall()
print("members", members)
for m in members:
    mid = m[2]
    npc = c.execute("select id, name from npcs where id=?", (mid,)).fetchone()
    print("  npc", npc)

# map events
print("\n=== map events ===")
print(
    c.execute(
        "select * from tower_def_map_events where tower_def_id=63"
    ).fetchall()
)

# where is 18946 placed
import os, re

root = r"G:\AAchina\Server\game\worlds\main_world\level_design\zone"
hits = []
for z in os.listdir(root):
    path = os.path.join(root, z, "zone_server", "npc_spawners.g")
    if not os.path.isfile(path):
        continue
    text = open(path, encoding="utf-8", errors="replace").read()
    if "spawnerType 18946" in text or "spawnerType\t18946" in text:
        for m in re.finditer(
            r"spawnerId\s+(\d+).*?spawnerType\s+18946.*?pos\s*\(\s*x\s+([-\d.]+)\s*,\s*y\s+([-\d.]+)\s*,\s*z\s+([-\d.]+)",
            text,
            re.S,
        ):
            hits.append((z, m.group(1), m.group(2), m.group(3), m.group(4)))
print("\n=== placements 18946 ===", hits[:20], "count", len(hits))

# milestone 10
try:
    print("\nmilestone", c.execute("select * from milestones where id=10").fetchone())
except Exception as e:
    print("milestone", e)

# any quests mentioning 망자 / tower 63
qs = c.execute(
    "select id, name from quest_contexts where name like '%망자%' or name like '%고요한%' limit 20"
).fetchall()
print("quests", qs)

print("\n=== wave spawner 18948 ===")
print(c.execute("select * from npc_spawners where id=18948").fetchone())
for m in c.execute("select * from npc_spawner_npcs where npc_spawner_id=18948"):
    print(" member", m)
    print(
        " npc",
        c.execute("select id, name, level from npcs where id=?", (m[2],)).fetchone(),
    )
print(
    "np_skills 16806",
    c.execute(
        "select skill_id, skill_use_condition_id, skill_use_param1 from np_skills "
        "where owner_id=16806 and owner_type='Npc'"
    ).fetchall(),
)
c.close()
