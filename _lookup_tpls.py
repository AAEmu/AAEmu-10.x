import sqlite3
from pathlib import Path

ids = [5290, 8582, 8583, 8594, 9420, 10209, 11191, 13013, 13230, 16739, 4429, 10521, 2393, 12051, 10990, 10989]
db = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
c = sqlite3.connect(str(db))
c.row_factory = sqlite3.Row

# doodad_almighties columns
cols = [r[1] for r in c.execute("pragma table_info(doodad_almighties)")]
print("almighty cols", cols)

for i in ids:
    row = c.execute(
        "select id, name, group_id, client_doodad, model from doodad_almighties where id=?",
        (i,),
    ).fetchone()
    print("almighty", dict(row) if row else i)

# groups for those group_ids
gids = sorted({r["group_id"] for r in c.execute(
    f"select group_id from doodad_almighties where id in ({','.join(str(x) for x in ids)})"
)})
print("group_ids", gids)
for g in gids:
    print(" group", g, c.execute("select id,name from doodad_groups where id=?", (g,)).fetchone())

# localized names for these idxs
prefer = [r[1] for r in c.execute("pragma table_info(localized_texts)")]
langs = [x for x in prefer if x in ("en_us", "ko", "zh_cn")]
print("langs", langs)
for i in ids:
    rows = c.execute(
        f"""
        select idx, en_us, ko, zh_cn from localized_texts
        where tbl_name='doodad_almighties' and tbl_column_name='name' and idx=?
        """,
        (i,),
    ).fetchall()
    for r in rows:
        print(f" loc {i}: en={r[1]!r} ko={r[2]!r} zh={r[3]!r}")

# search appetizer in en_us
hits = c.execute(
    """
    select idx, en_us, ko from localized_texts
    where tbl_name='doodad_almighties' and tbl_column_name='name'
      and (lower(ifnull(en_us,'')) like '%appet%'
           or lower(ifnull(en_us,'')) like '%apetiz%'
           or lower(ifnull(en_us,'')) like '%ample%mana%'
           or lower(ifnull(en_us,'')) like '%mana%table%')
    limit 40
    """
).fetchall()
print("en appetizer-like", len(hits))
for h in hits:
    print(" ", h)
