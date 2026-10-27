import sqlite3
db = r"G:\AAchina\Server\game\db\compact.sqlite3"
c = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
c.row_factory = sqlite3.Row
for sid in (36675,36704,36736,36722,36679,36567):
    sk=c.execute("SELECT id,name,target_type_id,target_selection_id,target_relation_id,target_area_count,target_area_radius,aoe_shape_id FROM skills WHERE id=?", (sid,)).fetchone()
    print(dict(sk) if sk else sid)
# which templates cast those - casters from log
for nid in (18035,18034,17781,17775,17777,17783):
    n=c.execute("SELECT id,name,faction_id,aggression FROM npcs WHERE id=?", (nid,)).fetchone()
    print("npc", dict(n) if n else nid)
