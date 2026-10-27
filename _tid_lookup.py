"""Locate specific tids in json + .g + ignore lists — line-state parser, no catastrophic regex."""
from __future__ import annotations

import re
from pathlib import Path

JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
CELLS = Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
WORLD = Path(r"G:\AAchina\Server\game\worlds\main_world")

TIDS = {
    4608,  # Halcyona / Golden Plains worldgate
    4610,
    8583,
    8592,
    8593,
    16739,  # Ethereal Haven
    3665,  # Statue of Nui
    15383,
    15653,
    15654,
    4281,
    1014,
    14308,
    8594,
    8595,
    8596,  # Abyssal / ship grave battle gates
}


def stream_json():
    tid = None
    x = y = z = None
    depth_obj = 0
    in_pos = False
    with JSON.open("r", encoding="utf-8") as f:
        for line in f:
            # crude but works for pretty-printed AAEmu json
            if '"UnitId"' in line:
                m = re.search(r":\s*(\d+)", line)
                if m:
                    tid = int(m.group(1))
            if '"X"' in line and tid is not None:
                m = re.search(r":\s*([-\d.]+)", line)
                if m:
                    x = float(m.group(1))
            if '"Y"' in line and tid is not None and x is not None:
                m = re.search(r":\s*([-\d.]+)", line)
                if m:
                    y = float(m.group(1))
            if '"Z"' in line and tid is not None and x is not None and y is not None:
                m = re.search(r":\s*([-\d.]+)", line)
                if m:
                    z = float(m.group(1))
                    if tid in TIDS:
                        yield tid, x, y, z
                    tid = x = y = z = None


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
        if tid not in TIDS:
            continue
        lx, ly, lz = map(float, pm.groups())
        yield tid, cx * 1024 + lx, cy * 1024 + ly, lz, path.parent.name


def main():
    print("=== json ===", flush=True)
    for tid, x, y, z in stream_json():
        print(f"  {tid} ({x:.2f},{y:.2f},{z:.2f})", flush=True)

    print("=== .g ===", flush=True)
    files = list(CELLS.glob("*_*/doodad.g"))
    for i, f in enumerate(files):
        if i and i % 400 == 0:
            print(f"  …{i}/{len(files)}", flush=True)
        for row in parse_g(f):
            print(f"  {row[0]} ({row[1]:.2f},{row[2]:.2f},{row[3]:.2f}) {row[4]}", flush=True)

    print("=== ignore/open mentioning tids ===", flush=True)
    files = list(WORLD.glob("**/doodad_open_*.g")) + list(WORLD.glob("**/ignore_*doodad*.g"))
    print(f"files {len(files)}", flush=True)
    for f in files:
        text = f.read_text(encoding="utf-8", errors="replace")
        found = [t for t in TIDS if re.search(rf"\b{t}\b", text)]
        if found:
            rel = str(f.relative_to(WORLD))
            print(f"  {rel}: {found}", flush=True)


if __name__ == "__main__":
    main()
