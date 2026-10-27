import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("remnant initial buffs")
for r in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id=17990"):
    b = c.execute("SELECT id,name FROM buffs WHERE id=?", (r["buff_id"],)).fetchone()
    print(dict(r), dict(b) if b else None)
print("wave initial buffs 17772")
for r in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id IN (17772,17773,17779)"):
    b = c.execute("SELECT id,name FROM buffs WHERE id=?", (r["buff_id"],)).fetchone()
    print(dict(r), dict(b) if b else None)
# ai params aggression range
print("npc_ai_params cols sample")
try:
    cols=[r[1] for r in c.execute("pragma table_info(npc_ai_params)")]
    print(cols[:20])
except Exception as e:
    print(e)
# check if 21394 still on 814 somehow via unit_reqs - skill condition
# buff 21363 armor on berserker
for bid in (21363,21378,21410,21412,21413,21414):
    b=c.execute("SELECT id,name FROM buffs WHERE id=?", (bid,)).fetchone()
    tags=list(c.execute("SELECT t.id,t.name FROM tagged_buffs tb JOIN tags t ON t.id=tb.tag_id WHERE tb.buff_id=?", (bid,)))
    print("buff", dict(b) if b else bid, [dict(t) for t in tags])
