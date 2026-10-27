import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
cols = [r[1] for r in c.execute("pragma table_info(tower_def_prog_spawn_targets)")]
print("cols", cols)
for tid in (8532, 8351, 8410, 8411, 8412, 8413, 8414, 8745):
    found = []
    for row in c.execute("select * from tower_def_prog_spawn_targets"):
        if tid in row:
            found.append(row)
    print(tid, "rows", len(found), found[:1])

# also spawn_target_type
try:
    for row in c.execute(
        "select spawn_target_type, spawn_target_id, count(*) from tower_def_prog_spawn_targets "
        "where spawn_target_id in (8532,8351,8410,8411,8412,8413,8414) group by 1,2"
    ):
        print(row)
except Exception as e:
    print(e)
