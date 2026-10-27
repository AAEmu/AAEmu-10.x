"""Why 16739 / 8532 / 8583 missing under LevelPack plant rules."""
from __future__ import annotations

import math
import re
import sqlite3
from pathlib import Path

CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
LOG_OLD = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Logs\Server_2026-10-23_00.log"
)
LOG = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Logs\Server.log"
)

POS_RE = re.compile(
    r"pos\s*\(\s*x\s+([-\d.eE+]+)\s*,\s*y\s+([-\d.eE+]+)\s*,\s*z\s+([-\d.eE+]+)\s*\)"
)
TYPE_RE = re.compile(r"type\s+(\d+)")
SPLIT_RE = re.compile(r"(?m)^doodad\r?\n")
IGNORE_RE = re.compile(r"doodadType\s+(\d+)")


def parse_cell(cx, cy):
    cell = CELLS / f"{cx:03d}_{cy:03d}"
    f = cell / "doodad.g"
    if not f.exists():
        return [], set()
    ign = set()
    for p in cell.glob("*.g"):
        n = p.name.lower()
        if n.startswith("doodad_open") or ("ignore" in n and "doodad" in n):
            ign |= {int(m.group(1)) for m in IGNORE_RE.finditer(p.read_text(encoding="utf-8", errors="replace"))}
    text = f.read_text(encoding="utf-8", errors="replace")
    ox, oy = cx * 1024.0, cy * 1024.0
    rows = []
    for block in SPLIT_RE.split(text):
        tm, pm = TYPE_RE.search(block), POS_RE.search(block)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        x = ox + float(pm.group(1))
        y = oy + float(pm.group(2))
        z = float(pm.group(3))
        rows.append((tid, x, y, z, tid in ign))
    return rows, ign


def neighbors(rows, target_tid, metres=2.0):
    targets = [r for r in rows if r[0] == target_tid]
    out = []
    for tid, x, y, z, ign in targets:
        near = []
        for ot, ox, oy, oz, oign in rows:
            if ot == tid and abs(ox - x) < 1e-3 and abs(oy - y) < 1e-3:
                continue
            d = math.hypot(ox - x, oy - y)  # XY like plant? rules use XYZ
            d3 = math.sqrt((ox - x) ** 2 + (oy - y) ** 2 + (oz - z) ** 2)
            if d3 <= metres:
                near.append((ot, d3, ox, oy, oz, oign))
        out.append(((tid, x, y, z, ign), near))
    return out


def main():
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)

    def name(tid):
        r = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        return r[0] if r else "?"

    # tower almighty targets from compact if table exists
    print("=== tower doodad targets near 8532/8410 ===")
    try:
        for row in c.execute(
            "select tower_def_id, doodad_id from tower_def_prog_spawn_targets "
            "where doodad_id in (8532,8410,8411,8412,8413,8414,8745,8351) "
            "or target_id in (8532,8410,8411,8412,8413,8414)"
        ):
            print(row)
    except Exception as e:
        print("query fail", e)
        # list tables
        tabs = [
            r[0]
            for r in c.execute(
                "select name from sqlite_master where name like '%tower%doodad%' "
                "or name like '%prog%spawn%' or name like '%almighty%'"
            )
        ]
        print("tabs", tabs)

    rows_mari, _ = parse_cell(10, 11)
    print("\n=== within 2m of 16739 Ethereal Haven (XYZ DedupMetres) ===")
    for stand, near in neighbors(rows_mari, 16739, 2.0):
        print("stand", stand, name(stand[0]))
        for n in sorted(near, key=lambda t: t[1]):
            print(f"  occ {n[0]} d={n[1]:.2f} ignore={n[5]} {name(n[0])}")

    print("\n=== within 2m of 8594 Abyssal Attack WG ===")
    for stand, near in neighbors(rows_mari, 8594, 2.0):
        print("stand", stand, name(stand[0]))
        for n in sorted(near, key=lambda t: t[1]):
            print(f"  occ {n[0]} d={n[1]:.2f} ignore={n[5]} {name(n[0])}")

    rows_sea, _ = parse_cell(19, 20)
    print("\n=== within 2m of 8532 Ezi Light (kraken statue) ===")
    for stand, near in neighbors(rows_sea, 8532, 2.0):
        print("stand", stand, name(stand[0]))
        for n in sorted(near, key=lambda t: t[1]):
            print(f"  occ {n[0]} d={n[1]:.2f} ignore={n[5]} {name(n[0])}")

    # game_schedule for these tids
    print("\n=== game_schedule_doodads ===")
    for tid in (16739, 8583, 8594, 8532, 8351, 8410):
        rows = c.execute(
            "select game_schedule_id, doodad_id from game_schedule_doodads where doodad_id=?",
            (tid,),
        ).fetchall()
        print(tid, name(tid), rows)

    # MapDoodadPlant in rotated log
    print("\n=== log MapDoodad* ===")
    for p in (LOG_OLD, LOG):
        if not p.exists():
            continue
        hits = []
        with p.open(encoding="utf-8", errors="replace") as f:
            for line in f:
                if "MapDoodad" in line or "ZoneDoodadPlacementCatalog" in line:
                    hits.append(line.rstrip())
        print(p.name, "hits", len(hits))
        for h in hits[:30]:
            print(" ", h[:220])
        for h in hits[-10:]:
            print(" ", h[:220])


if __name__ == "__main__":
    main()
