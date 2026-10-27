# -*- coding: utf-8 -*-
import os
import sqlite3

paths = [
    r"G:\AAchina\Server\game\db\compact.sqlite3",
    r"G:\AAchina\client\game\db\compact.sqlite3",
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\compact.sqlite3",
]

db = None
used = None
for p in paths:
    if not os.path.exists(p):
        print("MISSING", p)
        continue
    sz = os.path.getsize(p)
    c = sqlite3.connect(f"file:{p}?mode=ro", uri=True)
    c.row_factory = sqlite3.Row
    n = c.execute("select count(*) from sqlite_master where type='table'").fetchone()[0]
    print(p, "size", sz, "tables", n)
    if n and db is None:
        db = c
        used = p
        print("USING", p)

assert db is not None
c = db


def q(sql, *a):
    try:
        return c.execute(sql, a).fetchall()
    except Exception as e:
        print("ERR", sql[:90], e)
        return []


def dump(title, rows, limit=50):
    print(f"\n=== {title} ({len(rows)}) ===")
    for r in rows[:limit]:
        print(dict(r))


dump(
    "skill 36659",
    q(
        """select id,name,target_type_id,target_selection_id,target_area_radius,target_area_count,
                  target_relation_id,target_alive,target_dead,doodad_hit_family,plot_id,plot_only,
                  effect_delay,casting_time,channeling_time,max_range,min_range,front_angle,target_area_angle
           from skills where id=36659"""
    ),
)
dump("skill_effects 36659", q("select * from skill_effects where skill_id=36659"))
for se in q("select effect_id from skill_effects where skill_id=36659"):
    eid = se["effect_id"]
    dump(f"effects {eid}", q("select id,actual_type,actual_id from effects where id=?", eid))

for bid in (21093, 21394):
    dump(
        f"buff {bid}",
        q(
            """select id,name,duration,tick,stack_level,buff_kind_id,system,save,one_time
               from buffs where id=?""",
            bid,
        ),
    )
    dump(f"buff_triggers {bid}", q("select * from buff_triggers where buff_id=?", bid))
    dump(f"buff_tick_effects {bid}", q("select * from buff_tick_effects where buff_id=?", bid))

dump(
    "doodad_almighties 12312",
    q("select id,name,group_id,model,show_name from doodad_almighties where id=12312"),
)
dump(
    "doodad_func_groups 12312",
    q(
        "select id,doodad_almighty_id,doodad_func_group_kind_id,name,phase_msg from doodad_func_groups where doodad_almighty_id=12312 order by id"
    ),
)

groups = [r["id"] for r in q("select id from doodad_func_groups where doodad_almighty_id=12312")]
if groups:
    ph = ",".join(str(g) for g in groups)
    dump(
        "doodad_funcs for 12312 groups",
        q(
            f"select * from doodad_funcs where doodad_func_group_id in ({ph}) order by doodad_func_group_id, next_phase"
        ),
    )
    skill_hit_ids = [
        r["actual_id"]
        for r in q(
            f"select actual_id from doodad_funcs where doodad_func_group_id in ({ph}) and actual_func_type='DoodadFuncSkillHit'"
        )
    ]
    if skill_hit_ids:
        dump(
            "doodad_func_skill_hits",
            q(f"select * from doodad_func_skill_hits where id in ({','.join(map(str,skill_hit_ids))})"),
        )
    clout_ids = [
        r["actual_id"]
        for r in q(
            f"select actual_id from doodad_funcs where doodad_func_group_id in ({ph}) and actual_func_type='DoodadFuncClout'"
        )
    ]
    if clout_ids:
        dump(
            "doodad_func_clouts",
            q(f"select * from doodad_func_clouts where id in ({','.join(map(str,clout_ids))})"),
        )
        dump(
            "doodad_func_clout_effects",
            q(
                f"select * from doodad_func_clout_effects where doodad_func_clout_id in ({','.join(map(str,clout_ids))})"
            ),
        )

for pid in (35999, 36000, 4890):
    dump(f"func_group {pid}", q("select * from doodad_func_groups where id=?", pid))
    dump(f"funcs in {pid}", q("select * from doodad_funcs where doodad_func_group_id=?", pid))

for nid in (17996, 17789):
    dump(f"npc {nid}", q("select id,name,npc_kind_id,level,faction_id from npcs where id=?", nid))
    # np_skills schema varies
    cols = [r[1] for r in c.execute("pragma table_info(np_skills)")]
    print("np_skills cols", cols)
    if "npc_id" in cols:
        dump(f"np_skills {nid}", q("select * from np_skills where npc_id=?", nid))
    elif "owner_id" in cols:
        dump(f"np_skills {nid}", q("select * from np_skills where owner_id=?", nid))

dump(
    "skills 36650-36690",
    q(
        "select id,name,target_type_id,target_area_radius,target_area_count,doodad_hit_family from skills where id between 36650 and 36690"
    ),
)

dump("tower_defs 72", q("select * from tower_defs where id=72"))
print("tower_def_progs cols", [r[1] for r in c.execute("pragma table_info(tower_def_progs)")])
dump("tower_def_progs 72", q("select * from tower_def_progs where tower_def_id=72 order by id"))

gid = q("select group_id from doodad_almighties where id=12312")
if gid:
    g = gid[0][0]
    dump(
        f"skills doodad_hit_family={g}",
        q(
            "select id,name,doodad_hit_family,target_area_radius,target_area_count from skills where doodad_hit_family=?",
            g,
        ),
    )

# which skill hits SkillHit on 12312
print("\n=== SkillHit skill_ids on 12312 phases ===")
for r in q(
    """
    select df.doodad_func_group_id, df.id as func_id, sh.id as hit_id, sh.skill_id
    from doodad_funcs df
    join doodad_func_skill_hits sh on sh.id = df.actual_id
    where df.actual_func_type='DoodadFuncSkillHit'
      and df.doodad_func_group_id in (select id from doodad_func_groups where doodad_almighty_id=12312)
    """
):
    print(dict(r))

# OnSpawn for 17996 - find how skill is attached
print("\n=== looking for OnSpawn / skill links for 17996 ===")
for tname in [
    "npc_initial_buffs",
    "np_skills",
    "ai_events",
    "npc_mount_skills",
    "unit_req_skills",
]:
    try:
        cols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
    except Exception:
        continue
    print(tname, cols)
