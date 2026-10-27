#!/usr/bin/env python3
"""Census: doodad_spawns.json vs LevelPack (.g plant + orphan salvage).

Mirrors MapDoodadPlantRules.Decide + MapDoodadOrphanRules.Decide for main_world.
Schedule windows: templates in game_schedule_doodads are counted as SkipSchedule
(NotStarted/Ended) for the permanent-plant column; InProgress is unknown offline.
"""
from __future__ import annotations

import json
import pathlib
import re
import sqlite3
from collections import Counter, defaultdict

CELL_ROOT = pathlib.Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
JSON_PATH = pathlib.Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
DB = r"G:\AAchina\client\game\db\compact.sqlite3"
OUT = pathlib.Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\Tools\_map_doodad_census_report.txt"
)

GROUP = {
    2: "Deforestation",
    3: "Picking",
    4: "Mining",
    5: "Livestock",
    11: "Quest",
    12: "Agriculture",
    13: "SkillVolume",
    24: "Interaction",
    25: "Level",
    26: "Sale",
    39: "Excavation",
    40: "MarineAgriculture",
    43: "PortalWorldgate",
    44: "PortalMirage",
    45: "PortalInstance",
    47: "Unused",
    55: "DigTreasureNormal",
    56: "DigTreasureGrand",
    57: "DigTreasureRare",
    58: "DigTreasureArcane",
    59: "DigTreasureHeroic",
    65: "SportFishing",
    68: "HousingDoorWindow",
    69: "HousingFurniture",
    71: "FishSale",
}

PORTAL_GROUPS = {43, 44, 45}
DIG = set(range(55, 60))
RESOURCE = {2, 3, 4, 5, 12, 39, 40, 65}
SALE = {26, 71}
HOUSING = {68, 69}

SPLIT = re.compile(r"(?m)^doodad\r?\n")
TYPE_RE = re.compile(r"type\s+(\d+)")
POS_RE = re.compile(
    r"pos\s*\(\s*x\s+([-\d.eE+]+)\s*,\s*y\s+([-\d.eE+]+)\s*,\s*z\s+([-\d.eE+]+)\s*\)"
)
IGNORE_TYPE = re.compile(r"doodadType\s+(\d+)|type\s+(\d+)")
CELL_DIR = re.compile(r"^(\d+)_(\d+)$")


def gname(gid: int) -> str:
    return GROUP.get(gid, f"group_{gid}")


def is_ignore_file(name: str) -> bool:
    n = name.lower()
    if n.startswith("doodad_open"):
        return True
    return "ignore" in n and "doodad" in n


def parse_ignore(cell_dir: pathlib.Path) -> set[int]:
    ids: set[int] = set()
    for p in cell_dir.glob("*.g"):
        if not is_ignore_file(p.name):
            continue
        text = p.read_text(encoding="utf-8", errors="ignore")
        for m in IGNORE_TYPE.finditer(text):
            raw = m.group(1) or m.group(2)
            if raw:
                v = int(raw)
                if v:
                    ids.add(v)
    return ids


def parse_g_file(path: pathlib.Path, cx: int, cy: int) -> list[tuple[int, float, float, float]]:
    text = path.read_text(encoding="utf-8", errors="ignore")
    ox, oy = cx * 1024.0, cy * 1024.0
    out: list[tuple[int, float, float, float]] = []
    for block in SPLIT.split(text):
        if not block.strip():
            continue
        tm = TYPE_RE.search(block)
        pm = POS_RE.search(block)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        if tid == 0:
            continue
        lx, ly, lz = float(pm.group(1)), float(pm.group(2)), float(pm.group(3))
        out.append((tid, ox + lx, oy + ly, lz))
    return out


def same_stand(ax, ay, az, bx, by, bz, r2=4.0) -> bool:
    dx, dy, dz = ax - bx, ay - by, az - bz
    return dx * dx + dy * dy + dz * dz <= r2


def plant_decide(exists, ignored, tower, scheduled, gid) -> str:
    if not exists:
        return "SkipUnknownTemplate"
    if tower:
        return "SkipTowerAlmighty"
    if gid == 47:
        return "SkipUnusedGroup"
    if scheduled:
        return "SkipSchedule"
    if ignored:
        return "SkipIgnoredPermanent"
    return "Plant"


def orphan_decide(exists, in_lp, occupied, gid, evidence, resident, quest) -> str:
    if not exists:
        return "SkipUnknownTemplate"
    if in_lp:
        return "SkipJsonOrphanCoveredByLevelPack"
    if occupied:
        return "SkipStandOccupied"
    if gid == 47:
        return "SkipUnusedGroup"
    if gid in HOUSING:
        return "SkipHousingOrphan"
    if evidence:
        return "SkipRuntimeEvidence"
    if gid in DIG or gid in RESOURCE or gid in SALE:
        return "Plant"
    if gid == 24:
        if resident or quest:
            return "Plant"
        return "SkipRandomPlacement"
    if gid in (11, 13, 25) or gid in PORTAL_GROUPS:
        return "Plant"
    return "SkipJsonOrphanUnhandled"


def main() -> None:
    lines: list[str] = []

    def emit(s: str = "") -> None:
        lines.append(s)
        print(s)

    c = sqlite3.connect(DB)
    meta: dict[int, dict] = {}
    for tid, gid, model in c.execute(
        "select id, group_id, ifnull(model,'') from doodad_almighties"
    ):
        model = model or ""
        meta[tid] = {
            "gid": gid or 0,
            "model": model,
            "invisible": ("a://invalid" in model.lower())
            or ("env.invisible" in model.lower()),
        }

    cols = [r[1] for r in c.execute("pragma table_info(doodad_funcs)")]
    emit(f"doodad_funcs columns: {cols}")
    type_col = "actual_func_type" if "actual_func_type" in cols else "func_type"
    fg_col = "doodad_func_group_id"

    flags: dict[int, dict] = defaultdict(
        lambda: {"evidence": False, "resident": False, "quest": False}
    )
    almighty_of_fg: dict[int, int] = {}
    for fgid, aid in c.execute(
        "select id, doodad_almighty_id from doodad_func_groups "
        "where doodad_almighty_id is not null"
    ):
        almighty_of_fg[fgid] = aid

    for fgid, ftype in c.execute(f"select {fg_col}, {type_col} from doodad_funcs"):
        aid = almighty_of_fg.get(fgid)
        if aid is None:
            continue
        raw = ftype or ""
        ft = raw.lower().replace("_", "")
        if "evidenceitemloot" in ft:
            flags[aid]["evidence"] = True
        if "residenttownhall" in ft:
            flags[aid]["resident"] = True
        if "doodadfuncquest" in ft or ft.endswith("funcquest"):
            flags[aid]["quest"] = True

    sample_types = c.execute(
        f"select distinct {type_col} from doodad_funcs where lower({type_col}) like '%quest%' "
        f"or lower({type_col}) like '%resident%' or lower({type_col}) like '%evidence%'"
    ).fetchall()
    emit(f"func type samples (quest/resident/evidence): {sample_types[:40]}")

    scheduled = {
        r[0] for r in c.execute("select distinct doodad_id from game_schedule_doodads")
    }

    tower_ids: set[int] = set()
    tables = [
        r[0]
        for r in c.execute(
            "select name from sqlite_master where type='table' and name like '%tower%'"
        )
    ]
    emit(f"tower tables: {tables}")
    for tname in tables:
        tcols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
        if "doodad_almighty_id" in tcols:
            for (v,) in c.execute(
                f"select distinct doodad_almighty_id from {tname} "
                f"where doodad_almighty_id is not null"
            ):
                if v:
                    tower_ids.add(int(v))
        if "target_type" in tcols and "target_id" in tcols:
            for tid, tty in c.execute(f"select target_id, target_type from {tname}"):
                if (
                    tid
                    and tty
                    and "doodad" in str(tty).lower()
                    and "almighty" in str(tty).lower()
                ):
                    tower_ids.add(int(tid))
    emit(f"tower almighty template ids: {len(tower_ids)}")

    emit("parsing doodad.g …")
    g_rows: list[dict] = []
    g_templates: set[int] = set()
    for cell in CELL_ROOT.iterdir():
        if not cell.is_dir():
            continue
        m = CELL_DIR.match(cell.name)
        if not m:
            continue
        cx, cy = int(m.group(1)), int(m.group(2))
        path = cell / "doodad.g"
        if not path.exists():
            continue
        ignore = parse_ignore(cell)
        for tid, x, y, z in parse_g_file(path, cx, cy):
            g_templates.add(tid)
            g_rows.append({"tid": tid, "x": x, "y": y, "z": z, "ignored": tid in ignore})
    emit(f"level-pack placements: {len(g_rows)} templates: {len(g_templates)}")

    g_decisions = Counter()
    g_by_group_plant = Counter()
    g_planted: list[dict] = []
    for row in g_rows:
        tid = row["tid"]
        m = meta.get(tid)
        exists = m is not None
        gid = m["gid"] if m else 0
        inv = m["invisible"] if m else False
        d = plant_decide(
            exists, row["ignored"], tid in tower_ids, tid in scheduled, gid
        )
        g_decisions[d] += 1
        if d == "Plant":
            g_by_group_plant[gid] += 1
            g_planted.append({**row, "gid": gid, "invisible": inv})

    sched_in_g = sum(1 for r in g_rows if r["tid"] in scheduled)
    emit(f"level-pack rows with schedule template: {sched_in_g} (counted SkipSchedule offline)")

    emit("loading doodad_spawns.json …")
    raw = json.loads(JSON_PATH.read_text(encoding="utf-8"))
    j_rows = []
    for e in raw:
        tid = e.get("UnitId") or e.get("unitId")
        pos = e.get("Position") or e.get("position") or {}
        if not tid:
            continue
        j_rows.append(
            {
                "tid": int(tid),
                "x": float(pos.get("X") or pos.get("x") or 0),
                "y": float(pos.get("Y") or pos.get("y") or 0),
                "z": float(pos.get("Z") or pos.get("z") or 0),
                "title": e.get("Title") or e.get("title") or "",
            }
        )
    emit(f"json rows: {len(j_rows)}")

    # Spatial hash for stand occupancy (2 m = cell size 2).
    cell = 2.0
    grid: dict[tuple[int, int], list[dict]] = defaultdict(list)

    def cell_key(x: float, y: float) -> tuple[int, int]:
        return (int(x // cell), int(y // cell))

    def add_existing(ex: dict) -> None:
        grid[cell_key(ex["x"], ex["y"])].append(ex)

    def is_occupied(tid: int, gid: int, x: float, y: float, z: float) -> bool:
        cx, cy = cell_key(x, y)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for ex in grid.get((cx + dx, cy + dy), ()):
                    if not same_stand(ex["x"], ex["y"], ex["z"], x, y, z):
                        continue
                    if ex["tid"] == tid:
                        return True
                    if (
                        ex["gid"] in PORTAL_GROUPS
                        and gid in PORTAL_GROUPS
                        and not ex["invisible"]
                    ):
                        return True
        return False

    for p in g_planted:
        add_existing(
            {
                "tid": p["tid"],
                "x": p["x"],
                "y": p["y"],
                "z": p["z"],
                "gid": p["gid"],
                "invisible": p["invisible"],
            }
        )

    orphan_dec = Counter()
    orphan_plant_by_group = Counter()
    lost_by_group = Counter()
    lost_examples: dict[str, list] = defaultdict(list)
    care_buckets = {
        "quest_group11": Counter(),
        "interaction_quest_func": Counter(),
        "resident_townhall": Counter(),
        "portal": Counter(),
        "skill_volume": Counter(),
        "level": Counter(),
    }
    care_lost = {k: Counter() for k in care_buckets}

    orphan_planted = 0
    for e in j_rows:
        tid = e["tid"]
        m = meta.get(tid)
        exists = m is not None
        gid = m["gid"] if m else 0
        inv = m["invisible"] if m else False
        fl = flags[tid]
        occupied = is_occupied(tid, gid, e["x"], e["y"], e["z"])

        d = orphan_decide(
            exists,
            tid in g_templates,
            occupied,
            gid,
            fl["evidence"],
            fl["resident"],
            fl["quest"],
        )
        orphan_dec[d] += 1

        tags = []
        if gid == 11:
            tags.append("quest_group11")
        if fl["quest"]:
            tags.append("interaction_quest_func")
        if fl["resident"]:
            tags.append("resident_townhall")
        if gid in PORTAL_GROUPS:
            tags.append("portal")
        if gid == 13:
            tags.append("skill_volume")
        if gid == 25:
            tags.append("level")

        if d == "Plant":
            orphan_planted += 1
            orphan_plant_by_group[gid] += 1
            for t in tags:
                care_buckets[t]["salvaged"] += 1
            add_existing(
                {
                    "tid": tid,
                    "x": e["x"],
                    "y": e["y"],
                    "z": e["z"],
                    "gid": gid,
                    "invisible": inv,
                }
            )
        else:
            if d not in (
                "SkipJsonOrphanCoveredByLevelPack",
                "SkipStandOccupied",
            ):
                lost_by_group[gid] += 1
                key = f"{d}|{gid}"
                if len(lost_examples[key]) < 8:
                    nm = c.execute(
                        "select en_us from localized_texts where tbl_name='doodad_almighties' "
                        "and tbl_column_name='name' and idx=?",
                        (tid,),
                    ).fetchone()
                    lost_examples[key].append(
                        (tid, e["title"] or (nm[0] if nm else ""), e["x"], e["y"])
                    )
                for t in tags:
                    care_lost[t][d] += 1
            else:
                for t in tags:
                    care_buckets[t]["covered_or_occupied"] += 1

    for p in g_planted:
        tid, gid = p["tid"], p["gid"]
        fl = flags[tid]
        if gid == 11:
            care_buckets["quest_group11"]["levelpack_plant"] += 1
        if fl["quest"]:
            care_buckets["interaction_quest_func"]["levelpack_plant"] += 1
        if fl["resident"]:
            care_buckets["resident_townhall"]["levelpack_plant"] += 1
        if gid in PORTAL_GROUPS:
            care_buckets["portal"]["levelpack_plant"] += 1
        if gid == 13:
            care_buckets["skill_volume"]["levelpack_plant"] += 1
        if gid == 25:
            care_buckets["level"]["levelpack_plant"] += 1

    for e in j_rows:
        tid = e["tid"]
        m = meta.get(tid)
        gid = m["gid"] if m else 0
        fl = flags[tid]
        if gid == 11:
            care_buckets["quest_group11"]["json"] += 1
        if fl["quest"]:
            care_buckets["interaction_quest_func"]["json"] += 1
        if fl["resident"]:
            care_buckets["resident_townhall"]["json"] += 1
        if gid in PORTAL_GROUPS:
            care_buckets["portal"]["json"] += 1
        if gid == 13:
            care_buckets["skill_volume"]["json"] += 1
        if gid == 25:
            care_buckets["level"]["json"] += 1

    emit("")
    emit("=" * 72)
    emit("MAIN_WORLD MAP DOODAD CENSUS (LevelPack vs doodad_spawns.json)")
    emit("=" * 72)
    emit(f"json authority rows:     {len(j_rows)}")
    emit(f"level-pack .g rows:      {len(g_rows)}")
    emit(f"  -> Plant (permanent):   {g_decisions['Plant']}")
    emit(f"  -> SkipSchedule*:       {g_decisions['SkipSchedule']}  (*all scheduled templates offline)")
    emit(f"  -> SkipIgnoredPermanent:{g_decisions['SkipIgnoredPermanent']}")
    emit(f"  -> SkipTowerAlmighty:   {g_decisions['SkipTowerAlmighty']}")
    emit(f"  -> SkipUnusedGroup:     {g_decisions['SkipUnusedGroup']}")
    emit(f"  -> SkipUnknownTemplate: {g_decisions['SkipUnknownTemplate']}")
    emit(f"json orphan salvage Plant:{orphan_planted}")
    total_perm = g_decisions["Plant"] + orphan_planted
    emit(f"approx total permanent:  {total_perm}")
    emit(f"  (json was {len(j_rows)}; delta vs json = {total_perm - len(j_rows):+d})")
    emit("")
    emit("--- LevelPack .g decisions ---")
    for k, n in g_decisions.most_common():
        emit(f"  {k}: {n}")
    emit("")
    emit("--- LevelPack Plant by group (top 20) ---")
    for gid, n in g_by_group_plant.most_common(20):
        emit(f"  {gid:3d} {gname(gid):22s} {n}")
    emit("")
    emit("--- Orphan salvage decisions (every json row) ---")
    for k, n in orphan_dec.most_common():
        emit(f"  {k}: {n}")
    emit("")
    emit("--- Orphan Plant by group ---")
    for gid, n in orphan_plant_by_group.most_common():
        emit(f"  {gid:3d} {gname(gid):22s} {n}")
    emit("")
    emit("--- NO LONGER LOADED from json (not covered by .g template, not salvaged) ---")
    emit("    (excludes SkipJsonOrphanCoveredByLevelPack + SkipStandOccupied)")
    total_lost = sum(lost_by_group.values())
    emit(f"  total rows: {total_lost}")
    for gid, n in lost_by_group.most_common():
        emit(f"  {gid:3d} {gname(gid):22s} {n}")
    emit("")
    emit("--- Lost examples by decision|group ---")
    for key, exs in sorted(lost_examples.items(), key=lambda kv: -sum(1 for _ in kv[1])):
        # sort by count of lost in that bucket from lost_by_group-ish via examples only
        emit(f"  {key} (sample {len(exs)}):")
        for tid, title, x, y in exs:
            emit(f"    tpl={tid} '{title}' @ ({x:.1f},{y:.1f})")
    emit("")
    emit("--- CARE buckets (quest / community / portals) ---")
    for name, ctr in care_buckets.items():
        emit(f"  {name}:")
        for k, v in sorted(ctr.items()):
            emit(f"    {k}: {v}")
        lost = care_lost[name]
        if lost:
            emit("    LOST (not covered, not salvaged):")
            for k, v in lost.most_common():
                emit(f"      {k}: {v}")
        else:
            emit("    LOST: 0")

    emit("")
    emit("--- ResidentTownhall templates ---")
    res_tpls = sorted(tid for tid, fl in flags.items() if fl["resident"])
    emit(f"  templates with ResidentTownhall: {len(res_tpls)}")
    for tid in res_tpls[:40]:
        in_g = tid in g_templates
        jn = sum(1 for e in j_rows if e["tid"] == tid)
        gn = sum(1 for r in g_rows if r["tid"] == tid)
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        emit(f"  {tid} '{nm[0] if nm else ''}' in_g={in_g} g_rows={gn} json_rows={jn}")

    emit("")
    emit("--- Portal templates: json-only (not in any .g) ---")
    portal_only_j = Counter()
    for e in j_rows:
        tid = e["tid"]
        m = meta.get(tid)
        if not m or m["gid"] not in PORTAL_GROUPS:
            continue
        if tid not in g_templates:
            portal_only_j[tid] += 1
    emit(
        f"  json-only portal templates: {len(portal_only_j)} "
        f"rows: {sum(portal_only_j.values())}"
    )
    for tid, n in portal_only_j.most_common(30):
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        gid = meta[tid]["gid"]
        emit(f"  {tid} g{gid} x{n} '{nm[0] if nm else ''}'")

    # Also: scheduled .g that would plant when InProgress — important for festivals
    emit("")
    emit("--- Scheduled templates in .g (arm cron / plant when live) ---")
    sched_tpl = Counter()
    for r in g_rows:
        if r["tid"] in scheduled:
            sched_tpl[r["tid"]] += 1
    emit(f"  templates: {len(sched_tpl)} rows: {sum(sched_tpl.values())}")

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    emit("")
    emit(f"wrote {OUT}")


if __name__ == "__main__":
    main()
