#!/usr/bin/env python3
"""Recheck purchase / weather / spyglass / wreck / misc SkipRandom buckets."""
from __future__ import annotations

import json
import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
JSON = Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
OUT = Path(__file__).with_name("_skip_random_recheck.txt")


def main() -> None:
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
    lines: list[str] = []

    def p(*a) -> None:
        lines.append(" ".join(str(x) for x in a))

    # weather hub 2768
    for tid in (2768, 3225, 3085, 3086, 3226):
        name = c.execute(
            "select id, name, group_id, model from doodad_almighties where id=?", (tid,)
        ).fetchone()
        p("weather tpl", name)
        # all ratio_respawn targets FROM this template's phases
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        for gid in gids:
            for ft, fid in c.execute(
                "select actual_func_type, actual_func_id from doodad_phase_funcs "
                "where doodad_func_group_id=?",
                (gid,),
            ):
                if ft == "DoodadFuncRatioRespawn":
                    row = c.execute(
                        "select ratio, spawn_doodad_id from doodad_func_ratio_respawns where id=?",
                        (fid,),
                    ).fetchone()
                    tgt = c.execute(
                        "select name from doodad_almighties where id=?", (row[1],)
                    ).fetchone()
                    p("  ratio from", tid, "gid", gid, "->", row, tgt)

    # who ratio-respawns INTO 3225/3085/3086/3226?
    p("=== who spawns weather templates via RatioRespawn ===")
    for tid in (3225, 3085, 3086, 3226, 2768):
        rows = c.execute(
            "select r.id, r.ratio, g.doodad_almighty_id, a.name "
            "from doodad_func_ratio_respawns r "
            "join doodad_phase_funcs pf on pf.actual_func_type='DoodadFuncRatioRespawn' "
            "and pf.actual_func_id=r.id "
            "join doodad_func_groups g on g.id=pf.doodad_func_group_id "
            "join doodad_almighties a on a.id=g.doodad_almighty_id "
            "where r.spawn_doodad_id=?",
            (tid,),
        ).fetchall()
        p(" into", tid, "from", rows[:20], "n=", len(rows))

    # buffs 1917/1918
    for bid in (1917, 1918):
        p("buff", c.execute("select id, name from buffs where id=?", (bid,)).fetchone())

    # spyglass timer
    row = c.execute("select * from doodad_func_timers where id=7031").fetchone()
    cols = [x[1] for x in c.execute("pragma table_info(doodad_func_timers)")]
    p("spyglass timer", dict(zip(cols, row)) if row else None)
    # InteractionEffect 3456
    try:
        cols = [x[1] for x in c.execute("pragma table_info(interaction_effects)")]
        row = c.execute("select * from interaction_effects where id=3456").fetchone()
        p("interaction_effect 3456", dict(zip(cols, row)) if row else None)
    except Exception as e:
        p("ie err", e)

    # quest links to 6129?
    try:
        cols = [x[1] for x in c.execute("pragma table_info(quest_act_obj_interactions)")]
        p("qaoi cols", cols)
        q = c.execute(
            "select * from quest_act_obj_interactions where doodad_id=6129 limit 5"
        ).fetchall()
        p("quest_act_obj_interactions 6129", q)
    except Exception as e:
        p("qaoi err", e)
    # any quest component mentioning?
    # skill use on spyglass - public transport?
    p(
        "buff 8779",
        c.execute("select id, name from buffs where id=8779").fetchone(),
    )

    # purchase: are ALL purchase templates in skip set housing-deco related?
    purch_tids = [
        r[0]
        for r in c.execute(
            "select distinct a.id from doodad_funcs df "
            "join doodad_func_groups g on g.id=df.doodad_func_group_id "
            "join doodad_almighties a on a.id=g.doodad_almighty_id "
            "where df.actual_func_type='DoodadFuncPurchase'"
        )
    ]
    p("all purchase templates count", len(purch_tids))
    # how many of skip purchase are near mirage (3600-4700)?
    data = json.loads(JSON.read_text(encoding="utf-8"))
    by_tid: dict[int, list[tuple[float, float, float]]] = defaultdict(list)
    for e in data:
        u = e.get("UnitId") or e.get("unitId")
        if not u:
            continue
        pos = e.get("Position") or e.get("position") or {}
        x = pos.get("X") if pos.get("X") is not None else pos.get("x")
        y = pos.get("Y") if pos.get("Y") is not None else pos.get("y")
        z = pos.get("Z") if pos.get("Z") is not None else pos.get("z")
        if x is None:
            continue
        by_tid[int(u)].append((float(x), float(y), float(z)))

    purchase_skip = []
    for tid in purch_tids:
        pts = by_tid.get(tid, [])
        if not pts:
            continue
        # classify region
        mirage = sum(1 for x, y, _ in pts if 3000 < x < 5000 and 4000 < y < 5500)
        purchase_skip.append((tid, len(pts), mirage))
    p("purchase templates with json stands:", len(purchase_skip))
    p("  all-in-mirage", sum(1 for _, n, m in purchase_skip if m == n and n > 0))
    p("  sample", purchase_skip[:10])

    # spyglass stands XY
    pts = by_tid.get(6129, [])
    p("spyglass 6129 stands", len(pts), "sample", pts[:8])

    # salvage wreck templates detail
    for tid in (10626, 8787, 8334, 8336, 10647):
        name = c.execute(
            "select id,name,group_id from doodad_almighties where id=?", (tid,)
        ).fetchone()
        p("wreck", name, "json", len(by_tid.get(tid, [])))
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        func_types = Counter()
        for gid in gids:
            for (ft,) in c.execute(
                "select actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (gid,),
            ):
                func_types[ft] += 1
            for (ft,) in c.execute(
                "select actual_func_type from doodad_phase_funcs where doodad_func_group_id=?",
                (gid,),
            ):
                func_types[f"phase:{ft}"] += 1
        p("  funcs", dict(func_types))

    # other + no_funcs + invisible + fakeuse_other from classify
    other_tids = [
        12363, 10808, 5791, 12367, 12356, 5921, 5890, 12365, 12364, 12362,
        12360, 12358, 10807, 9437, 12357, 4543, 9374, 8802, 12100, 12101,
        9371, 9373, 11664, 12054, 12055, 12372, 8590, 8786, 2841, 2612,
        5930, 5789, 5107, 5144, 8782, 5788, 4893, 6301, 10865, 12443,
        5103, 5149, 6256, 7407, 5790, 5795, 5794, 5793, 5792, 10649, 2301,
    ]
    p("=== misc templates ===")
    for tid in other_tids:
        row = c.execute(
            "select id, name, group_id, ifnull(model,'') from doodad_almighties where id=?",
            (tid,),
        ).fetchone()
        if not row:
            continue
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        ftypes = []
        for gid in gids:
            ftypes += [
                r[0]
                for r in c.execute(
                    "select actual_func_type from doodad_funcs where doodad_func_group_id=?",
                    (gid,),
                )
            ]
            ftypes += [
                "p:" + r[0]
                for r in c.execute(
                    "select actual_func_type from doodad_phase_funcs where doodad_func_group_id=?",
                    (gid,),
                )
            ]
        pts = by_tid.get(tid, [])
        p(
            " ",
            tid,
            row[1][:40] if row[1] else "?",
            "g",
            row[2],
            "n",
            len(pts),
            "f",
            Counter(ftypes).most_common(6),
            "m",
            (row[3] or "")[:50],
        )

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {OUT} lines={len(lines)}")


if __name__ == "__main__":
    main()
