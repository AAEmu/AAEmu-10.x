import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print(dict(c.execute("SELECT * FROM ai_files WHERE id=27").fetchone()))
# remnant ai file
n = c.execute("SELECT id,ai_file_id,npc_ai_param_id FROM npcs WHERE id=17990").fetchone()
print("remnant", dict(n))
print(dict(c.execute("SELECT * FROM ai_files WHERE id=?", (n["ai_file_id"],)).fetchone()))
# client params
cols=[r[1] for r in c.execute("pragma table_info(npc_ai_client_params)")]
print("client cols", cols)
p=c.execute("SELECT * FROM npc_ai_client_params WHERE id=4188").fetchone()
print("client 4188", dict(p) if p else None)
# try id = npc id
p=c.execute("SELECT * FROM npc_ai_client_params LIMIT 1").fetchone()
print("sample", dict(p) if p else None)
