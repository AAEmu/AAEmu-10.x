import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for nid in (17772,17773,17778,17779,17990,18041,18042,17916,17789):
    n=c.execute("SELECT id,name,faction_id,npc_kind_id FROM npcs WHERE id=?", (nid,)).fetchone()
    print(dict(n) if n else nid)
# faction relations vs Nuian 101 / alliance 148
print("--- factions 101/148 vs enemy factions")
fids=set()
for nid in (17772,17773,17778,17779,17990,18041):
    r=c.execute("SELECT faction_id FROM npcs WHERE id=?", (nid,)).fetchone()
    if r: fids.add(r[0])
print("enemy fids", fids)
for f in fids:
    for r in c.execute("SELECT * FROM faction_relations WHERE (faction1_id=? AND faction2_id IN (101,148)) OR (faction2_id=? AND faction1_id IN (101,148))", (f,f)):
        print(dict(r))
    name=c.execute("SELECT id,name FROM system_factions WHERE id=?", (f,)).fetchone()
    print("faction", dict(name) if name else f)
