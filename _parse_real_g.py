"""Parse doodad.g with the real paren-pos format the C# catalog uses."""
from __future__ import annotations

import math
import re
import sqlite3
from pathlib import Path

CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")

# paren form: pos ( x N, y N, z N )  OR brace form
POS_RE = re.compile(
    r"pos\s*(?:\(\s*x\s*([-\d.]+)\s*,\s*y\s*([-\d.]+)\s*,\s*z\s*([-\d.]+)\s*\)|"
    r"\{\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+))",
    re.I,
)
TYPE_RE = re.compile(r"\btype\s+(\d+)", re.I)
SPLIT_RE = re.compile(r"(?=^doodad\s*$)", re.M | re.I)
IGNORE_TYPE_RE = re.compile(r"doodadType\s+(\d+)|type\s+(\d+)", re.I)

MARI = (11183.46, 12069.52)
SEA = (20104.7, 21012.9)


def parse_file(path: Path, cell_x: int, cell_y: int):
    text = path.read_text(encoding="utf-8", errors="replace")
    ox, oy = cell_x * 1024.0, cell_y * 1024.0
    for block in SPLIT_RE.split(text):
        if not block.strip().lower().startswith("doodad"):
            continue
        tm = TYPE_RE.search(block)
        pm = POS_RE.search(block)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        if pm.group(1) is not None:
            lx, ly, lz = float(pm.group(1)), float(pm.group(2)), float(pm.group(3))
        else:
            lx, ly, lz = float(pm.group(4)), float(pm.group(5)), float(pm.group(6))
        yield tid, ox + lx, oy + ly, lz


def cell_ignore(cell_dir: Path) -> set[int]:
    ids = set()
    for f in cell_dir.iterdir():
        name = f.name.lower()
        if not name.endswith(".g"):
            continue
        if "doodad_open" in name or ("ignore" in name and "doodad" in name):
            text = f.read_text(encoding="utf-8", errors="replace")
            for m in IGNORE_TYPE_RE.finditer(text):
                for g in m.groups():
                    if g:
                        ids.add(int(g))
    return ids


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
            "select id,group_id,substr(model,1,70) from doodad_almighties where id=?",
            (tid,),
        ).fetchone()

    def scan(center, radius, label, name_filter=None):
        print(f"\n=== {label} .g within {radius}m ===", flush=True)
        cx0, cy0 = int(center[0] // 1024), int(center[1] // 1024)
        cells = []
        r_cells = int(radius // 1024) + 2
        for cx in range(cx0 - r_cells, cx0 + r_cells + 1):
            for cy in range(cy0 - r_cells, cy0 + r_cells + 1):
                cells.append((cx, cy))
        for cx, cy in cells:
            cell = CELLS / f"{cx:03d}_{cy:03d}"
            f = cell / "doodad.g"
            if not f.exists():
                continue
            ign = cell_ignore(cell)
            for tid, x, y, z in parse_file(f, cx, cy):
                d = math.hypot(x - center[0], y - center[1])
                if d > radius:
                    continue
                nm = name(tid)
                if name_filter and not any(k in nm.lower() for k in name_filter):
                    if label == "SEA" and d > 80:
                        continue
                    if label == "SEA":
                        pass  # still show close ones? only filtered
                        if d > 150:
                            continue
                marked = tid in ign
                print(
                    f"  {tid} d={d:.1f} ignore={marked} ({x:.1f},{y:.1f},{z:.1f}) "
                    f"{nm} {info(tid)} cell={cx:03d}_{cy:03d}"
                )

    scan(MARI, 40, "MARI")
    scan(
        SEA,
        500,
        "SEA",
        name_filter=(
            "statue",
            "nui",
            "nuia",
            "grave",
            "worldgate",
            "goddess",
            "monument",
            "memorial",
            "abyssal",
            "ezi",
        ),
    )

    # specifically 16739 / 8594 / 3665 anywhere in nearby cells
    print("\n=== force tids ===", flush=True)
    want = {16739, 8594, 8583, 3665, 15383, 15653, 15654, 4608}
    for cx in range(9, 22):
        for cy in range(10, 22):
            cell = CELLS / f"{cx:03d}_{cy:03d}"
            f = cell / "doodad.g"
            if not f.exists():
                continue
            ign = cell_ignore(cell)
            for tid, x, y, z in parse_file(f, cx, cy):
                if tid in want:
                    print(
                        f"  {tid} ignore={tid in ign} ({x:.1f},{y:.1f},{z:.1f}) "
                        f"{name(tid)} cell={cx:03d}_{cy:03d}"
                    )


if __name__ == "__main__":
    main()
