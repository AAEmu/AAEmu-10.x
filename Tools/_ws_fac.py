import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for a,b in ((103,115),(115,103),(148,115),(115,148),(101,115),(115,101)):
    rows=list(c.execute("SELECT * FROM system_faction_relations WHERE (faction1_id=? AND faction2_id=?) OR (faction1_id=? AND faction2_id=?)", (a,b,b,a)))
    print(a,b, [dict(r) for r in rows])
print("--- state meaning ---")
# enum if any
try:
  for r in c.execute("SELECT * FROM enum_system_faction_relation_states"):
    print(dict(r))
except Exception as e:
  print("enum", e)
