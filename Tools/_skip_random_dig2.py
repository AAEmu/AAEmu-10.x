#!/usr/bin/env python3
"""Finals / weather / purchase decorations for SkipRandom."""
from __future__ import annotations

import json
import sqlite3
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
OUT = Path(__file__).with_name("_skip_random_dig2.txt")
JPATH = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)


def main() -> None:
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
    lines: list[str] = []

    def p(*a) -> None:
        lines.append(" ".join(str(x) for x in a))

    for fid in (4460, 1655):
        cols = [x[1] for x in c.execute("pragma table_info(doodad_func_finals)")]
        row = c.execute("select * from doodad_func_finals where id=?", (fid,)).fetchone()
        p("final", fid, dict(zip(cols, row)) if row else None)

    for tid in (10712, 3484, 3225, 3085):
        gs = c.execute(
            "select game_schedule_id from game_schedule_doodads where doodad_id=?",
            (tid,),
        ).fetchall()
        p("schedule", tid, gs)

    for t in ("item_housing_decorations", "housing_decorations"):
        cols = [x[1] for x in c.execute(f"pragma table_info({t})")]
        p(t, cols)
        p(" count", c.execute(f"select count(*) from {t}").fetchone())
        p(" sample3", c.execute(f"select * from {t} limit 3").fetchall())

    p(
        "ihd items 36163/36164/36454",
        c.execute(
            "select * from item_housing_decorations where item_id in (36163,36164,36454,36455)"
        ).fetchall(),
    )

    hd_cols = [x[1] for x in c.execute("pragma table_info(housing_decorations)")]
    for col in hd_cols:
        if "doodad" in col.lower():
            hits = c.execute(
                f"select * from housing_decorations where {col}=10734 limit 2"
            ).fetchall()
            p("hd by", col, hits)

    # purchase item -> does it reference the same doodad as placeable?
    # housing_decorations often has doodad_id
    if "doodad_id" in hd_cols:
        for did in (10734, 10763, 10740):
            rows = c.execute(
                "select * from housing_decorations where doodad_id=?", (did,)
            ).fetchall()
            p("hd doodad_id", did, rows)

    try:
        p("item cat 7", c.execute("select id, name from item_categories where id=7").fetchall())
    except Exception as e:
        p("item cat err", e)

    for tid in (3225, 3085, 3086, 3226):
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        for gid in gids:
            df = c.execute(
                "select actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            pf = c.execute(
                "select actual_func_type from doodad_phase_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            p("weather", tid, "gid", gid, "f", df, "p", pf)

    p("climates", c.execute("select * from climates").fetchall())

    for rid in (1003, 1004):
        cols = [x[1] for x in c.execute("pragma table_info(doodad_func_ratio_changes)")]
        row = c.execute(
            "select * from doodad_func_ratio_changes where id=?", (rid,)
        ).fetchone()
        p("ratio_change", rid, dict(zip(cols, row)) if row else None)

    data = json.loads(JPATH.read_text(encoding="utf-8"))
    zs = []
    for e in data:
        u = e.get("UnitId") or e.get("unitId")
        if u != 10712:
            continue
        pos = e.get("Position") or e.get("position") or {}
        z = pos.get("Z") if pos.get("Z") is not None else pos.get("z")
        if z is not None:
            zs.append(float(z))
    if zs:
        under = sum(1 for z in zs if z < 100)
        p(
            "10712 json z n=",
            len(zs),
            "z<100",
            under,
            "z>=100",
            len(zs) - under,
            "min",
            min(zs),
            "max",
            max(zs),
        )

    for tid in (10763, 10734, 3225, 2831, 10712):
        pts = []
        for e in data:
            u = e.get("UnitId") or e.get("unitId")
            if u != tid:
                continue
            pos = e.get("Position") or e.get("position") or {}
            x = pos.get("X") if pos.get("X") is not None else pos.get("x")
            y = pos.get("Y") if pos.get("Y") is not None else pos.get("y")
            if x is not None:
                pts.append((round(float(x), 0), round(float(y), 0)))
        p(
            "tid",
            tid,
            "stands",
            len(pts),
            "unique_xy~1m",
            len(set(pts)),
            "sample",
            pts[:5],
        )

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {OUT} lines={len(lines)}")


if __name__ == "__main__":
    main()
