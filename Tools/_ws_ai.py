import sqlite3, json
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print([dict(r) for r in c.execute("SELECT * FROM enum_npc_kinds")])
for nid in (17772, 17990):
    n = c.execute("SELECT id,npc_ai_param_id,npc_ai_client_param_id,activate_ai_always,ai_file_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print("npc", dict(n))
    if n and n["npc_ai_param_id"]:
        p = c.execute("SELECT * FROM npc_ai_params WHERE id=?", (n["npc_ai_param_id"],)).fetchone()
        print("param", dict(p) if p else None)
    rows = list(c.execute("SELECT * FROM npc_ai_params WHERE id=?", (nid,)))
    print("by npc id", rows)
# Server may use different schema - try ai_param text
try:
    p = c.execute("SELECT id, ai_param FROM npc_ai_params WHERE id=(SELECT npc_ai_param_id FROM npcs WHERE id=17772)").fetchone()
    print("ai_param 17772", p["ai_param"][:500] if p and p["ai_param"] else p)
except Exception as e:
    print(e)
