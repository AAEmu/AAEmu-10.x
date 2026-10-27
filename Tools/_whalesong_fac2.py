import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print([r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%faction%'")])
# try common names
for t in ("system_faction_relations","faction_relation","system_factions"):
    try:
        print(t, [r[1] for r in c.execute(f"PRAGMA table_info({t})")])
    except: pass
for r in c.execute("SELECT * FROM system_faction_relations WHERE (id=? OR id2=?) AND (id IN (101,148,115) OR id2 IN (101,148,115))", (115,115)).fetchall() if False else []:
    pass
# discover
cols=None
for t in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%relation%'"):
    print("tbl", t[0], [r[1] for r in c.execute(f"PRAGMA table_info({t[0]})")])
