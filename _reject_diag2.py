"""Focused: Marianople worldgate + Sea of Graves statue near Silent Sea."""
from __future__ import annotations

import json
import math
import re
import sqlite3
from collections import defaultdict
from pathlib import Path

CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")

MARI = (11183.46, 12069.52)
# live player / Silent Sea area from aa_status
SEA = (20104.7, 21012.9)


def parse_g(path: Path):
    m = re.search(r"(\d+)_(\d+)", path.parent.name)
    if not m:
        return
    cx, cy = int(m.group(1)), int(m.group(2))
    text = path.read_text(encoding="utf-8", errors="replace")
    for block in re.finditer(
        r"doodad\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}", text, re.I
    ):
        body = block.group(1)
        tm = re.search(r"\btype\s+(\d+)", body)
        pm = re.search(r"pos\s*\{\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)", body)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        lx, ly, lz = map(float, pm.groups())
        yield tid, cx * 1024 + lx, cy * 1024 + ly, lz


def main():
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)

    def name(tid):
        r = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        return r[0] if r else "?"

    def info(tid):
        return c.execute(
            "select id,group_id,name,model from doodad_almighties where id=?", (tid,)
        ).fetchone()

    # name hunt
    print("=== name hunt ===")
    for row in c.execute(
        "select idx, en_us from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and ("
        "lower(en_us) like '%statue%' or lower(en_us) like '%nuia%' "
        "or lower(en_us) like '%grave%' or lower(en_us) like '%worldgate%' "
        "or lower(en_us) like '%ethereal%' or lower(en_us) like '%golden plain%'"
        ") order by en_us limit 100"
    ):
        print(row, info(row[0]))

    print("\nloading json…", flush=True)
    rows = json.loads(JSON.read_text(encoding="utf-8"))
    # DoodadSpawner list: UnitId + Position
    jnear_mari = []
    jnear_sea = []
    jstatues = []
    for r in rows:
        tid = int(r.get("UnitId") or 0)
        pos = r.get("Position") or {}
        x, y, z = float(pos.get("X", 0)), float(pos.get("Y", 0)), float(pos.get("Z", 0))
        if abs(x - MARI[0]) < 50 and abs(y - MARI[1]) < 50:
            jnear_mari.append((tid, x, y, z))
        if abs(x - SEA[0]) < 400 and abs(y - SEA[1]) < 400:
            jnear_sea.append((tid, x, y, z))
        n = name(tid).lower()
        if "statue" in n or "nuia" in n:
            jstatues.append((tid, x, y, z, name(tid)))

    print("\n=== json near Marianople ===")
    for tid, x, y, z in sorted(jnear_mari, key=lambda t: (t[1] - MARI[0]) ** 2 + (t[2] - MARI[1]) ** 2):
        d = math.hypot(x - MARI[0], y - MARI[1])
        print(f"  {tid} d={d:.1f} ({x:.1f},{y:.1f},{z:.1f}) {name(tid)} {info(tid)}")

    print("\n=== json statues (all) ===")
    for t in jstatues:
        print(t)

    print("\n=== json near Silent Sea player (±400m) — large/landmark-ish ===")
    # print worldgates + statues + anything with scale-looking names / group Level
    for tid, x, y, z in sorted(jnear_sea, key=lambda t: (t[1] - SEA[0]) ** 2 + (t[2] - SEA[1]) ** 2)[:80]:
        n = name(tid)
        g = info(tid)
        if any(
            k in n.lower()
            for k in ("statue", "nuia", "grave", "worldgate", "monument", "goddess", "memorial")
        ) or (g and g[1] in (22, 43, 44, 45, 46)):  # Level / portals common
            d = math.hypot(x - SEA[0], y - SEA[1])
            print(f"  {tid} d={d:.0f} ({x:.1f},{y:.1f},{z:.1f}) {n} {g}")

    # scan only cells covering mari + sea boxes
    def cell_range(x0, y0, r):
        cells = []
        for cx in range(int((x0 - r) // 1024), int((x0 + r) // 1024) + 1):
            for cy in range(int((y0 - r) // 1024), int((y0 + r) // 1024) + 1):
                cells.append(f"{cx:03d}_{cy:03d}")
        return cells

    print("\n=== .g scan Marianople cells ===")
    for cell in cell_range(MARI[0], MARI[1], 80):
        f = CELLS / cell / "doodad.g"
        if not f.exists():
            continue
        hits = []
        for tid, x, y, z in parse_g(f):
            if math.hypot(x - MARI[0], y - MARI[1]) < 60:
                hits.append((tid, x, y, z))
        if hits:
            print(cell, "hits", len(hits))
            for tid, x, y, z in sorted(hits, key=lambda t: math.hypot(t[1] - MARI[0], t[2] - MARI[1]))[:25]:
                d = math.hypot(x - MARI[0], y - MARI[1])
                print(f"    {tid} d={d:.1f} ({x:.1f},{y:.1f}) {name(tid)} {info(tid)}")

    print("\n=== .g scan Silent Sea / Graves cells (±500m) ===")
    for cell in cell_range(SEA[0], SEA[1], 500):
        f = CELLS / cell / "doodad.g"
        if not f.exists():
            continue
        hits = []
        for tid, x, y, z in parse_g(f):
            n = name(tid).lower()
            g = info(tid)
            if math.hypot(x - SEA[0], y - SEA[1]) > 500:
                continue
            if any(k in n for k in ("statue", "nuia", "grave", "monument", "goddess", "memorial")):
                hits.append((tid, x, y, z, name(tid), g))
        if hits:
            print(cell)
            for h in hits:
                tid, x, y, z, n, g = h
                d = math.hypot(x - SEA[0], y - SEA[1])
                print(f"    {tid} d={d:.0f} ({x:.1f},{y:.1f},{z:.1f}) {n} {g}")

    # also dump ALL json near sea with group_id looking landmark (Level=22?)
    print("\n=== group ids of interest near sea (json, all within 200m) ===")
    for tid, x, y, z in jnear_sea:
        if math.hypot(x - SEA[0], y - SEA[1]) > 200:
            continue
        g = info(tid)
        print(f"  {tid} ({x:.1f},{y:.1f}) {name(tid)} {g}")


if __name__ == "__main__":
    main()
