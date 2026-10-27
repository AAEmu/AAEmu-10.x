import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
b = c.execute("SELECT id,name,taunt,taunt_with_top_aggro,aura_radius,aura_relation_id,faction_id,pacifist FROM buffs WHERE id=21582").fetchone()
print("21582", dict(b))
for r in c.execute(
    """
    SELECT bt.id, bt.event_id, e.actual_type, e.actual_id
    FROM buff_triggers bt JOIN effects e ON e.id=bt.effect_id
    WHERE bt.buff_id=21582 LIMIT 30
    """
):
    print("trig", dict(r))
# unit modifiers?
try:
    for r in c.execute("SELECT * FROM buff_unit_modifiers WHERE buff_id=21582 LIMIT 20"):
        print("mod", dict(r))
except Exception as e:
    print("mod err", e)
# tags
for r in c.execute("SELECT t.id,t.name FROM tagged_buffs tb JOIN tags t ON t.id=tb.tag_id WHERE tb.buff_id=21582"):
    print("tag", dict(r))
# aggro effect type
print("effects named aggro on remnant skills")
