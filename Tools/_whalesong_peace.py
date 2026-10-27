# -*- coding: utf-8 -*-
import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row


def d(t, rows):
    print(f"=== {t} ({len(rows)}) ===")
    for r in rows:
        print(dict(r))


d(
    "skill_hits peace phases",
    c.execute(
        "select * from doodad_func_skill_hits where id in (4890,4892,4919,4964,4965,4966,4967)"
    ).fetchall(),
)

print("phase_funcs cols", [r[1] for r in c.execute("pragma table_info(doodad_phase_funcs)")])
for g in (35999, 36000, 36001, 36149):
    d(
        f"phase_funcs {g}",
        c.execute(
            "select * from doodad_phase_funcs where doodad_func_group_id=?", (g,)
        ).fetchall(),
    )

d("buff_effects 23219", c.execute("select * from buff_effects where id=23219").fetchall())
print("buff_effects cols", [r[1] for r in c.execute("pragma table_info(buff_effects)")])

# tags
print("tagged_buffs cols", [r[1] for r in c.execute("pragma table_info(tagged_buffs)")])
d("tagged_buffs tag 3274", c.execute("select * from tagged_buffs where tag_id=3274").fetchall())
d("tags 3274", c.execute("select * from tags where id=3274").fetchall())
d(
    "buff 21394 tags",
    c.execute("select * from tagged_buffs where buff_id=21394").fetchall(),
)

d("buff 21394", c.execute("select id,name,duration,tick from buffs where id=21394").fetchall())
d("buff 21093", c.execute("select id,name,duration,tick from buffs where id=21093").fetchall())
d("buff from effect 23219", c.execute("select id,name,duration from buffs where id=23219").fetchall())

# what does BuffEffect 23219 apply? buff_id column
be = c.execute("select * from buff_effects where id=23219").fetchone()
if be:
    print("buff_effect 23219 full", dict(be))

d("clouts buff 21394", c.execute("select * from doodad_func_clouts where buff_id=21394").fetchall())
clouts = [r["id"] for r in c.execute("select id from doodad_func_clouts where buff_id=21394")]
if clouts:
    ids = ",".join(map(str, clouts))
    d(
        "doodad_funcs Clout peace",
        c.execute(
            f"select * from doodad_funcs where actual_func_type='DoodadFuncClout' and actual_func_id in ({ids})"
        ).fetchall(),
    )
    # find which almighty
    for r in c.execute(
        f"""
        select df.doodad_func_group_id, df.actual_func_id, g.doodad_almighty_id, a.name
        from doodad_funcs df
        join doodad_func_groups g on g.id=df.doodad_func_group_id
        join doodad_almighties a on a.id=g.doodad_almighty_id
        where df.actual_func_type='DoodadFuncClout' and df.actual_func_id in ({ids})
        """
    ):
        print("clout owner", dict(r))

# skill_use_condition 5 = OnSpawn?
d(
    "skill_use_conditions",
    c.execute("select * from skill_use_conditions").fetchall()
    if False
    else [],
)
print(
    "enum/skill use?",
    [r[1] for r in c.execute("pragma table_info(skill_use_conditions)")],
)
try:
    d("suc", c.execute("select * from skill_use_conditions").fetchall())
except Exception as e:
    print(e)

# enum_skill_use_conditions or similar
for t in c.execute(
    "select name from sqlite_master where type='table' and name like '%skill_use%'"
).fetchall():
    print("table", t[0])
    d(t[0], c.execute(f"select * from {t[0]}").fetchall())

# application_method_id 1 meaning
for t in c.execute(
    "select name from sqlite_master where type='table' and (name like '%application%' or name like '%skill_effect_app%')"
).fetchall():
    print("app table", t[0])

# Does 36659 SkillHit match skill_id on 4890?
hit = c.execute("select * from doodad_func_skill_hits where id=4890").fetchone()
print("hit 4890", dict(hit) if hit else None)
if hit:
    sid = hit["skill_id"]
    d(f"skill that SkillHit 4890 wants ({sid})", c.execute("select id,name from skills where id=?", (sid,)).fetchall())

# All skills that match any 12312 SkillHit skill_id
hits = c.execute(
    "select * from doodad_func_skill_hits where id in (4890,4892,4919,4964,4965,4966,4967)"
).fetchall()
for h in hits:
    print("HIT", dict(h), "-> skill", c.execute("select id,name from skills where id=?", (h["skill_id"],)).fetchone())

# BuffEffect: does it DISPEL tag or APPLY buff?
# Check if buff 23219 is a dummy / remove type
print("buffs cols sample", [r[1] for r in c.execute("pragma table_info(buffs)")])
d(
    "buff 23219 detail",
    c.execute("select * from buffs where id=23219").fetchall(),
)

# special effect path for remove?
d(
    "effects of type Dispel for skill 36659 chain",
    c.execute(
        """
        select e.id, e.actual_type, e.actual_id from effects e
        where e.id in (select effect_id from skill_effects where skill_id=36659)
        """
    ).fetchall(),
)

# buff_modifiers / buff_remove?
for name in ("buff_remove_effects", "dispel_effects", "buff_effects"):
    try:
        cols = [r[1] for r in c.execute(f"pragma table_info({name})")]
        print(name, cols[:30])
    except Exception as e:
        print(name, e)
