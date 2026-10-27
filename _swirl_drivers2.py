"""Continue swirl driver hunt with correct table names."""
import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)

SKILLS = [47196, 27841, 27842, 27672, 27910, 27813]

print("=== tables with skill ===")
tabs = [
    r[0]
    for r in c.execute(
        "select name from sqlite_master where type='table' and ("
        "name like '%skill%' or name like '%npc%skill%' or name like '%ai_command%')"
    )
]
print(tabs)

for t in tabs:
    cols = [r[1] for r in c.execute(f"pragma table_info({t})")]
    if any("skill" in x.lower() for x in cols):
        skill_cols = [x for x in cols if "skill" in x.lower()]
        print(f"\n{t} skill cols {skill_cols}")
        for sid in SKILLS:
            # try each skill column
            for sc in skill_cols:
                try:
                    rows = c.execute(f"select * from {t} where {sc}=?", (sid,)).fetchall()
                except Exception:
                    continue
                if rows:
                    print(f"  {sid} via {sc}: {len(rows)} eg {rows[0]}")

print("\n=== effects / plots referencing Activate skills ===")
# skill_effects
for sid in SKILLS:
    # buffs named similarly
    for row in c.execute(
        "select idx, en_us from localized_texts where tbl_name='buffs' "
        "and tbl_column_name='name' and ("
        "lower(en_us) like '%golden plain%' or lower(en_us) like '%abyssal attack%' "
        "or lower(en_us) like '%ethereal haven%' or lower(en_us) like '%worldgate%')"
        " limit 5"
    ):
        pass

for row in c.execute(
    "select idx, en_us from localized_texts where tbl_column_name='name' and ("
    "lower(en_us) like '%activate%golden%' or lower(en_us) like '%activate%abyssal%' "
    "or lower(en_us) like '%abyssal attack begin%' or lower(en_us) like '%ethereal haven%')"
    " limit 40"
):
    print("loc", row)

# transfer / zone gates
print("\n=== open_world / worldgate tables ===")
for name in c.execute(
    "select name from sqlite_master where type='table' and ("
    "name like '%worldgate%' or name like '%portal%' or name like '%battle_field%' "
    "or name like '%instant_game%' or name like '%game_rule%')"
).fetchall():
    print(name[0])

# battle_fields content
try:
    cols = [r[1] for r in c.execute("pragma table_info(battle_fields)")]
    print("battle_fields cols", cols)
    for row in c.execute("select * from battle_fields limit 20"):
        print(row)
except Exception as e:
    print(e)

# check Server scripts for skill ids - can't write Server; grep client scripts?
print("\n=== doodad_func_timers next ===")
cols = [r[1] for r in c.execute("pragma table_info(doodad_func_timers)")]
print(cols)
for tid in (19463, 10421, 10418, 10639):
    row = c.execute("select * from doodad_func_timers where id=?", (tid,)).fetchone()
    print(tid, dict(zip(cols, row)) if row else None)
