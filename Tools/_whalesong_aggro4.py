import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
cols=[r[1] for r in c.execute("PRAGMA table_info(npcs)")]
print([x for x in cols if "aggro" in x.lower() or "aggress" in x.lower() or "sight" in x.lower() or "attack" in x.lower()])
for nid in (17772,17773,17779,17990):
    n=c.execute("SELECT id,name,aggression,attack_start_range_scale,sight_range_scale,sight_fov_scale FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(n))
for bid in (21363,21378,21394):
    b=c.execute("SELECT id,name,duration FROM buffs WHERE id=?", (bid,)).fetchone()
    print("buff", dict(b))
    for r in c.execute("SELECT bt.id,bt.event_id,bt.effect_id,e.actual_type,e.actual_id FROM buff_triggers bt JOIN effects e ON e.id=bt.effect_id WHERE bt.buff_id=? LIMIT 15", (bid,)):
        print(" ", dict(r))
# special effects on those
for bid in (21363,21378):
    for r in c.execute("""
      SELECT se.* FROM buff_triggers bt
      JOIN effects e ON e.id=bt.effect_id
      JOIN special_effects se ON se.id=e.actual_id
      WHERE bt.buff_id=? AND e.actual_type='SpecialEffect' LIMIT 20
    """, (bid,)):
        print("se", bid, dict(r))
