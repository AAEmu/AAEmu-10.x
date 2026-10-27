"""Why Marianople worldgate + Sea of Graves Nuia statue missing under LevelPack."""
from __future__ import annotations

import json
import math
import re
import sqlite3
from collections import defaultdict
from pathlib import Path

WORLDS = Path(r"G:\AAchina\Server\game\worlds\main_world")
CELLS = WORLDS / "level_design" / "cells"
JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
# client compact is authoritative for names; repo Data may be empty/stub
DB_CANDS = [
    Path(r"G:\AAchina\client\game\db\compact.sqlite3"),
    Path(r"G:\AAchina\Server\game\db\compact.sqlite3"),
    Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\compact.sqlite3"),
]

# Marianople plaza around GP/Ethereal
MARI = (11183.46, 12069.52)
# Sea of Graves - approximate from memory / search by name later
# Nuia continent sea - often around eastern waters; we'll find by name


def load_db():
    for p in DB_CANDS:
        if not p.exists():
            continue
        c = sqlite3.connect(f"file:{p}?mode=ro", uri=True)
        try:
            c.execute("select 1 from doodad_almighties limit 1")
            print("db", p)
            return c
        except Exception:
            c.close()
    raise SystemExit("no doodad_almighties db")


def loc_name(c, tid):
    row = c.execute(
        "select en_us, ko from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and idx=?",
        (tid,),
    ).fetchone()
    return row


def tpl(c, tid):
    return c.execute(
        "select id, group_id, name, model from doodad_almighties where id=?", (tid,)
    ).fetchone()


def parse_g_file(path: Path):
    """Yield (tid, x,y,z) world coords from one doodad.g (cell-local pos)."""
    m = re.search(r"cells[/\\](\d+)_(\d+)", str(path).replace("\\", "/"))
    if not m:
        return
    cx, cy = int(m.group(1)), int(m.group(2))
    text = path.read_text(encoding="utf-8", errors="replace")
    # blocks: doodad { type N ... pos { x y z } }
    for block in re.finditer(
        r"doodad\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}", text, re.I
    ):
        body = block.group(1)
        tm = re.search(r"\btype\s+(\d+)", body)
        pm = re.search(
            r"pos\s*\{\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)", body
        )
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        lx, ly, lz = float(pm.group(1)), float(pm.group(2)), float(pm.group(3))
        yield tid, cx * 1024 + lx, cy * 1024 + ly, lz


def load_g_index(wanted: set[int] | None = None, near=None, radius=80.0):
    """Scan all cells; optionally filter by tid set or near XY."""
    by_tid = defaultdict(list)
    near_hits = []
    files = list(CELLS.glob("*_*/doodad.g"))
    print(f"scanning {len(files)} doodad.g …", flush=True)
    for i, f in enumerate(files):
        if i and i % 200 == 0:
            print(f"  …{i}/{len(files)}", flush=True)
        for tid, x, y, z in parse_g_file(f):
            if wanted is not None and tid not in wanted and near is None:
                # still need near scan for neighbors — always collect if near
                pass
            if wanted is None or tid in wanted:
                by_tid[tid].append((x, y, z, str(f)))
            if near is not None:
                dx, dy = x - near[0], y - near[1]
                if dx * dx + dy * dy <= radius * radius:
                    near_hits.append((tid, x, y, z, str(f)))
    return by_tid, near_hits


def load_json():
    print("loading json…", flush=True)
    data = json.loads(JSON.read_text(encoding="utf-8"))
    # shape: list of {UnitId/Id, Position:{X,Y,Z}, …} or Spawns
    if isinstance(data, dict):
        rows = data.get("Spawns") or data.get("spawns") or data.get("Doodads") or []
    else:
        rows = data
    out = []
    for r in rows:
        tid = r.get("UnitId") or r.get("unitId") or r.get("TemplateId") or r.get("Id")
        pos = r.get("Position") or r.get("pos") or {}
        if isinstance(pos, dict):
            x = pos.get("X") or pos.get("x")
            y = pos.get("Y") or pos.get("y")
            z = pos.get("Z") or pos.get("z")
        else:
            continue
        if tid is None or x is None:
            continue
        out.append((int(tid), float(x), float(y), float(z)))
    print(f"json rows {len(out)}", flush=True)
    return out


def main():
    c = load_db()

    # find statue / nuia / sea of graves by name
    print("\n=== name hunt (statue / nuia / graves / worldgate) ===")
    q = """
    select idx, en_us, ko from localized_texts
    where tbl_name='doodad_almighties' and tbl_column_name='name'
      and (
        lower(en_us) like '%statue%'
        or lower(en_us) like '%nuia%'
        or lower(en_us) like '%grave%'
        or lower(en_us) like '%worldgate%'
        or lower(en_us) like '%ethereal%'
        or lower(en_us) like '%golden plain%'
        or ko like '%누이아%'
        or ko like '%동상%'
        or ko like '%묘%'
      )
    limit 80
    """
    for row in c.execute(q):
        t = tpl(c, row[0])
        print(row, "->", t)

    json_rows = load_json()

    # Marianople pad
    print("\n=== Marianople pad (±40m json) ===")
    for tid, x, y, z in json_rows:
        if abs(x - MARI[0]) < 40 and abs(y - MARI[1]) < 40:
            print(tid, f"{x:.2f},{y:.2f},{z:.2f}", loc_name(c, tid), tpl(c, tid))

    # Sea of Graves — find zone coords via worldgates named graves or large statue in eastern sea
    # Common AA Sea of Graves is west of Nuia / around Two Crowns-Golden — search json for large statue groups
    print("\n=== json stands with 'statue' or 'Nuia' in name ===")
    statue_tids = set()
    for row in c.execute(
        "select idx, en_us from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and (lower(en_us) like '%statue%' or lower(en_us) like '%nuia%statue%' "
        "or lower(en_us) like '%statue of nuia%' or lower(en_us) like '%goddess%')"
    ):
        statue_tids.add(row[0])
        print("name", row)

    # also group_id Level/Quest portals + big landmark doodads — scan json for statue tids
    print("\n=== json placements of statue tids ===")
    for tid, x, y, z in json_rows:
        if tid in statue_tids:
            print(tid, f"{x:.2f},{y:.2f},{z:.2f}", loc_name(c, tid))

    # scan .g for mari neighbors + 16739/8583 + statue tids
    wanted = {8583, 8592, 8593, 16739} | statue_tids
    by_tid, near = load_g_index(wanted=wanted, near=MARI, radius=60)
    print("\n=== .g near Marianople pad ===")
    for h in sorted(near, key=lambda t: (t[1] - MARI[0]) ** 2 + (t[2] - MARI[1]) ** 2)[:40]:
        tid, x, y, z, f = h
        d = math.hypot(x - MARI[0], y - MARI[1])
        print(f"  {tid} d={d:.2f} ({x:.2f},{y:.2f},{z:.2f})", loc_name(c, tid), Path(f).parent.name)

    print("\n=== .g stands for key tids ===")
    for tid in sorted(wanted):
        stands = by_tid.get(tid, [])
        if stands:
            print(tid, len(stands), "eg", stands[0][:3], loc_name(c, tid))

    # Sea of Graves region: look up zone - often around 9000-11000 x, 14000-16000 or similar
    # Prefer: find "Sea of Graves" return point / zone
    print("\n=== zone / return points mentioning graves ===")
    for tname in ("zones", "return_points", "world_zones"):
        try:
            cols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
            print(tname, cols[:12])
        except Exception:
            pass
    try:
        for row in c.execute(
            "select idx, en_us from localized_texts where lower(en_us) like '%sea of grave%' "
            "or lower(en_us) like '%graves%' limit 30"
        ):
            print("loc", row)
    except Exception as e:
        print(e)


if __name__ == "__main__":
    main()
