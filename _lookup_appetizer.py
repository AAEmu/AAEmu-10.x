import sqlite3
from pathlib import Path

dbs = [
    Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\compact.sqlite3"),
    Path(r"G:\AAchina\client\game\db\compact.sqlite3"),
    Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\compact.sqlite3"),
]

needles = ["ample", "appetizer", "apetizer", "mana", "table"]

for db in dbs:
    if not db.exists():
        print("missing", db)
        continue
    c = sqlite3.connect(str(db))
    tables = {r[0] for r in c.execute("select name from sqlite_master where type='table'")}
    print("===", db, "tables", len(tables))
    if "doodad_almighties" not in tables:
        print(" no doodad_almighties")
        continue
    # name column search
    rows = c.execute(
        """
        select id, name, group_id, client_doodad, model
        from doodad_almighties
        where lower(name) like '%appet%'
           or lower(name) like '%apetiz%'
           or lower(name) like '%ample%mana%'
           or (lower(name) like '%mana%' and lower(name) like '%table%')
        limit 50
        """
    ).fetchall()
    print(" almighty name hits", len(rows))
    for r in rows:
        print(" ", r)

    # localized_texts
    if "localized_texts" in tables:
        cols = [r[1] for r in c.execute("pragma table_info(localized_texts)")]
        text_cols = [x for x in cols if x not in ("id", "tbl_name", "tbl_column_name", "idx")]
        # search en_us / ko / zh
        prefer = [c for c in ("en_us", "en_eu", "ko", "zh_cn", "zh_tw") if c in text_cols]
        if not prefer:
            prefer = text_cols[:3]
        where = " or ".join([f"lower({col}) like ?" for col in prefer])
        params = []
        for col in prefer:
            params.extend(["%appetizer%", "%apetizer%", "%ample mana%"])
        # simpler: one pattern set
        clauses = []
        args = []
        for col in prefer:
            for pat in ("%appetizer%", "%apetizer%", "%ample%mana%", "%mana%appet%"):
                clauses.append(f"lower(ifnull({col},'')) like ?")
                args.append(pat)
        sql = f"""
            select id, tbl_name, tbl_column_name, idx, {", ".join(prefer)}
            from localized_texts
            where tbl_name like '%doodad%' and ({" or ".join(clauses)})
            limit 40
        """
        try:
            loc = c.execute(sql, args).fetchall()
            print(" localized hits", len(loc), "cols", prefer)
            for r in loc:
                print(" ", r)
        except Exception as e:
            print(" loc err", e)

    # group 69 furniture?
    g = c.execute(
        "select id, name from doodad_groups where id in (47,69,24,11) or lower(name) like '%furn%' or lower(name) like '%unused%' or lower(name) like '%미사용%'"
    ).fetchall()
    print(" groups of interest", g)

    # template 5290 from around
    print(" tpl 5290", c.execute("select id,name,group_id,client_doodad from doodad_almighties where id=5290").fetchone())
