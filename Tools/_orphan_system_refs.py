#!/usr/bin/env python3
"""Targeted content refs for leftover map-orphan templates."""
from __future__ import annotations

import sqlite3

DB = r"G:\AAchina\client\game\db\compact.sqlite3"
IDS = [
    7701, 12290, 12291, 5900, 12251, 6591, 10053, 10054, 12343, 7232,
    2032, 5288, 5290, 7019, 4614, 1693, 2037, 2121, 9696, 293, 294, 1800, 1605,
]

# Tables worth checking (not whole DB).
TABLES = [
    "housing_binding_doodads",
    "game_schedule_doodads",
    "quest_act_con_accept_doodads",
    "quest_act_obj_interactions",
    "quest_act_obj_doodad_phase_checks",
    "doodad_func_bindings",
    "doodad_func_bindingss",  # typo guard
    "dominion_housings",
    "dominions",
    "nation_member_configs",
    "indun_events",
    "tower_def_prog_spawn_targets",
    "specialty_npcs",
    "specialties",
    "craft_packs",
    "crafts",
    "item_doodad_conversions",
    "item_housings",
    "housing_decorations",
    "slave_initial_doodads",
    "npc_doodad_bindings",
]


def main() -> None:
    c = sqlite3.connect(DB)
    existing = {r[0] for r in c.execute("select name from sqlite_master where type='table'")}
    print("available of interest:", sorted(t for t in TABLES if t in existing))

    # discover doodad_func_* actual tables for Binding
    bind_tables = [
        r[0]
        for r in c.execute(
            "select name from sqlite_master where type='table' and name like 'doodad_func%'"
        )
    ]
    print("doodad_func tables sample:", [t for t in bind_tables if "bind" in t.lower() or "buff" in t.lower()][:20])

    for tid in IDS:
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        row = c.execute(
            "select group_id, model from doodad_almighties where id=?", (tid,)
        ).fetchone()
        gid = row[0] if row else None
        hits = []
        for t in TABLES:
            if t not in existing:
                continue
            cols = [r[1] for r in c.execute(f"pragma table_info({t})")]
            for col in cols:
                if col in ("doodad_id", "doodad_almighty_id", "target_id", "id") or col.endswith("doodad_id"):
                    try:
                        if col == "id" and t not in ("doodad_almighties",):
                            # only when table is keyed by doodad
                            if t.startswith("doodad_") and t != "doodad_almighties":
                                continue
                            if "doodad" not in t:
                                continue
                        n = c.execute(f"select count(*) from {t} where {col}=?", (tid,)).fetchone()[0]
                    except sqlite3.Error:
                        continue
                    if n:
                        hits.append(f"{t}.{col}={n}")
        # func types
        fgs = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        ftypes = []
        for fg in fgs:
            for (ft,) in c.execute(
                "select actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (fg,),
            ):
                ftypes.append(ft)
        print(f"{tid} g{gid} '{nm[0] if nm else ''}': refs={hits or '-'} funcs={ftypes}")

    # housing names for 7701 again + any craft pack specialty zone link
    print("\n--- specialty craft pack ids on leftover workbenches ---")
    for tid in (7701, 12290, 12291, 5900, 12251):
        fgs = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        for fg in fgs:
            for (fid, ft) in c.execute(
                "select actual_func_id, actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (fg,),
            ):
                if ft == "DoodadFuncCraftPack":
                    pack = c.execute(
                        "select craft_pack_id from doodad_func_craft_packs where id=?",
                        (fid,),
                    ).fetchone()
                    print(f"  {tid} craft_pack_id={pack}")

    # g101 all + schedule?
    print("\n--- purification (g101) schedule / quest ---")
    for (tid,) in c.execute("select id from doodad_almighties where group_id=101"):
        sched = c.execute(
            "select count(*) from game_schedule_doodads where doodad_id=?", (tid,)
        ).fetchone()[0]
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        print(f"  {tid} '{nm[0] if nm else ''}' schedule_rows={sched}")

    # g53 nation tome vs normal — Binding owner?
    print("\n--- DoodadFuncBinding for 12343 ---")
    if "doodad_func_bindings" in existing:
        cols = [r[1] for r in c.execute("pragma table_info(doodad_func_bindings)")]
        print("  cols", cols)
        # get actual_func_id
        for fg in c.execute(
            "select id from doodad_func_groups where doodad_almighty_id=12343"
        ):
            for (fid, ft) in c.execute(
                "select actual_func_id, actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (fg[0],),
            ):
                print(f"  func {ft} id={fid}")
                if ft == "DoodadFuncBinding" and "doodad_func_bindings" in existing:
                    print(
                        "   ",
                        c.execute(
                            "select * from doodad_func_bindings where id=?", (fid,)
                        ).fetchone(),
                    )

    # dominion statues g80
    print("\n--- g80 territory statues ---")
    for tid, in c.execute("select id from doodad_almighties where group_id=80"):
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        bind = c.execute(
            "select count(*) from housing_binding_doodads where doodad_id=?", (tid,)
        ).fetchone()[0]
        print(f"  {tid} '{nm[0] if nm else ''}' housing_bind={bind}")


if __name__ == "__main__":
    main()
