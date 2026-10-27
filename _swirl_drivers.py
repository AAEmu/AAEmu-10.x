"""Who casts the SkillHit skills that open worldgate swirls / whirlpool."""
from __future__ import annotations

import sqlite3
from pathlib import Path

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)

SKILLS = {
    47196: "Ethereal Haven open/close (16739)",
    27841: "GP Battle open (8583)",
    27842: "Abyssal Attack open (8594)",
    27672: "Ezi statue phase (8532)",
    27910: "Vortex whirlpool->invis (8351)",
    27813: "Vortex invis->whirlpool (8351)",
}


def skill_name(sid):
    r = c.execute(
        "select en_us from localized_texts where tbl_name='skills' "
        "and tbl_column_name='name' and idx=?",
        (sid,),
    ).fetchone()
    return r[0] if r else "?"


print("=== skill rows ===")
for sid, note in SKILLS.items():
    row = c.execute("select id, ability_id, cooldown_time, casting_time from skills where id=?", (sid,)).fetchone()
    print(f"{sid} {skill_name(sid)!r} {note} {row}")

print("\n=== np_skills ===")
for sid in SKILLS:
    rows = c.execute(
        "select npc_id, skill_id, skill_use_condition_id from np_skills where skill_id=?",
        (sid,),
    ).fetchall()
    print(sid, "count", len(rows), rows[:15])
    for npc_id, *_ in rows[:5]:
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='npcs' "
            "and tbl_column_name='name' and idx=?",
            (npc_id,),
        ).fetchone()
        print(f"  npc {npc_id} {nm}")

print("\n=== buffs that cast these (buff_triggers / effects) ===")
# special_effects type cast skill — find type id
types = c.execute(
    "select id, name from enum_special_effect_types where lower(name) like '%skill%' "
    "or lower(name) like '%cast%' limit 40"
).fetchall()
print("special effect type names", types[:20])

# effects table link
for sid in SKILLS:
    # buff_modifiers / skill effects
    try:
        se = c.execute(
            "select id, special_effect_type_id, value1, value2 from special_effects "
            "where value1=? or value2=?",
            (sid, sid),
        ).fetchall()
        if se:
            print(f"special_effects hit skill {sid}:", se[:10])
    except Exception as e:
        print("se err", e)

print("\n=== skill effects / plots that reference skill ids as targets ===")
# skill_effects → effects
for sid in (47196, 27841, 27842):
    # skills that APPLY hit on doodad — the SkillHit receives the skill
    # look for buff tick skills, ai commands
    ai = c.execute(
        "select id, cmd_id, skill_id from ai_commands where skill_id=?", (sid,)
    ).fetchall()
    print(f"ai_commands {sid}", ai[:10])

print("\n=== quest / schedule / battle links ===")
# game_schedule_npcs with those npcs
# battle_field doodads
for table in (
    "battle_fields",
    "battle_field_doodads",
    "indun_events",
    "tower_def_progs",
):
    try:
        cols = [r[1] for r in c.execute(f"pragma table_info({table})")]
        print(table, cols[:12])
    except Exception:
        pass

# search localized for skill 47196 description
print("\n=== skill 47196 desc / related ===")
for col in ("name", "description", "web_description"):
    r = c.execute(
        f"select en_us, ko from localized_texts where tbl_name='skills' "
        f"and tbl_column_name=? and idx=47196",
        (col,),
    ).fetchone()
    if r:
        print(col, r)

# npc_mount / spawners of npcs that have the skill
print("\n=== npc_spawners for driver npcs ===")
npc_ids = set()
for sid in SKILLS:
    for row in c.execute("select npc_id from np_skills where skill_id=?", (sid,)):
        npc_ids.add(row[0])
print("driver npc ids", sorted(npc_ids)[:40], "total", len(npc_ids))

# unit_reqs / tags?
print("\n=== doodad_func_timers for Haven open path ===")
# from earlier: phase_func timer 19463 on 48987
for tid in (19463, 10421, 10418, 10639, 10491):
    cols = [r[1] for r in c.execute("pragma table_info(doodad_func_timers)")]
    row = c.execute("select * from doodad_func_timers where id=?", (tid,)).fetchone()
    print(tid, dict(zip(cols, row)) if row else None)

print("\n=== FxGroupCallback 301 ===")
try:
    cols = [r[1] for r in c.execute("pragma table_info(doodad_func_fx_group_callbacks)")]
    print("cols", cols)
    print(c.execute("select * from doodad_func_fx_group_callbacks where id=301").fetchone())
except Exception as e:
    print(e)
# maybe named doodad_func_fx_groups
for name in c.execute(
    "select name from sqlite_master where name like '%fx%group%' or name like '%callback%'"
).fetchall():
    print("tbl", name[0])
