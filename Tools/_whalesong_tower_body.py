import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
# which spawners produce 17990 / 17916
for nid in (17990,17916,17999,18098):
    for r in c.execute("SELECT n.npc_spawner_id, s.name, s.activation_state FROM npc_spawner_npcs n JOIN npc_spawners s ON s.id=n.npc_spawner_id WHERE n.member_id=?", (nid,)):
        print(nid, dict(r))
print("npc names")
for nid in (17990,17916,17999,18098,17789):
    print(dict(c.execute("SELECT id,name FROM npcs WHERE id=?", (nid,)).fetchone()))
