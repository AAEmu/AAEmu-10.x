import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
patterns = [
    "%sea of grave%",
    "%ship grave%",
    "%graveyard%",
    "%statue of nui%",
    "%goddess%",
    "%large%statue%",
    "%colossal%",
    "%거대%",
    "%누이 여신%",
    "%누이아%",
]
for q in patterns:
    rows = c.execute(
        """
        select idx, en_us from localized_texts
        where tbl_name='doodad_almighties' and tbl_column_name='name'
          and (lower(en_us) like ? or ifnull(ko,'') like ?)
        limit 30
        """,
        (q, q),
    ).fetchall()
    if not rows:
        continue
    print("Q", q)
    for r in rows:
        t = c.execute(
            "select id, group_id, name, model from doodad_almighties where id=?",
            (r[0],),
        ).fetchone()
        print(" ", r, t)
