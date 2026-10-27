import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
for tid in (3484, 10712, 11665, 877, 878):
    print("\n===", tid, "===")
    print(c.execute("select id, group_id, model from doodad_almighties where id=?", (tid,)).fetchone())
    groups = c.execute(
        "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
    ).fetchall()
    for (gid,) in groups:
        for f in c.execute(
            "select actual_func_type, actual_func_id from doodad_funcs where doodad_func_group_id=?",
            (gid,),
        ):
            print(" ", gid, f)
# any table linking treasure dig markers
for name in c.execute(
    "select name from sqlite_master where type='table' and name like '%doodad%'"
):
    n = name[0]
    if "spawn" in n or "treasure" in n or "random" in n or "sunken" in n:
        print("interesting", n)
