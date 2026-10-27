# Resolve np_skills casters for worldgate activate skills.
import sqlite3

s = sqlite3.connect("file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
s.row_factory = sqlite3.Row
c = sqlite3.connect("file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row

NPCS = [15157, 15156, 15159, 15077, 21022]
SKILLS = {
    15157: 27841,
    15156: 27842,
    15159: 27813,
    15077: 27672,
    21022: 47196,
}

print("=== skill_use_condition enums ===")
for t in (
    "enum_skill_use_conditions",
    "skill_use_conditions",
    "np_skill_use_conditions",
):
    try:
        print(t, [dict(r) for r in s.execute(f"select * from {t}")])
    except Exception as e:
        print(t, e)

# condition ids we saw: 5 and 2
print("\n=== distinct skill_use_condition_id in np_skills ===")
for r in s.execute(
    "select skill_use_condition_id, count(*) n from np_skills group by skill_use_condition_id"
):
    print(dict(r))

print("\n=== NPC names + np_skills rows ===")
for nid in NPCS:
    n = c.execute(
        "select en_us from localized_texts where tbl_name='npcs' and "
        "tbl_column_name='name' and idx=?",
        (nid,),
    ).fetchone()
    npc = c.execute("select id, name from npcs where id=?", (nid,)).fetchone()
    print(f"npc {nid} loc={n[0] if n else None} row={dict(npc) if npc else None}")
    for r in s.execute("select * from np_skills where owner_id=? and owner_type='Npc'", (nid,)):
        print("  np_skill", dict(r))
        sid = r["skill_id"]
        sn = c.execute(
            "select en_us from localized_texts where tbl_name='skills' and "
            "tbl_column_name='name' and idx=?",
            (sid,),
        ).fetchone()
        print("    skill name", sn[0] if sn else sid)

print("\n=== where these NPCs are tower spawn targets / schedule ===")
for nid in NPCS:
    # as kill targets
    for r in c.execute(
        "select id, name from tower_defs where kill_npc_id=? or target_npc_spawner_id=?",
        (nid, nid),
    ):
        print("tower_defs", nid, dict(r))
    # spawn target via spawner that uses this npc — need npc_spawner members
    # Server may have npc_spawner_npcs or similar
    for (name,) in s.execute(
        "select name from sqlite_master where type='table' and name like '%spawner%'"
    ):
        cols = [x[1] for x in s.execute(f"pragma table_info({name})")]
        if "npc_id" not in cols and "unit_id" not in cols:
            continue
        col = "npc_id" if "npc_id" in cols else "unit_id"
        n = s.execute(f"select count(*) from [{name}] where [{col}]=?", (nid,)).fetchone()[0]
        if n:
            print(f"  {name}.{col}={nid} n={n}")
            print(" ", dict(s.execute(f"select * from [{name}] where [{col}]=? limit 1", (nid,)).fetchone()))

# Also check client for same np_skills
print("\n=== client has np_skills? ===")
try:
    print("client count", c.execute("select count(*) from np_skills").fetchone()[0])
except Exception as e:
    print("client", e)

# skill_use_condition_id meanings from sample of condition 5
print("\n=== sample other condition-5 skills (on spawn?) ===")
for r in s.execute(
    "select * from np_skills where skill_use_condition_id=5 and skill_id in "
    "(27841,27842,27813,47196,27672,27910,31928) "
):
    print(dict(r))

# 27910 caster?
print("\n=== np_skills for 27910 / 31928 ===")
for sid in (27910, 31928):
    for r in s.execute("select * from np_skills where skill_id=?", (sid,)):
        print(dict(r))
        n = c.execute(
            "select en_us from localized_texts where tbl_name='npcs' and "
            "tbl_column_name='name' and idx=?",
            (r["owner_id"],),
        ).fetchone()
        print("  npc", n[0] if n else r["owner_id"])
