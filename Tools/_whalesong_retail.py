"""Whalesong TD 72 retail chain from compact — no invented ids in conclusions."""
import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")

print("=== tower_defs 72 ===")
cols = [x[1] for x in c.execute("pragma table_info(tower_defs)")]
row = c.execute("select * from tower_defs where id=72").fetchone()
td = dict(zip(cols, row))
for k in (
    "id", "name", "target_npc_spawner_id", "kill_npc_id", "start_msg", "end_msg",
    "wave_start_msg", "tod", "faction_id",
):
    if k in td:
        print(f"  {k}={td[k]}")
# print non-null-ish
for k, v in td.items():
    if v not in (None, 0, "", "f") and k not in ("name",):
        if k not in ("id",):
            pass
print("  non-empty fields:")
for k, v in td.items():
    if v not in (None, 0, 0.0, "", "f", "t") or k in ("id", "target_npc_spawner_id"):
        if isinstance(v, str) and len(v) > 80:
            continue
        print(f"    {k}={v}")

print("\n=== tower_def_progs for 72 ===")
for r in c.execute(
    "select id, tower_def_id, msg, cond_comp, cond_value, next_cond from tower_def_progs where tower_def_id=72 order by id"
):
    print(r)

print("\n=== tower_def_prog_spawn_targets / kill ===")
# discover tables
for t in c.execute(
    "select name from sqlite_master where type='table' and name like 'tower_def%'"
):
    print("table", t[0])

print("\n=== npc 17789/17790/17996/17990 spawn skills / initial buffs ===")
for nid in (17789, 17790, 17996, 17990):
    print("---", nid, c.execute("select name from npcs where id=?", (nid,)).fetchone())
    for r in c.execute("select * from npc_initial_buffs where npc_id=?", (nid,)):
        print("  initial_buff", r, c.execute("select name from buffs where id=?", (r[2],)).fetchone())

# npc_on_spawn / skill lists
for tname in (
    "npc_initial_buffs",
    "npc_passive_buffs",
    "npc_mount_skills",
    "ai_commands",
):
    pass

# Find skills linked to these NPCs via effects / spawn
print("\n=== buffs on 17789/17790 that SkillUse ===")
for nid in (17789, 17790, 17996):
    for r in c.execute("select buff_id from npc_initial_buffs where npc_id=?", (nid,)):
        bid = r[0]
        print(f"npc {nid} buff {bid}", c.execute("select name,duration from buffs where id=?", (bid,)).fetchone())
        for bt in c.execute(
            "select id, event_id, effect_id from buff_triggers where buff_id=? order by id",
            (bid,),
        ):
            e = c.execute(
                "select actual_id, actual_type from effects where id=?", (bt[2],)
            ).fetchone()
            detail = None
            if e and e[1] == "SpecialEffect":
                detail = c.execute(
                    "select special_effect_type_id, value1, value2, value3, value4 from special_effects where id=?",
                    (e[0],),
                ).fetchone()
            elif e and e[1] == "KillNpcWithoutCorpseEffect":
                detail = c.execute(
                    "select * from kill_npc_without_corpse_effects where id=?", (e[0],)
                ).fetchone()
            elif e and e[1] == "NpcSpawnerSpawnEffect":
                detail = c.execute(
                    "select id, spawner_id from npc_spawner_spawn_effects where id=?",
                    (e[0],),
                ).fetchone()
            if e and e[1] in (
                "SpecialEffect",
                "KillNpcWithoutCorpseEffect",
                "NpcSpawnerSpawnEffect",
                "SpawnEffect",
                "BuffEffect",
            ):
                print(f"  bt{bt[0]} event={bt[1]} {e} {detail}")

print("\n=== OnSpawn: how 17996 casts 36659 ===")
# search skill_effects / plots / npc spawn skills
for r in c.execute(
    """
    select se.id, se.special_effect_type_id, se.value1, se.value2
    from special_effects se where se.value1=36659 and se.special_effect_type_id=33
    """
):
    print("special", r)
# unit_reqs / npc skill packs?
for t in c.execute(
    "select name from sqlite_master where type='table' and (name like '%spawn%skill%' or name like '%npc%skill%' or name like '%on_spawn%' or name like '%np_skill%')"
):
    print("cand", t[0])

# skills that NPCs know - npc_skills might be empty name; try tagged
print("\n=== skill 36659 AoE vs doodad SkillHit path ===")
print(
    "skillhits needing 36659:",
    list(
        c.execute(
            """
            select g.doodad_almighty_id, df.doodad_func_group_id, df.next_phase, da.name, da.model
            from doodad_funcs df
            join doodad_func_groups g on g.id=df.doodad_func_group_id
            join doodad_func_skill_hits h on h.id=df.actual_func_id
            join doodad_almighties da on da.id=g.doodad_almighty_id
            where df.actual_func_type='DoodadFuncSkillHit' and h.skill_id=36659
            """
        )
    ),
)

print("\n=== 12312 clout relation ===")
cl = c.execute("select * from doodad_func_clouts where id=3380").fetchone()
clcols = [x[1] for x in c.execute("pragma table_info(doodad_func_clouts)")]
print(dict(zip(clcols, cl)))

print("\n=== buff 21394 immune attrs ===")
for r in c.execute(
    "select unit_attribute_id, unit_modifier_type_id, value from unit_modifiers where owner_type='Buff' and owner_id=21394"
):
    print(r)
for r in c.execute(
    "select tag_id from tagged_buffs where buff_id=21394"
):
    print("tag", r, c.execute("select name from tags where id=?", (r[0],)).fetchone())
