import math
import sqlite3
from pathlib import Path

db = Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\compact.sqlite3")
c = sqlite3.connect(db)
for tid in (8583, 16739, 8592, 8593):
    row = c.execute(
        "select id, group_id, name from doodad_almighties where id=?", (tid,)
    ).fetchone()
    loc = c.execute(
        "select en_us from localized_texts where tbl_name=? and tbl_column_name=? and idx=?",
        ("doodad_almighties", "name", tid),
    ).fetchone()
    print("tpl", tid, row, loc)

# distance math
gp = (11183.46, 12069.52, 145.46)
haven = (11183.55, 12068.78)  # from neighbor scan
d = math.hypot(gp[0] - haven[0], gp[1] - haven[1])
print(f"dist_xy GP8583 -> EtherealHaven16739 = {d:.3f} m")
print("orphan: group 43 PortalWorldgate would Plant if free stand")
print("reject: SkipStandOccupied (LevelPack already planted 16739 within CrossTemplateOccupy)")
