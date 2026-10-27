# -*- coding: utf-8 -*-
import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row

# buff 21093 triggers → effects
for r in c.execute(
    "select bt.id, bt.event_id, bt.effect_id, e.actual_type, e.actual_id from buff_triggers bt join effects e on e.id=bt.effect_id where bt.buff_id=21093 order by bt.id"
):
    print("trig", dict(r))
    if r["actual_type"] == "SpecialEffect":
        se = c.execute("select * from special_effects where id=?", (r["actual_id"],)).fetchone()
        print("  special", dict(se) if se else None)
    elif r["actual_type"] == "KillNpcWithoutCorpseEffect":
        ke = c.execute(
            "select * from kill_npc_without_corpse_effects where id=?", (r["actual_id"],)
        ).fetchone()
        print("  kill", dict(ke) if ke else None)
    elif r["actual_type"] == "BuffEffect":
        be = c.execute("select * from buff_effects where id=?", (r["actual_id"],)).fetchone()
        print("  buffeff", dict(be) if be else None)

# where is 21093 applied to 17789?
print("\nnpc_initial_buffs 17789", [dict(r) for r in c.execute("select * from npc_initial_buffs where npc_id=17789")])
print("buffs on 17789 via skills?")
for r in c.execute(
    """
    select ns.skill_id, s.name, se.effect_id, e.actual_type, e.actual_id
    from np_skills ns
    join skills s on s.id=ns.skill_id
    left join skill_effects se on se.skill_id=s.id
    left join effects e on e.id=se.effect_id
    where ns.owner_id=17789
    """
):
    print(dict(r))

# prog spawn targets for tower 72 that spawn 17789 / 17996
print("\ntower_def_prog_spawn_targets cols", [r[1] for r in c.execute("pragma table_info(tower_def_prog_spawn_targets)")])
for r in c.execute(
    "select * from tower_def_prog_spawn_targets where tower_def_prog_id in (280,281,282,305,306)"
):
    print(dict(r))
