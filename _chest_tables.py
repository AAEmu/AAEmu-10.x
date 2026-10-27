import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
for t in (3484, 10712):
    print(t, c.execute("select id, group_id, client_doodad from doodad_almighties where id=?", (t,)).fetchone())

for r in c.execute(
    "select name from sqlite_master where type='table' and ("
    "name like '%treasure%' or name like '%chest%' or name like '%random%' "
    "or name like '%sunken%')"
):
    print("table", r[0])

# group 55-59 names
for g in range(55, 60):
    print("group", g, c.execute("select name from doodad_groups where id=?", (g,)).fetchone())
