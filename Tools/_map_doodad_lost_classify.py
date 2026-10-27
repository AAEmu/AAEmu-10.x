#!/usr/bin/env python3
"""Classify every json row LevelPack would NOT load (not covered, not salvaged)."""
from __future__ import annotations

import json
import pathlib
import re
import sqlite3
from collections import Counter, defaultdict

CELL = pathlib.Path(r"G:\AAchina\Server\game\worlds\main_world\level_design\cells")
JSON = pathlib.Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"
)
DB = r"G:\AAchina\client\game\db\compact.sqlite3"
OUT = pathlib.Path(
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\Tools\_map_doodad_lost_classify.txt"
)

SPLIT = re.compile(r"(?m)^doodad\r?\n")
TYPE = re.compile(r"type\s+(\d+)")

PORTAL = {43, 44, 45}
DIG = set(range(55, 60))
RES = {2, 3, 4, 5, 12, 39, 40, 65}
SALE = {26, 71}
HOUS = {68, 69}
SALVAGE = DIG | RES | SALE | {11, 13, 24, 25} | PORTAL

GROUP = {
    0: "unknown/missing",
    24: "Interaction",
    34: "group_34",
    46: "group_46",
    47: "Unused",
    54: "group_54",
    68: "HousingDoorWindow",
    69: "HousingFurniture",
    72: "group_72",
}


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
        meta[tid] = {"gid": gid or 0, "model": model or "", "exists": True}

    almighty = {
        fg: aid
        for fg, aid in c.execute(
            "select id, doodad_almighty_id from doodad_func_groups "
            "where doodad_almighty_id is not null"
        )
    }
    flags = defaultdict(lambda: {"quest": False, "resident": False, "evidence": False, "types": set()})
    for fg, ft in c.execute(
        "select doodad_func_group_id, actual_func_type from doodad_funcs"
    ):
        aid = almighty.get(fg)
        if not aid:
            continue
        flags[aid]["types"].add(ft or "")
        if ft == "DoodadFuncQuest":
            flags[aid]["quest"] = True
        if ft == "DoodadFuncResidentTownhallUiOpen":
            flags[aid]["resident"] = True
        if ft == "DoodadFuncEvidenceItemLoot":
            flags[aid]["evidence"] = True

    scheduled = {
        r[0] for r in c.execute("select distinct doodad_id from game_schedule_doodads")
    }
    # specialty / workbench related tables
    tables = [r[0] for r in c.execute("select name from sqlite_master where type='table'")]
    specialty_related = [t for t in tables if "special" in t.lower() or "workbench" in t.lower() or "purif" in t.lower() or "memory" in t.lower() or "campfire" in t.lower() or "nation" in t.lower()]
    emit(f"specialty-ish tables: {specialty_related}")

    # housing public?
    house_tables = [t for t in tables if "hous" in t.lower()]
    emit(f"housing tables: {house_tables[:30]}")

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

    # Classify each json-only (template not in .g) that orphan rules would NOT plant
    lost_rows = []
    for e in raw:
        tid = int(e.get("UnitId") or 0)
        if not tid:
            continue
        pos = e.get("Position") or {}
        x = float(pos.get("X") or 0)
        y = float(pos.get("Y") or 0)
        z = float(pos.get("Z") or 0)
        title = e.get("Title") or ""

        m = meta.get(tid)
        exists = m is not None
        gid = m["gid"] if m else 0
        fl = flags[tid]

        # covered by level pack template → not "lost"
        if tid in g:
            continue

        # orphan Decide (sans occupancy — json-only templates aren't occupied by same tpl from .g)
        decision = None
        if not exists:
            decision = "SkipUnknownTemplate"
        elif gid == 47:
            decision = "SkipUnusedGroup"
        elif gid in HOUS:
            decision = "SkipHousingOrphan"
        elif fl["evidence"]:
            decision = "SkipRuntimeEvidence"
        elif gid in DIG or gid in RES or gid in SALE:
            decision = "Plant"  # salvaged — skip
            continue
        elif gid == 24:
            if fl["resident"] or fl["quest"]:
                continue  # Plant
            decision = "SkipRandomPlacement"
        elif gid in (11, 13, 25) or gid in PORTAL:
            continue  # Plant
        else:
            decision = "SkipJsonOrphanUnhandled"

        lost_rows.append(
            {
                "tid": tid,
                "gid": gid,
                "x": x,
                "y": y,
                "z": z,
                "title": title,
                "decision": decision,
                "quest": fl["quest"],
                "resident": fl["resident"],
                "evidence": fl["evidence"],
                "scheduled": tid in scheduled,
                "func_types": sorted(fl["types"]),
                "near_origin": (abs(x) < 50 and abs(y) < 50),
            }
        )

    emit(f"lost rows (json-only, not salvaged): {len(lost_rows)}")

    by_dec = Counter(r["decision"] for r in lost_rows)
    emit("--- by decision ---")
    for k, n in by_dec.most_common():
        emit(f"  {k}: {n}")

    # ----- Unknown templates -----
    emit("")
    emit("=" * 72)
    emit("SkipUnknownTemplate — template not in doodad_almighties")
    emit("=" * 72)
    unk = [r for r in lost_rows if r["decision"] == "SkipUnknownTemplate"]
    by_tid = Counter(r["tid"] for r in unk)
    emit(f"rows={len(unk)} templates={len(by_tid)}")
    # title clusters
    by_title = Counter((r["title"] or "(empty)") for r in unk)
    emit("by Title (top 20):")
    for t, n in by_title.most_common(20):
        emit(f"  {n:5d}  {t!r}")
    emit("top templates:")
    for tid, n in by_tid.most_common(40):
        titles = Counter(r["title"] for r in unk if r["tid"] == tid)
        emit(f"  {tid} x{n} titles={dict(titles.most_common(3))}")

    # ----- Random placement -----
    emit("")
    emit("=" * 72)
    emit("SkipRandomPlacement — Interaction without quest/resident")
    emit("=" * 72)
    rnd = [r for r in lost_rows if r["decision"] == "SkipRandomPlacement"]
    by_tid = Counter(r["tid"] for r in rnd)
    emit(f"rows={len(rnd)} templates={len(by_tid)}")
    # func type census
    ftc = Counter()
    for r in rnd:
        for ft in r["func_types"] or ["(none)"]:
            ftc[ft] += 1
    emit("func types (row-weighted sample via templates×count):")
    # better: per template once * count
    for tid, n in by_tid.most_common(30):
        sample = next(r for r in rnd if r["tid"] == tid)
        emit(
            f"  {tid} x{n} '{sample['title']}' funcs={sample['func_types'][:6]} "
            f"sched={sample['scheduled']}"
        )

    # FakeUse / chest markers
    fake = [
        (tid, n)
        for tid, n in by_tid.items()
        if any(
            "FakeUse" in ft or "Chest" in (next(r for r in rnd if r["tid"] == tid)["title"] or "")
            for ft in (next(r for r in rnd if r["tid"] == tid)["func_types"] or [])
        )
        or "Chest" in (next(r for r in rnd if r["tid"] == tid)["title"] or "")
        or "Barrel" in (next(r for r in rnd if r["tid"] == tid)["title"] or "")
    ]
    emit(f"chest/barrel-ish templates: {len(fake)} rows={sum(n for _,n in fake)}")

    # ----- Evidence -----
    emit("")
    emit("=" * 72)
    emit("SkipRuntimeEvidence")
    emit("=" * 72)
    ev = [r for r in lost_rows if r["decision"] == "SkipRuntimeEvidence"]
    by_tid = Counter(r["tid"] for r in ev)
    emit(f"rows={len(ev)} templates={len(by_tid)}")
    for tid, n in by_tid.most_common(15):
        sample = next(r for r in ev if r["tid"] == tid)
        emit(f"  {tid} x{n} '{sample['title']}'")

    # ----- Housing -----
    emit("")
    emit("=" * 72)
    emit("SkipHousingOrphan")
    emit("=" * 72)
    hous = [r for r in lost_rows if r["decision"] == "SkipHousingOrphan"]
    emit(f"rows={len(hous)}")
    near = sum(1 for r in hous if r["near_origin"])
    far = len(hous) - near
    emit(f"  near origin (<50,<50): {near}")
    emit(f"  world-placed: {far}")
    by_tid_far = Counter(r["tid"] for r in hous if not r["near_origin"])
    emit("  world-placed templates:")
    for tid, n in by_tid_far.most_common(25):
        sample = next(r for r in hous if r["tid"] == tid and not r["near_origin"])
        gid = sample["gid"]
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        emit(
            f"    {tid} g{gid} x{n} '{sample['title'] or (nm[0] if nm else '')}' "
            f"@({sample['x']:.0f},{sample['y']:.0f})"
        )
    # check housing_bindings / public houses in compact if any
    for tname in ("housings", "housing", "public_houses", "housing_areas", "taxations"):
        if tname in tables:
            cols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
            emit(f"  table {tname} cols={cols[:15]} count={c.execute(f'select count(*) from {tname}').fetchone()[0]}")

    # ----- Unhandled -----
    emit("")
    emit("=" * 72)
    emit("SkipJsonOrphanUnhandled")
    emit("=" * 72)
    unh = [r for r in lost_rows if r["decision"] == "SkipJsonOrphanUnhandled"]
    by_gid = Counter(r["gid"] for r in unh)
    emit(f"rows={len(unh)}")
    for gid, n in by_gid.most_common():
        gnm = c.execute("select name from doodad_groups where id=?", (gid,)).fetchone()
        emit(f"  g{gid} {gnm[0] if gnm else GROUP.get(gid,'?')}: {n}")

    by_tid = Counter(r["tid"] for r in unh)
    emit("all unhandled templates:")
    for tid, n in by_tid.most_common():
        sample = next(r for r in unh if r["tid"] == tid)
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        gnm = c.execute(
            "select name from doodad_groups where id=?", (sample["gid"],)
        ).fetchone()
        # content refs
        refs = []
        if tid in scheduled:
            refs.append("game_schedule_doodads")
        for q in (
            ("specialty_npcs", "doodad_id"),
            ("specialty_packs", "doodad_id"),
            ("quest_act_obj_interactions", "doodad_id"),
            ("quest_act_con_accept_doodads", "doodad_id"),
            ("indun_events", "doodad_id"),
        ):
            tname, col = q
            if tname not in tables:
                continue
            tcols = [r[1] for r in c.execute(f"pragma table_info({tname})")]
            if col not in tcols:
                continue
            cnt = c.execute(
                f"select count(*) from {tname} where {col}=?", (tid,)
            ).fetchone()[0]
            if cnt:
                refs.append(f"{tname}×{cnt}")
        # also search doodad_id columns broadly for this id in a few known
        emit(
            f"  {tid} g{sample['gid']}({gnm[0] if gnm else '?'}) x{n} "
            f"'{nm[0] if nm else sample['title']}' quest={sample['quest']} "
            f"funcs={sample['func_types'][:5]} refs={refs or '-'} "
            f"@({sample['x']:.0f},{sample['y']:.0f})"
        )

    # ----- Quest-func lost (any decision) -----
    emit("")
    emit("=" * 72)
    emit("ALL lost rows that carry DoodadFuncQuest")
    emit("=" * 72)
    qlost = [r for r in lost_rows if r["quest"]]
    emit(f"rows={len(qlost)} templates={len({r['tid'] for r in qlost})}")
    for tid, n in Counter(r["tid"] for r in qlost).most_common():
        sample = next(r for r in qlost if r["tid"] == tid)
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        gnm = c.execute(
            "select name from doodad_groups where id=?", (sample["gid"],)
        ).fetchone()
        emit(
            f"  {tid} g{sample['gid']}({gnm[0] if gnm else '?'}) x{n} "
            f"'{nm[0] if nm else ''}' decision={sample['decision']}"
        )

    # group name dump for 34/46/54/72
    emit("")
    emit("--- doodad_groups names for unhandled ---")
    for gid in sorted({r["gid"] for r in unh} | {34, 46, 54, 72, 53, 80, 86, 101}):
        gnm = c.execute("select name from doodad_groups where id=?", (gid,)).fetchone()
        emit(f"  {gid}: {gnm[0] if gnm else '(missing)'}")

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    emit(f"\nwrote {OUT}")


if __name__ == "__main__":
    main()
