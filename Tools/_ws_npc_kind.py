import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for nid in (17772, 17773, 17779, 17990):
    n = c.execute(
        "SELECT id,name,npc_kind_id,aggression,absolute_life,exp FROM npcs WHERE id=?",
        (nid,),
    ).fetchone()
    print("npc", dict(n) if n else nid)
print("kinds", [dict(r) for r in c.execute("SELECT * FROM enum_npc_kinds")])
# ai_params for these
for nid in (17772, 17990):
    rows = list(c.execute("SELECT * FROM npc_ai_params WHERE id=?", (nid,)))
    print("ai", nid, [dict(r) for r in rows[:3]], "count", len(rows))
# check npc_ai_client_params
try:
    cols = [r[1] for r in c.execute("pragma table_info(npc_ai_client_params)")]
    print("client_ai cols", cols)
    for nid in (17772, 17990):
        r = c.execute("SELECT * FROM npc_ai_client_params WHERE id=?", (nid,)).fetchone()
        if r:
            print("client", dict(r))
except Exception as e:
    print(e)
