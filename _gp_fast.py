import json
import pathlib
import re
import sqlite3
from collections import defaultdict

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
ids = (8583, 8592, 8593)

print("=== templates ===")
for tid in ids:
    a = c.execute("select id, group_id, model from doodad_almighties where id=?", (tid,)).fetchone()
    nm = c.execute(
        "select en_us from localized_texts where tbl_name=? and tbl_column_name=? and idx=?",
        ("doodad_almighties", "name", tid),
    ).fetchone()
    print(tid, a, nm)
    for g in c.execute(
        "select id, doodad_func_group_kind_id, model from doodad_func_groups where doodad_almighty_id=? order by id",
        (tid,),
    ):
        print("  phase", g)

# json stands for these ids
data = json.loads(
    pathlib.Path(
        r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
    ).read_text(encoding="utf-8")
)
print("\n=== all json stands ===")
for e in data:
    uid = e.get("UnitId") or e.get("unitId")
    if uid not in ids:
        continue
    pos = e.get("Position") or {}
    x, y, z = float(pos.get("X") or 0), float(pos.get("Y") or 0), float(pos.get("Z") or 0)
    print(uid, round(x, 2), round(y, 2), round(z, 2))

# .g: one pass, only these types
root = pathlib.Path(r"G:\AAchina\client\game\worlds\main_world\level_design\cells")
g = defaultdict(list)
type_re = re.compile(r"\btype\s+(\d+)\b")
pos_re = re.compile(r"pos\s*\(\s*x\s+([-\d.]+)\s*,\s*y\s+([-\d.]+)\s*,\s*z\s+([-\d.]+)")
for f in root.glob("*/doodad.g"):
    text = f.read_text(encoding="utf-8", errors="ignore")
    if not any(f"type {t}" in text for t in ids):
        continue
    cx, cy = map(int, f.parent.name.split("_"))
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        if lines[i].strip() != "doodad":
            i += 1
            continue
        block = []
        i += 1
        while i < len(lines) and lines[i].strip() != "doodad":
            block.append(lines[i])
            i += 1
            if len(block) > 40:
                break
        b = "\n".join(block)
        tm = type_re.search(b)
        pm = pos_re.search(b)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        if tid not in ids:
            continue
        lx, ly, lz = map(float, pm.groups())
        g[tid].append((round(cx * 1024 + lx, 2), round(cy * 1024 + ly, 2), round(lz, 2), f.parent.name))

print("\n=== .g stands ===")
for tid in ids:
    print(tid, g.get(tid, []))

# neighbors within 3m of each json mari stand for occupancy
print("\n=== neighbors within 3m of GP json in mari box ===")
mari = [
    (uid, x, y, z)
    for e in data
    for uid in [e.get("UnitId") or e.get("unitId")]
    if uid in ids
    for pos in [e.get("Position") or {}]
    for x, y, z in [(float(pos.get("X") or 0), float(pos.get("Y") or 0), float(pos.get("Z") or 0))]
    if 10800 < x < 11500 and 11600 < y < 12300
]
# also load .g neighbors around those points from cell 010_011 mainly
cell = root / "010_011" / "doodad.g"
text = cell.read_text(encoding="utf-8", errors="ignore") if cell.exists() else ""
cx, cy = 10, 11
near = []
if text:
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        if lines[i].strip() != "doodad":
            i += 1
            continue
        block = []
        i += 1
        while i < len(lines) and lines[i].strip() != "doodad":
            block.append(lines[i])
            i += 1
            if len(block) > 40:
                break
        b = "\n".join(block)
        tm = type_re.search(b)
        pm = pos_re.search(b)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        lx, ly, lz = map(float, pm.groups())
        wx, wy = cx * 1024 + lx, cy * 1024 + ly
        for uid, x, y, z in mari:
            dx, dy = wx - x, wy - y
            if dx * dx + dy * dy < 25:
                nm = c.execute(
                    "select en_us from localized_texts where tbl_name=? and tbl_column_name=? and idx=?",
                    ("doodad_almighties", "name", tid),
                ).fetchone()
                near.append((uid, "near .g", tid, round(wx, 2), round(wy, 2), round((dx * dx + dy * dy) ** 0.5, 2), nm))

# json neighbors
for uid, x, y, z in mari:
    for e in data:
        ou = e.get("UnitId") or e.get("unitId")
        if not ou or ou == uid:
            continue
        pos = e.get("Position") or {}
        ox, oy = float(pos.get("X") or 0), float(pos.get("Y") or 0)
        dx, dy = ox - x, oy - y
        if dx * dx + dy * dy < 25:
            nm = c.execute(
                "select en_us from localized_texts where tbl_name=? and tbl_column_name=? and idx=?",
                ("doodad_almighties", "name", ou),
            ).fetchone()
            near.append((uid, "near json", ou, round(ox, 2), round(oy, 2), round((dx * dx + dy * dy) ** 0.5, 2), nm))

for n in near:
    print(n)

print("\norphan rule: group 43 PortalWorldgate plants IF not templateInLevelPack and not standOccupied")
print("templateInLevelPack = tid appears anywhere in .g:", {tid: bool(g.get(tid)) for tid in ids})
