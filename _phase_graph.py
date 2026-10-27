"""Phase graph for 16739 / 8594 / 8351 / 8532 — who advances to swirly."""
import sqlite3
from pathlib import Path

c = sqlite3.connect(r"file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)

for tid in (16739, 8594, 8583, 8351, 8532):
    print(f"\n======== {tid} ========")
    groups = {
        r[0]: r
        for r in c.execute(
            "select id, doodad_func_group_kind_id, substr(model,1,80) "
            "from doodad_func_groups where doodad_almighty_id=?",
            (tid,),
        )
    }
    for gid, g in groups.items():
        print(f"group {gid} kind={g[1]} model={g[2]}")
        for f in c.execute(
            "select id, actual_func_type, actual_func_id, next_phase "
            "from doodad_funcs where doodad_func_group_id=?",
            (gid,),
        ):
            print(f"  func {f}")
        # phase funcs
        for pf in c.execute(
            "select id, actual_func_type, actual_func_id "
            "from doodad_phase_funcs where doodad_func_group_id=?",
            (gid,),
        ):
            print(f"  phase_func {pf}")

# skills that hit these
print("\n=== skills targeting worldgate / ship_grave / golden plains names ===")
for row in c.execute(
    """
    select s.id, lt.en_us from skills s
    left join localized_texts lt on lt.tbl_name='skills' and lt.tbl_column_name='name' and lt.idx=s.id
    where lower(ifnull(lt.en_us,'')) like '%worldgate%'
       or lower(ifnull(lt.en_us,'')) like '%abyssal%'
       or lower(ifnull(lt.en_us,'')) like '%golden plain%'
       or lower(ifnull(lt.en_us,'')) like '%ethereal%'
       or lower(ifnull(lt.en_us,'')) like '%ship grave%'
       or s.id in (6257,6258,2228,2225,2214,2370,2371)
    limit 40
    """
):
    print(row)

# doodad_func_skill_hits for those func ids
print("\n=== doodad_func_skill_hits for portal funcs ===")
for fid in (6257, 6258, 2228, 2225, 2214, 2370, 2371, 2506):
    rows = c.execute("select * from doodad_func_skill_hits where id=?", (fid,)).fetchall()
    print(fid, rows)
