import re
import sqlite3
from pathlib import Path

CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)

c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
print("=== zone names ===")
for row in c.execute(
    "select idx, en_us from localized_texts "
    "where tbl_column_name='name' and ("
    "lower(en_us) like '%grave%' or lower(en_us) like '%silent sea%' "
    "or lower(en_us) like '%drowned%' or lower(en_us) like '%abyssal%')"
    " limit 50"
):
    print(row)

print("=== statue-ish doodads ===")
for row in c.execute(
    "select id, group_id, name, substr(model,1,90) from doodad_almighties "
    "where model like '%ship_grave%' or model like '%ncom_statue%' "
    "or model like '%ncom_revival%' or model like '%event_statue%' "
    "or model like '%memorial_statue%' or name like '%거대%여신%'"
):
    print(row)

# json placements for promising tids
tids = {r[0] for r in c.execute(
    "select id from doodad_almighties where model like '%ncom_statue%' "
    "or model like '%ncom_revival%' or model like '%memorial_statue%' "
    "or model like '%event_statue%' or id in (3665,15383,15653,15654,17124,4281)"
)}
print("candidate tids", sorted(tids)[:40], "count", len(tids))

tid = x = y = z = None
print("=== json placements ===")
with JSON.open(encoding="utf-8") as f:
    for line in f:
        if '"UnitId"' in line:
            m = re.search(r":\s*(\d+)", line)
            tid = int(m.group(1)) if m else None
        elif '"X"' in line and tid in tids:
            x = float(re.search(r":\s*([-\d.]+)", line).group(1))
        elif '"Y"' in line and tid in tids and x is not None:
            y = float(re.search(r":\s*([-\d.]+)", line).group(1))
        elif '"Z"' in line and tid in tids and x is not None and y is not None:
            z = float(re.search(r":\s*([-\d.]+)", line).group(1))
            nm = c.execute(
                "select en_us from localized_texts where tbl_name='doodad_almighties' "
                "and tbl_column_name='name' and idx=?",
                (tid,),
            ).fetchone()
            print(tid, f"({x:.1f},{y:.1f},{z:.1f})", nm)
            tid = x = y = z = None
