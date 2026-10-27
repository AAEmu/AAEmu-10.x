"""How GP / Abyssal swirly gates are gated vs base worldgates."""
from __future__ import annotations

import sqlite3
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)

TIDS = (16739, 8583, 8594, 8592, 8593, 8595, 8596, 8351, 8532, 10521, 8033)


def name(tid):
    r = c.execute(
        "select en_us from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and idx=?",
        (tid,),
    ).fetchone()
    return r[0] if r else "?"


print("=== almighty + phases ===")
for tid in TIDS:
    a = c.execute(
        "select id, group_id, substr(model,1,100) from doodad_almighties where id=?",
        (tid,),
    ).fetchone()
    print(f"\n{tid} {name(tid)} {a}")
    phases = c.execute(
        "select id, doodad_func_group_kind_id, substr(model,1,90) "
        "from doodad_func_groups where doodad_almighty_id=? order by doodad_func_group_kind_id, id",
        (tid,),
    ).fetchall()
    for p in phases:
        print(f"  phase {p}")

print("\n=== game_schedule_doodads + schedule window ===")
for tid in TIDS:
    rows = c.execute(
        "select gsd.game_schedule_id, gs.start_time, gs.end_time, gs.start_time_min, gs.end_time_min, "
        "gs.st_year, gs.st_month, gs.st_day, gs.ed_year, gs.ed_month, gs.ed_day "
        "from game_schedule_doodads gsd "
        "left join game_schedules gs on gs.id=gsd.game_schedule_id "
        "where gsd.doodad_id=?",
        (tid,),
    ).fetchall()
    if rows:
        print(tid, name(tid), rows)

print("\n=== game_schedule_doodads mentioning battle / ship_grave / golden / abyssal names via join ===")
# schedules that touch any portal group 43 near these
for row in c.execute(
    """
    select gsd.doodad_id, gsd.game_schedule_id, da.group_id, lt.en_us, gs.start_time, gs.end_time
    from game_schedule_doodads gsd
    join doodad_almighties da on da.id=gsd.doodad_id
    left join localized_texts lt on lt.tbl_name='doodad_almighties' and lt.tbl_column_name='name' and lt.idx=da.id
    left join game_schedules gs on gs.id=gsd.game_schedule_id
    where da.group_id=43 and (
      lower(ifnull(lt.en_us,'')) like '%golden%'
      or lower(ifnull(lt.en_us,'')) like '%abyssal%'
      or lower(ifnull(lt.en_us,'')) like '%ethereal%'
      or lower(ifnull(lt.en_us,'')) like '%ship%'
      or da.id in (8583,8594,8592,8593,16739,8351,8532)
    )
    limit 40
    """
):
    print(row)

print("\n=== funcs on 16739 / 8594 / 8583 (first groups) ===")
for tid in (16739, 8594, 8583, 8532, 8351):
    print(f"-- {tid} {name(tid)}")
    for g in c.execute(
        "select id, doodad_func_group_kind_id from doodad_func_groups where doodad_almighty_id=?",
        (tid,),
    ):
        funcs = c.execute(
            "select id, actual_func_type, actual_func_id, next_phase "
            "from doodad_funcs where doodad_func_group_id=?",
            (g[0],),
        ).fetchall()
        print(f"  group {g}: {funcs[:8]}{'...' if len(funcs)>8 else ''}")
