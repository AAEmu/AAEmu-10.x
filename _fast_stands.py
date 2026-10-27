"""Stream-parse doodad_spawns.json; check mari + sea stands vs .g ignore."""
from __future__ import annotations

import math
import re
import sqlite3
from pathlib import Path

JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
IGNORE_ROOT = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design")

MARI = (11183.46, 12069.52)
SEA = (20104.7, 21012.9)  # player / silent sea — also tower test seed


def stream_json_stands():
    """Minimal streaming extract of UnitId + Position X/Y/Z from array of objects."""
    # file is ~24MB; regex over chunks is fine
    text = JSON.read_text(encoding="utf-8")
    # DoodadSpawner: "UnitId": N ... "X": ... "Y": ... "Z":
    for m in re.finditer(
        r'"UnitId"\s*:\s*(\d+)[\s\S]*?"X"\s*:\s*([-\d.]+)[\s\S]*?"Y"\s*:\s*([-\d.]+)[\s\S]*?"Z"\s*:\s*([-\d.]+)',
        text,
    ):
        yield int(m.group(1)), float(m.group(2)), float(m.group(3)), float(m.group(4))


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


def load_ignore_types():
    """Collect template ids listed in doodad_open_*/ignore_*doodad* under cells+zone."""
    ids = set()
    patterns = ("**/doodad_open_*.g", "**/ignore_*doodad*.g", "**/ignore_spawners*.g")
    files = []
    for pat in patterns:
        files.extend(IGNORE_ROOT.glob(pat))
        files.extend(Path(r"G:\AAchina\Server\game\worlds\main_world").glob(pat))
    print(f"ignore files {len(files)}", flush=True)
    for f in files:
        text = f.read_text(encoding="utf-8", errors="replace")
        for m in re.finditer(r"\btype\s+(\d+)", text):
            ids.add(int(m.group(1)))
        for m in re.finditer(r"\b(\d{3,5})\b", text):
            # also bare ids in ignore lists
            pass
        for m in re.finditer(r"spawnerType\s+(\d+)|doodadType\s+(\d+)|id\s+(\d+)", text):
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
            "select id,group_id,name,substr(model,1,80) from doodad_almighties where id=?",
            (tid,),
        ).fetchone()

    print("streaming json…", flush=True)
    mari = []
    sea = []
    statues = []
    n = 0
    for tid, x, y, z in stream_json_stands():
        n += 1
        if abs(x - MARI[0]) < 60 and abs(y - MARI[1]) < 60:
            mari.append((tid, x, y, z))
        if abs(x - SEA[0]) < 600 and abs(y - SEA[1]) < 600:
            sea.append((tid, x, y, z))
        nm = name(tid).lower()
        if "statue" in nm and ("nui" in nm or "nuia" in nm or "ezi" in nm or "large" in nm or "goddess" in nm):
            statues.append((tid, x, y, z, name(tid)))
        elif "sea of" in nm or "ship grave" in nm or "graveyard" in nm:
            statues.append((tid, x, y, z, name(tid)))
    print(f"json objects matched {n}", flush=True)

    print("\n=== MARI json ===")
    for tid, x, y, z in sorted(mari, key=lambda t: math.hypot(t[1] - MARI[0], t[2] - MARI[1])):
        print(f"  {tid} d={math.hypot(x-MARI[0],y-MARI[1]):.1f} {name(tid)} {info(tid)}")

    print("\n=== statue/graves name hits in json ===")
    for t in statues:
        print(t)

    print("\n=== SEA json landmarks (name filter) ===")
    for tid, x, y, z in sorted(sea, key=lambda t: math.hypot(t[1] - SEA[0], t[2] - SEA[1])):
        nm = name(tid)
        low = nm.lower()
        g = info(tid)
        if any(
            k in low
            for k in (
                "statue",
                "nui",
                "grave",
                "worldgate",
                "monument",
                "goddess",
                "memorial",
                "abyssal",
                "ship",
            )
        ):
            print(f"  {tid} d={math.hypot(x-SEA[0],y-SEA[1]):.0f} ({x:.1f},{y:.1f},{z:.1f}) {nm} {g}")

    # .g in mari + sea cells
    def cells(x0, y0, r):
        for cx in range(int((x0 - r) // 1024), int((x0 + r) // 1024) + 1):
            for cy in range(int((y0 - r) // 1024), int((y0 + r) // 1024) + 1):
                yield f"{cx:03d}_{cy:03d}"

    ignore = load_ignore_types()
    print(f"ignore type count {len(ignore)}", flush=True)

    for label, center, rad in (("MARI", MARI, 80), ("SEA", SEA, 500)):
        print(f"\n=== .g {label} ===")
        for cell in cells(center[0], center[1], rad):
            f = CELLS / cell / "doodad.g"
            if not f.exists():
                continue
            for tid, x, y, z in parse_g(f):
                if math.hypot(x - center[0], y - center[1]) > rad:
                    continue
                nm = name(tid)
                low = nm.lower()
                interesting = label == "MARI" or any(
                    k in low
                    for k in (
                        "statue",
                        "nui",
                        "grave",
                        "worldgate",
                        "ethereal",
                        "golden",
                        "abyssal",
                        "monument",
                        "goddess",
                        "memorial",
                    )
                )
                if label == "MARI" and math.hypot(x - center[0], y - center[1]) < 60:
                    interesting = True
                if not interesting and label == "SEA":
                    continue
                if label == "MARI" and math.hypot(x - center[0], y - center[1]) >= 60:
                    continue
                ign = tid in ignore
                print(
                    f"  {tid} d={math.hypot(x-center[0],y-center[1]):.1f} "
                    f"ignore={ign} ({x:.1f},{y:.1f},{z:.1f}) {nm} {info(tid)} cell={cell}"
                )


if __name__ == "__main__":
    main()
