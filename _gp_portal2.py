import json
import pathlib
import re
import sqlite3
from collections import defaultdict

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")

print("=== Golden Plains names ===")
for row in c.execute(
    "select idx, en_us from localized_texts where tbl_name=? and tbl_column_name=? and en_us like ?",
    ("doodad_almighties", "name", "%Golden Plains%"),
):
    a = c.execute("select group_id from doodad_almighties where id=?", (row[0],)).fetchone()
    print(row[0], a, row[1])

data = json.loads(
    pathlib.Path(
        r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
    ).read_text(encoding="utf-8")
)

hits = []
for e in data:
    uid = e.get("UnitId") or e.get("unitId")
    if not uid:
        continue
    pos = e.get("Position") or {}
    x = float(pos.get("X") or 0)
    y = float(pos.get("Y") or 0)
    z = float(pos.get("Z") or 0)
    if not (10800 < x < 11500 and 11600 < y < 12300):
        continue
    name = c.execute(
        "select en_us from localized_texts where tbl_name=? and tbl_column_name=? and idx=?",
        ("doodad_almighties", "name", uid),
    ).fetchone()
    nm = (name[0] if name else "") or ""
    if "Golden" in nm or "Worldgate" in nm or "Battle" in nm:
        hits.append((uid, round(x, 2), round(y, 2), round(z, 2), nm))

print("json mari", sorted(set(hits)))

root = pathlib.Path(r"G:\AAchina\client\game\worlds\main_world\level_design\cells")
want = {h[0] for h in hits} | {8583, 8592, 8593}
g_any = defaultdict(int)
g_mari = defaultdict(list)
type_re = re.compile(r"\btype\s+(\d+)\b")
pos_re = re.compile(r"pos\s*\(\s*x\s+([-\d.]+)\s*,\s*y\s+([-\d.]+)\s*,\s*z\s+([-\d.]+)")

for f in root.glob("*/doodad.g"):
    text = f.read_text(encoding="utf-8", errors="ignore")
    if not any(f"type {t}" in text for t in want):
        continue
    for tid in want:
        if f"type {tid}" in text:
            g_any[tid] += 1
    lines = text.splitlines()
    i = 0
    cx, cy = map(int, f.parent.name.split("_"))
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
        if tid not in want:
            continue
        lx, ly, lz = map(float, pm.groups())
        wx, wy = cx * 1024 + lx, cy * 1024 + ly
        if 10800 < wx < 11500 and 11600 < wy < 12300:
            g_mari[tid].append((round(wx, 2), round(wy, 2), round(lz, 2), f.parent.name))

print("g_any_files", dict(g_any))
print("g_mari", dict(g_mari))
for tid in sorted(want):
    a = c.execute("select group_id from doodad_almighties where id=?", (tid,)).fetchone()
    phases = c.execute(
        "select id, doodad_func_group_kind_id, substr(model,1,60) from doodad_func_groups "
        "where doodad_almighty_id=? order by id",
        (tid,),
    ).fetchall()
    print("tid", tid, "group", a, "nphases", len(phases))
    for p in phases:
        print(" ", p)
