import sqlite3, json
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# clout on 35999 vs 36000
print("=== clouts")
for gid in (35999,36000):
    for r in c.execute("""
    SELECT dpf.*, dc.* FROM doodad_phase_funcs dpf
    LEFT JOIN doodad_func_clouts dc ON dc.id=dpf.actual_func_id AND dpf.actual_func_type LIKE '%Clout%'
    WHERE dpf.doodad_func_group_id=?
    """, (gid,)):
        print(gid, dict(r))
# buff 21394 details
print("21394", dict(c.execute("SELECT id,name,duration FROM buffs WHERE id=21394").fetchone()))
# buffs on enemy templates nearby
for nid in (18008,18013,17990,18024,18019):
    buffs=[dict(r) for r in c.execute("SELECT * FROM npc_initial_buffs WHERE npc_id=?", (nid,))]
    name=c.execute("SELECT name FROM npcs WHERE id=?", (nid,)).fetchone()
    print(nid, name[0] if name else None, "buffs", buffs)
# faction of those npcs
print("=== factions")
for nid in (18008,18013,17990,18024,18019,17996):
    r=c.execute("SELECT id,name,faction_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(r) if r else nid)
