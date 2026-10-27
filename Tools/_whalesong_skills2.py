import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
cols=[r[1] for r in c.execute("PRAGMA table_info(skills)")]
print([x for x in cols if "target" in x.lower() or "area" in x.lower() or "aoe" in x.lower()][:30])
for sid in (36675,36704,36736,36722,36679,36567):
    sk=c.execute("SELECT id,name,target_type_id,target_selection_id,target_relation_id,target_area_count,target_area_radius FROM skills WHERE id=?", (sid,)).fetchone()
    print(dict(sk) if sk else sid)
