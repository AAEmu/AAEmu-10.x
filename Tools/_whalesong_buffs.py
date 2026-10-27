import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("--- buff_triggers 21093/21408/21411")
for bid in (21093,21408,21411,21393,21394):
    for r in c.execute("SELECT * FROM buff_triggers WHERE buff_id=?", (bid,)):
        print(dict(r))
# effects for those
ids=set()
for bid in (21093,21408,21411):
    for r in c.execute("SELECT effect_id FROM buff_triggers WHERE buff_id=?", (bid,)):
        ids.add(r[0])
print("effect ids", ids)
print("effects cols", [r[1] for r in c.execute("PRAGMA table_info(effects)")])
for eid in ids:
    e=c.execute("SELECT * FROM effects WHERE id=?", (eid,)).fetchone()
    print("effect", dict(e) if e else eid)
# special / skill use effects
for t in ("special_effects","skill_use_effects","buff_effects"):
    try:
        for eid in ids:
            for r in c.execute(f"SELECT * FROM {t} WHERE id=? OR effect_id=?", (eid,eid)):
                print(t, dict(r))
    except Exception as e:
        print(t, e)
# towers 73-76 progs - faction sub towers that spawn enemies
print("--- tower 73-76 spawn targets")
for tid in (73,74,75,76):
    for r in c.execute("""
      SELECT p.id prog, s.spawn_target_id, s.spawn_target_type
      FROM tower_def_progs p
      JOIN tower_def_prog_spawn_targets s ON s.tower_def_prog_id=p.id
      WHERE p.tower_def_id=? ORDER BY p.id""", (tid,)):
        print("t", tid, dict(r))
