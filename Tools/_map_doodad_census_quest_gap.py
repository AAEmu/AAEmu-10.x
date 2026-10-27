#!/usr/bin/env python3
"""Quest-func json orphans that current OrphanRules do not salvage."""
from __future__ import annotations

import json
import pathlib
import re
import sqlite3
from collections import Counter

CELL = pathlib.Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
JSON = pathlib.Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
DB = r"G:\AAchina\client\game\db\compact.sqlite3"

SPLIT = re.compile(r"(?m)^doodad\r?\n")
TYPE = re.compile(r"type\s+(\d+)")

PORTAL = {43, 44, 45}
DIG = set(range(55, 60))
RES = {2, 3, 4, 5, 12, 39, 40, 65}
SALE = {26, 71}
HOUS = {68, 69}
SALVAGE_GROUPS = DIG | RES | SALE | {11, 13, 24, 25} | PORTAL


def main() -> None:
    c = sqlite3.connect(DB)
    meta = {
        tid: (gid or 0)
        for tid, gid in c.execute("select id, group_id from doodad_almighties")
    }
    almighty = {
        fg: aid
        for fg, aid in c.execute(
            "select id, doodad_almighty_id from doodad_func_groups "
            "where doodad_almighty_id is not null"
        )
    }
    quest: set[int] = set()
    for fg, ft in c.execute(
        "select doodad_func_group_id, actual_func_type from doodad_funcs"
    ):
        aid = almighty.get(fg)
        if aid and ft == "DoodadFuncQuest":
            quest.add(aid)

    g: set[int] = set()
    for cell in CELL.iterdir():
        p = cell / "doodad.g"
        if not p.exists():
            continue
        for block in SPLIT.split(p.read_text(encoding="utf-8", errors="ignore")):
            m = TYPE.search(block)
            if m:
                t = int(m.group(1))
                if t:
                    g.add(t)

    raw = json.loads(JSON.read_text(encoding="utf-8"))
    by_tpl: Counter = Counter()
    by_group: Counter = Counter()
    for e in raw:
        tid = e.get("UnitId")
        if not tid or tid in g or tid not in quest:
            continue
        gid = meta.get(tid, 0)
        if gid in SALVAGE_GROUPS:
            continue
        if gid in HOUS or gid == 47:
            continue
        by_tpl[tid] += 1
        by_group[gid] += 1

    print(f"quest-func json-only NOT salvaged: {sum(by_tpl.values())} rows, {len(by_tpl)} templates")
    print("by group:", dict(by_group.most_common()))
    for tid, n in by_tpl.most_common():
        gid = meta.get(tid, 0)
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        print(f"  {tid} g{gid} x{n} {nm[0] if nm else ''!r}")


if __name__ == "__main__":
    main()
