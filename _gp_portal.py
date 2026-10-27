import json
import pathlib
import re
import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")

print("=== Golden Plains / Battle Worldgate names ===")
for row in c.execute(
    """
    select idx, en_us, ko from localized_texts
    where tbl_name='doodad_almighties' and tbl_column_name='name'
      and (en_us like '%Golden Plains%' or en_us like '%Battle Worldgate%'
           or ko like '%황금 평원%')
    """
):
    tid = row[0]
    a = c.execute(
        "select id, group_id, model from doodad_almighties where id=?", (tid,)
    ).fetchone()
    print(tid, a, row[1])

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
        "select en_us from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and idx=?",
        (uid,),
    ).fetchone()
    nm = (name[0] if name else "") or ""
    if "Golden" in nm or "Worldgate" in nm or "Battle" in nm:
        hits.append((uid, round(x, 2), round(y, 2), round(z, 2), nm))

print("\n=== json in Marianople box ===")
for h in sorted(set(hits)):
    print(h)

root = pathlib.Path(r"G:\AAchina\client\game\worlds\main_world\level_design\cells")
ids = {h[0] for h in hits} | {8583, 8592, 8593, 8033, 10521, 10520}
type_re = re.compile(r"\btype\s+(\d+)\b")
pos_re = re.compile(r"pos\s*\(\s*x\s+([-\d.]+)\s*,\s*y\s+([-\d.]+)\s*,\s*z\s+([-\d.]+)")

print("\n=== .g Marianople stands ===")
for tid in sorted(ids):
    found = []
    for f in root.glob("*/doodad.g"):
        text = f.read_text(encoding="utf-8", errors="ignore")
        if not re.search(rf"\btype\s+{tid}\b", text):
            continue
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
            if not tm or int(tm.group(1)) != tid or not pm:
                continue
            lx, ly, lz = map(float, pm.groups())
            cx, cy = map(int, f.parent.name.split("_"))
            wx, wy = cx * 1024 + lx, cy * 1024 + ly
            if 10800 < wx < 11500 and 11600 < wy < 12300:
                found.append((round(wx, 2), round(wy, 2), round(lz, 2), f.parent.name))
    in_any = any(
        re.search(rf"\btype\s+{tid}\b", f.read_text(encoding="utf-8", errors="ignore"))
        for f in root.glob("*/doodad.g")
    )
    a = c.execute("select group_id from doodad_almighties where id=?", (tid,)).fetchone()
    name = c.execute(
        "select en_us from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and idx=?",
        (tid,),
    ).fetchone()
    print(f"tid={tid} group={a} in_any_g={in_any} mari_g={found} name={name}")
    for g in c.execute(
        "select id, doodad_func_group_kind_id, model from doodad_func_groups "
        "where doodad_almighty_id=? order by id",
        (tid,),
    ):
        print("  phase", g)
