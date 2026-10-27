import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# wave NPC AI + aggro
for nid in (17772,17773,17779,17780,17990):
    n=dict(c.execute("SELECT id,name,faction_id,npc_ai_param_id,aggro_link,absolute_return_distance,absolute_sight_range_scale FROM npcs WHERE id=?", (nid,)).fetchone())
    print("NPC", n)
    aid=n.get("npc_ai_param_id")
    if aid:
        # schema
        pass
print("npc_ai_params cols", [r[1] for r in c.execute("PRAGMA table_info(npc_ai_params)")])
print("npc_ai_client_params cols", [r[1] for r in c.execute("PRAGMA table_info(npc_ai_client_params)")])
# aggro links
print("aggro tables", [r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%aggro%'")])
for nid in (17772,17773,17779):
    for t in ("npc_aggro_links","aggro_links"):
        try:
            rows=c.execute(f"SELECT * FROM {t} WHERE owner_id=? OR npc_id=? OR id=?", (nid,nid,nid)).fetchall()
            if rows: print(t, nid, [dict(r) for r in rows[:5]])
        except Exception as e:
            cols=[r[1] for r in c.execute(f"PRAGMA table_info({t})")]
            print(t, "cols", cols, e)
# faction 115 vs 101
for r in c.execute("SELECT * FROM system_faction_relations WHERE (faction1_id=115 AND faction2_id IN (101,148,1)) OR (faction2_id=115 AND faction1_id IN (101,148,1))"):
    print("rel", dict(r))
print("enum states", [dict(r) for r in c.execute("SELECT * FROM enum_unit_relationship_codes")])
# ai params for 17772
aid=c.execute("SELECT npc_ai_param_id FROM npcs WHERE id=17772").fetchone()[0]
print("ai_param id", aid)
row=c.execute("SELECT * FROM npc_ai_params WHERE id=?", (aid,)).fetchone()
print("ai_param", dict(row) if row else None)
