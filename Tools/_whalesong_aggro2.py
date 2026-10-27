import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print("npcs cols sample", [r[1] for r in c.execute("PRAGMA table_info(npcs)")][:40])
for nid in (17772,17773,17779,17780,17990):
    n=c.execute("SELECT id,name,faction_id,npc_ai_param_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(n))
print("--- faction rel 115")
for r in c.execute("SELECT * FROM system_faction_relations WHERE faction1_id=115 OR faction2_id=115"):
    d=dict(r)
    if d["faction1_id"] in (1,101,115,148,149) or d["faction2_id"] in (1,101,115,148,149):
        print(d)
print("--- enum relationship / state")
for t in ("enum_system_faction_relation_states","enum_unit_relationship_codes"):
    try:
        print(t, [dict(r) for r in c.execute(f"SELECT * FROM {t}")])
    except Exception as e: print(t,e)
# ai param
print("npc_ai_params cols", [r[1] for r in c.execute("PRAGMA table_info(npc_ai_params)")])
for nid in (17772,17779):
    aid=c.execute("SELECT npc_ai_param_id FROM npcs WHERE id=?", (nid,)).fetchone()[0]
    row=c.execute("SELECT * FROM npc_ai_params WHERE id=?", (aid,)).fetchone()
    print("ai", nid, aid, dict(row) if row else None)
# buff 21394 - who gets it / does it block combat
b=c.execute("SELECT id,name,duration FROM buffs WHERE id=21394").fetchone()
print("buff21394", dict(b) if b else None)
# tags on wave npcs
print("npc tags", [r[1] for r in c.execute("PRAGMA table_info(tagged_npcs)")] if False else "")
for t in ("tagged_npcs","npc_initial_buffs"):
    cols=[r[1] for r in c.execute(f"PRAGMA table_info({t})")]
    print(t, cols)
for nid in (17772,17779):
    for r in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id=?", (nid,)):
        print("initbuff", dict(r))
