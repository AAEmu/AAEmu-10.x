import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
n = c.execute("SELECT id,npc_ai_param_id,npc_ai_client_param_id,activate_ai_always,ai_file_id,npc_kind_id FROM npcs WHERE id=17772").fetchone()
print(dict(n))
pid = n["npc_ai_param_id"]
print("param_id", pid)
if pid:
    p = c.execute("SELECT * FROM npc_ai_params WHERE id=?", (pid,)).fetchone()
    print(dict(p) if p else None)
# list all tables with ai
for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%ai%'"):
    print("t", r[0])
