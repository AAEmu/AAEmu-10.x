import sqlite3

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
print("skill_effects", c.execute("pragma table_info(skill_effects)").fetchall())
print("sample", c.execute("select * from skill_effects limit 3").fetchall())
print(
    "gimmick tables",
    [t[0] for t in c.execute("select name from sqlite_master where name like '%gimmick%'").fetchall()],
)
print(
    "effects tables",
    [
        t[0]
        for t in c.execute(
            "select name from sqlite_master where type='table' and name like '%effect%'"
        ).fetchall()
    ][:50],
)
# dig from earlier: effects id 12893 actual_id 94 actual_type SpawnGimmickEffect — that was effects table?
print("effects?", [t[0] for t in c.execute("select name from sqlite_master where name='effects'").fetchall()])
if c.execute("select name from sqlite_master where name='effects'").fetchone():
    print("effects cols", c.execute("pragma table_info(effects)").fetchall())
    print("12893", c.execute("select * from effects where id=12893").fetchone())
