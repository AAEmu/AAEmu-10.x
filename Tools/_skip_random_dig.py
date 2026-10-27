#!/usr/bin/env python3
"""Dig sunken / weather / purchase ownership for SkipRandomPlacement."""
from __future__ import annotations

import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
OUT = Path(__file__).with_name("_skip_random_dig.txt")


def main() -> None:
    lines: list[str] = []
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)

    def p(*a):
        lines.append(" ".join(str(x) for x in a))

    # --- sunken 10712 / 3484 chain ---
    for tid in (10712, 3484):
        name = c.execute("select name from doodad_almighties where id=?", (tid,)).fetchone()
        p("=== template", tid, name)
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        p("  groups", gids)
        for gid in gids:
            kind = c.execute(
                "select doodad_func_group_kind_id from doodad_func_groups where id=?",
                (gid,),
            ).fetchone()
            df = c.execute(
                "select actual_func_type, actual_func_id, next_phase, func_skill_id "
                "from doodad_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            pf = c.execute(
                "select actual_func_type, actual_func_id "
                "from doodad_phase_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            p("  gid", gid, "kind", kind, "funcs", df, "phase", pf)
            for ft, fid, *_ in df:
                if ft == "DoodadFuncFakeUse":
                    row = c.execute(
                        "select * from doodad_func_fake_uses where id=?", (fid,)
                    ).fetchone()
                    cols = [x[1] for x in c.execute("pragma table_info(doodad_func_fake_uses)")]
                    p("    fake", dict(zip(cols, row)))
                if ft == "DoodadFuncLootPack":
                    row = c.execute(
                        "select * from doodad_func_loot_packs where id=?", (fid,)
                    ).fetchone()
                    p("    loot", row)
            for ft, fid in pf:
                tbl = {
                    "DoodadFuncRespawn": "doodad_func_respawns",
                    "DoodadFuncTimer": "doodad_func_timers",
                    "DoodadFuncRatioRespawn": "doodad_func_ratio_respawns",
                    "DoodadFuncClimateReact": "doodad_func_climate_reacts",
                }.get(ft)
                if tbl:
                    try:
                        row = c.execute(f"select * from {tbl} where id=?", (fid,)).fetchone()
                        cols = [x[1] for x in c.execute(f"pragma table_info({tbl})")]
                        p("    phase", ft, dict(zip(cols, row)) if row else None)
                    except Exception as e:
                        p("    phase err", ft, e)

    # walk next phases from 30180 / 8357
    for start in (30180, 8357, 6495, 6489):
        seen = set()
        q = [start]
        p("--- walk from", start)
        while q:
            gid = q.pop(0)
            if gid in seen or gid is None or gid < 0:
                continue
            seen.add(gid)
            al = c.execute(
                "select doodad_almighty_id, doodad_func_group_kind_id from doodad_func_groups where id=?",
                (gid,),
            ).fetchone()
            df = c.execute(
                "select actual_func_type, actual_func_id, next_phase from doodad_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            pf = c.execute(
                "select actual_func_type, actual_func_id from doodad_phase_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            p("  ", gid, "al", al, "f", df, "p", pf)
            for ft, fid in pf:
                tbl = {
                    "DoodadFuncRespawn": "doodad_func_respawns",
                    "DoodadFuncTimer": "doodad_func_timers",
                    "DoodadFuncRatioRespawn": "doodad_func_ratio_respawns",
                }.get(ft)
                if tbl:
                    row = c.execute(f"select * from {tbl} where id=?", (fid,)).fetchone()
                    cols = [x[1] for x in c.execute(f"pragma table_info({tbl})")]
                    p("      ", ft, dict(zip(cols, row)) if row else None)
            for _, _, nxt in df:
                if nxt and nxt > 0 and nxt not in seen:
                    q.append(nxt)

    # skills
    for sid in (15341, 15342, 13528, 13501, 20580):
        sk = c.execute("select id, name, cooldown_time from skills where id=?", (sid,)).fetchone()
        p("skill", sk)
        eids = [
            r[0]
            for r in c.execute("select effect_id from skill_effects where skill_id=?", (sid,))
        ]
        for eid in eids[:12]:
            # try special_effects / effects table variants
            for tbl in ("special_effects", "effects"):
                try:
                    cols = [x[1] for x in c.execute(f"pragma table_info({tbl})")]
                    if "id" not in cols:
                        continue
                    row = c.execute(f"select * from {tbl} where id=?", (eid,)).fetchone()
                    if row:
                        d = dict(zip(cols, row))
                        p("  ", tbl, {k: d[k] for k in list(d)[:8]})
                except Exception:
                    pass

    # group of 10712
    p(
        "10712 group_id",
        c.execute("select group_id from doodad_almighties where id=10712").fetchone(),
    )
    p(
        "3484 group_id",
        c.execute("select group_id from doodad_almighties where id=3484").fetchone(),
    )
    for g in (55, 56, 57, 58, 59):
        n = c.execute(
            "select count(*) from doodad_almighties where group_id=?", (g,)
        ).fetchone()
        p("dig group", g, "count", n)

    # weather volumes — any climate / zone link
    for tid in (3225, 3085, 3086, 3226):
        name = c.execute("select name, group_id, model from doodad_almighties where id=?", (tid,)).fetchone()
        p("weather tpl", tid, name)
        # search tables mentioning climate / weather doodad
    # climate_zones? zone_climates?
    tables = [
        r[0]
        for r in c.execute(
            "select name from sqlite_master where type='table' and ("
            "name like '%climate%' or name like '%weather%' or name like '%cloud%')"
        )
    ]
    p("climate/weather tables", tables)
    for t in tables:
        cols = [x[1] for x in c.execute(f"pragma table_info({t})")]
        p(" ", t, cols)
        if "doodad" in " ".join(cols).lower() or "almighty" in " ".join(cols).lower():
            sample = c.execute(f"select * from {t} limit 3").fetchall()
            p("   sample", sample)

    # purchase skins: item category + housing_decoration
    p("=== purchase items for SkipRandom fence skins ===")
    purch_ids = [
        r[0]
        for r in c.execute("select id from doodad_func_purchases")
    ]
    # map purchase templates in skip set from classify
    fence_tids = [
        r[0]
        for r in c.execute(
            "select id from doodad_almighties where name like '%Fence%' or name like '%Wall%' "
            "or name like '%Column%' or name like '%Brick%'"
        )
        if 10700 <= r[0] <= 10800
    ]
    p("fence-ish 107xx count", len(fence_tids))
    # housing_rebuild / housing_decos?
    ht = [
        r[0]
        for r in c.execute(
            "select name from sqlite_master where type='table' and name like '%hous%'"
        )
    ]
    p("hous tables", ht)
    for t in (
        "housing_binding_doodads",
        "housing_rebuild_materials",
        "item_housings",
        "housing_areas",
    ):
        try:
            cols = [x[1] for x in c.execute(f"pragma table_info({t})")]
            p(t, cols)
        except Exception:
            pass

    # DoodadFuncPurchase → item → is it a housing placeable?
    sample_purch = c.execute(
        "select p.id, p.item_id, p.count, a.id, a.name "
        "from doodad_funcs f "
        "join doodad_func_groups g on g.id=f.doodad_func_group_id "
        "join doodad_almighties a on a.id=g.doodad_almighty_id "
        "join doodad_func_purchases p on p.id=f.actual_func_id "
        "where f.actual_func_type='DoodadFuncPurchase' and a.id between 10730 and 10780 "
        "limit 15"
    ).fetchall()
    p("purchase sample", sample_purch)
    for row in sample_purch[:5]:
        iid = row[1]
        item = c.execute(
            "select id, name, category_id from items where id=?", (iid,)
        ).fetchone()
        p("  item", item)
        # item_housings?
        try:
            ih = c.execute("select * from item_housings where item_id=?", (iid,)).fetchone()
            p("  item_housings", ih)
        except Exception as e:
            p("  item_housings err", e)

    # barrels: are they in .g or only json? (already SkipRandom = json not in .g)
    # check doodad_func_respawns globally for fakeuse loot barrels
    p("=== any Respawn phase on ndeco barrel templates ===")
    for tid in (2831, 2828, 2822, 2783, 2540, 10712, 3484, 10626):
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        for gid in gids:
            pf = c.execute(
                "select actual_func_type, actual_func_id from doodad_phase_funcs where doodad_func_group_id=?",
                (gid,),
            ).fetchall()
            if pf:
                p(" tid", tid, "gid", gid, pf)

    # how many doodad_func_respawns exist at all
    try:
        n = c.execute("select count(*) from doodad_func_respawns").fetchone()
        p("doodad_func_respawns count", n)
        sample = c.execute("select * from doodad_func_respawns limit 5").fetchall()
        cols = [x[1] for x in c.execute("pragma table_info(doodad_func_respawns)")]
        p(" cols", cols, "sample", sample)
        # which almighties use them
        linked = c.execute(
            "select a.id, a.name, pf.doodad_func_group_id, r.min_time, r.max_time "
            "from doodad_phase_funcs pf "
            "join doodad_func_respawns r on r.id=pf.actual_func_id "
            "join doodad_func_groups g on g.id=pf.doodad_func_group_id "
            "join doodad_almighties a on a.id=g.doodad_almighty_id "
            "where pf.actual_func_type='DoodadFuncRespawn' "
            "limit 30"
        ).fetchall()
        p("respawn linked templates sample", linked)
        sunk = c.execute(
            "select a.id, a.name, r.min_time, r.max_time "
            "from doodad_phase_funcs pf "
            "join doodad_func_respawns r on r.id=pf.actual_func_id "
            "join doodad_func_groups g on g.id=pf.doodad_func_group_id "
            "join doodad_almighties a on a.id=g.doodad_almighty_id "
            "where pf.actual_func_type='DoodadFuncRespawn' "
            "and (a.name like '%Sunken%' or a.name like '%Treasure%' or a.name like '%Barrel%' "
            "or a.name like '%Crate%' or a.id in (10712,3484))"
        ).fetchall()
        p("respawn sunken/treasure/barrel", sunk)
    except Exception as e:
        p("respawn err", e)

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {OUT} lines={len(lines)}")


if __name__ == "__main__":
    main()
