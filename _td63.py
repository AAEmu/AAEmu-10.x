import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
cols = [x[1] for x in c.execute("pragma table_info(tower_defs)")]
row = c.execute("select * from tower_defs where id=63").fetchone()
print("tower_defs 63:")
if row:
    for k, v in zip(cols, row):
        print(f"  {k}={v}")

# schedules
for sql in [
    "select name from sqlite_master where type='table' and name like '%tower%'",
    "select name from sqlite_master where type='table' and name like '%schedule%'",
]:
    print(sql, c.execute(sql).fetchall())

# common link tables
for t in (
    "game_schedule_tower_defs",
    "tower_def_progs",
    "tower_defs",
):
    try:
        print(t, [x[1] for x in c.execute(f"pragma table_info({t})")])
    except Exception as e:
        print(t, e)

try:
    rows = c.execute(
        "select * from game_schedule_tower_defs where tower_def_id=63"
    ).fetchall()
    print("game_schedule_tower_defs", rows)
except Exception as e:
    print("gst", e)

# find schedule rows by scanning
for t, col in [
    ("game_schedules", None),
]:
    cols2 = [x[1] for x in c.execute(f"pragma table_info({t})")]
    print("game_schedules cols", cols2[:20])

# localized
try:
    for lang in ("ko", "en_us", "zh_cn"):
        pass
    ltcols = [x[1] for x in c.execute("pragma table_info(localized_texts)")]
    print("lt cols sample", ltcols[:15])
    rows = c.execute(
        "select id, tbl_name, tbl_column_name, idx, ko, en_us from localized_texts "
        "where tbl_name like '%tower%' and idx=63 limit 10"
    ).fetchall()
    print("localized", rows)
except Exception as e:
    print("loc err", e)

# compare 36
row36 = c.execute("select id, name, target_npc_spawner_id from tower_defs where id in (36,37,22,63)").fetchall()
print("compare", row36)
c.close()
