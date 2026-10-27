import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
for sid in (47196, 27841, 27842, 27840):
    name = c.execute(
        "select en_us from localized_texts where tbl_name='skills' and tbl_column_name='name' and idx=?",
        (sid,),
    ).fetchone()
    sk = c.execute(
        "select id, ability_id, cooldown_time, casting_time, is_auto_attack from skills where id=?",
        (sid,),
    ).fetchone()
    print("skill", sid, name, sk)
    # np_skills / npc skills
    nps = c.execute("select npc_id, skill_id from np_skills where skill_id=?", (sid,)).fetchall()
    print("  np_skills", nps[:10], "count", len(nps))
    # buff triggers?
    # special effects cast skill
    try:
        se = c.execute(
            "select id, special_effect_type_id, value1 from special_effects where value1=?",
            (sid,),
        ).fetchall()
        print("  special_effects value1", se[:10])
    except Exception as e:
        print("  se", e)

# battle field / tower that might open these
print("\n=== battle_fields / game_rule mentioning grave or golden ===")
for row in c.execute(
    "select idx, en_us from localized_texts where lower(en_us) like '%abyssal%' "
    "or lower(en_us) like '%golden plain%' or lower(en_us) like '%ship grave%' "
    "or lower(en_us) like '%whirlpool%' limit 30"
):
    print(row)
