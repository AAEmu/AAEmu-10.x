import pathlib
import re
import json
import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
print("item 38778", c.execute(
    "select en_us, ko from localized_texts where tbl_name='items' and tbl_column_name='name' and idx=38778"
).fetchone())
print("skill 31928", c.execute(
    "select en_us, ko from localized_texts where tbl_name='skills' and tbl_column_name='name' and idx=31928"
).fetchone())

# .g placements for conditional gates
type_re = re.compile(r"\btype\s+(1052[0-3])\b")
pos_re = re.compile(r"pos\s*\(\s*x\s+([-\d.]+)\s*,\s*y\s+([-\d.]+)\s*,\s*z\s+([-\d.]+)")
root = pathlib.Path(r"G:\AAchina\client\game\worlds\main_world\level_design\cells")
g_hits = []
for f in root.glob("*/doodad.g"):
    text = f.read_text(encoding="utf-8", errors="ignore")
    # blocks aren't always single-level {}; walk line-oriented
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        if lines[i].strip() == "doodad":
            block = []
            i += 1
            while i < len(lines) and not (lines[i].strip() == "doodad" and block):
                if lines[i].strip() == "doodad" and block:
                    break
                block.append(lines[i])
                i += 1
                if len(block) > 30:
                    break
            b = "\n".join(block)
            tm = type_re.search(b)
            pm = pos_re.search(b)
            if tm and pm:
                tid = int(tm.group(1))
                lx, ly, lz = map(float, pm.groups())
                cx, cy = map(int, f.parent.name.split("_"))
                g_hits.append((tid, cx * 1024 + lx, cy * 1024 + ly, lz, f.parent.name))
            continue
        i += 1

print("\n.g conditional worldgates:")
for h in sorted(g_hits):
    print(f"  {h[0]} @ ({h[1]:.3f},{h[2]:.3f},{h[3]:.3f}) cell {h[4]}")

# json always-open overlapping within 2m
p = pathlib.Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json")
data = json.loads(p.read_text(encoding="utf-8"))
json_open = []
for e in data:
    uid = e.get("UnitId") or e.get("unitId")
    if uid not in (6605, 8033, 8034):
        continue
    pos = e.get("Position") or {}
    json_open.append((uid, float(pos.get("X") or 0), float(pos.get("Y") or 0), float(pos.get("Z") or 0)))

print("\noverlaps (.g conditional vs json always-open within 3m):")
for gt, gx, gy, gz, cell in g_hits:
    for ju, jx, jy, jz in json_open:
        dx, dy = gx - jx, gy - jy
        if dx * dx + dy * dy < 9:
            print(f"  COLLISION .g {gt} @ ({gx:.2f},{gy:.2f}) vs json {ju} @ ({jx:.2f},{jy:.2f}) d={ (dx*dx+dy*dy)**0.5:.2f}")

# also check 10547/10548 near gate - dual state furniture?
for tid in (10044, 10209, 10547, 10548):
    a = c.execute("select id, group_id, model from doodad_almighties where id=?", (tid,)).fetchone()
    name = c.execute(
        "select en_us from localized_texts where tbl_name='doodad_almighties' and tbl_column_name='name' and idx=?",
        (tid,),
    ).fetchone()
    groups = c.execute(
        "select id, model, doodad_func_group_kind_id from doodad_func_groups where doodad_almighty_id=? order by id",
        (tid,),
    ).fetchall()
    print(f"\nnear-pad {tid} {name} almighty={a}")
    for g in groups:
        print(" ", g)
