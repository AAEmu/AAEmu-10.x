import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
cols = [r[1] for r in c.execute("pragma table_info(npcs)")]
print([x for x in cols if "kind" in x or "aggro" in x or "life" in x or "ai" in x][:30])
for nid in (17772, 17773, 17779, 17990):
    n = c.execute("SELECT id,name,npc_kind_id,aggression,faction_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(n))
td = c.execute("SELECT * FROM tower_defs WHERE id=72").fetchone()
print("tower72 keys", list(td.keys()) if td else None)
if td:
    print({k: td[k] for k in td.keys() if k in ("id","name","target_npc_spawner_id","target_npc_id","kill_npc_id") or "target" in k or "spawner" in k})
# tower def progs
for r in c.execute("SELECT id,tower_def_id,cond_completed_count,cond_to_next_time FROM tower_def_progs WHERE tower_def_id=72"):
    print("prog", dict(r))
