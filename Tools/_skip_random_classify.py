#!/usr/bin/env python3
"""Break down SkipRandomPlacement Interaction orphans by system."""
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
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\Tools\_skip_random_classify.txt"
)


def main() -> None:
    lines: list[str] = []

    def emit(s: str = "") -> None:
        lines.append(s)
        print(s)

    c = sqlite3.connect(DB)
    meta = {
        tid: {"gid": gid or 0, "model": model or ""}
        for tid, gid, model in c.execute(
            "select id, group_id, ifnull(model,'') from doodad_almighties"
        )
    }
    almighty = {
        fg: aid
        for fg, aid in c.execute(
            "select id, doodad_almighty_id from doodad_func_groups "
            "where doodad_almighty_id is not null"
        )
    }
    flags = defaultdict(
        lambda: {
            "types": [],
            "quest": False,
            "resident": False,
            "evidence": False,
            "craft": False,
            "navi": False,
            "purchase": False,
            "fake": False,
            "loot": False,
            "climate": False,
            "respawn": False,
            "ratio_respawn": False,
        }
    )
    for fg, ft in c.execute(
        "select doodad_func_group_id, actual_func_type from doodad_funcs"
    ):
        aid = almighty.get(fg)
        if not aid:
            continue
        flags[aid]["types"].append(ft or "")
        if ft == "DoodadFuncQuest":
            flags[aid]["quest"] = True
        elif ft == "DoodadFuncResidentTownhallUiOpen":
            flags[aid]["resident"] = True
        elif ft == "DoodadFuncEvidenceItemLoot":
            flags[aid]["evidence"] = True
        elif ft == "DoodadFuncCraftPack":
            flags[aid]["craft"] = True
        elif ft == "DoodadFuncNaviOpenMailbox":
            flags[aid]["navi"] = True
        elif ft == "DoodadFuncPurchase":
            flags[aid]["purchase"] = True
        elif ft == "DoodadFuncFakeUse":
            flags[aid]["fake"] = True
        elif ft in ("DoodadFuncLootPack", "DoodadFuncLootItem"):
            flags[aid]["loot"] = True
        elif ft == "DoodadFuncClimateReact":
            flags[aid]["climate"] = True
        elif ft == "DoodadFuncRespawn":
            flags[aid]["respawn"] = True
        elif ft == "DoodadFuncRatioRespawn":
            flags[aid]["ratio_respawn"] = True

    # actual func tables for FakeUse / Purchase / Climate / Respawn on key templates
    def func_detail(tid: int) -> list[str]:
        out = []
        for (fg,) in c.execute(
            "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
        ):
            for fid, ft, nxt in c.execute(
                "select actual_func_id, actual_func_type, next_phase "
                "from doodad_funcs where doodad_func_group_id=?",
                (fg,),
            ):
                extra = ""
                if ft == "DoodadFuncFakeUse":
                    row = c.execute(
                        "select skill_id, fake_skill_id, target_parent "
                        "from doodad_func_fake_uses where id=?",
                        (fid,),
                    ).fetchone()
                    extra = f" skill={row}"
                elif ft == "DoodadFuncPurchase":
                    # probe columns
                    cols = [
                        r[1]
                        for r in c.execute("pragma table_info(doodad_func_purchases)")
                    ]
                    row = c.execute(
                        "select * from doodad_func_purchases where id=?", (fid,)
                    ).fetchone()
                    extra = f" cols={cols} row={row}"
                elif ft == "DoodadFuncClimateReact":
                    row = c.execute(
                        "select * from doodad_func_climate_reacts where id=?", (fid,)
                    ).fetchone()
                    extra = f" {row}"
                elif ft == "DoodadFuncRespawn":
                    row = c.execute(
                        "select * from doodad_func_respawns where id=?", (fid,)
                    ).fetchone()
                    extra = f" {row}"
                elif ft == "DoodadFuncRatioRespawn":
                    row = c.execute(
                        "select * from doodad_func_ratio_respawns where id=?", (fid,)
                    ).fetchone()
                    extra = f" {row}"
                out.append(f"{ft} id={fid} next={nxt}{extra}")
        return out

    bound = {
        r[0] for r in c.execute("select distinct doodad_id from housing_binding_doodads")
    }
    TYPE = re.compile(r"type\s+(\d+)")
    SPLIT = re.compile(r"(?m)^doodad\r?\n")
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
    rows = []
    for e in raw:
        tid = e.get("UnitId")
        if not tid or tid in g:
            continue
        m = meta.get(tid)
        if not m or m["gid"] != 24:
            continue
        fl = flags[tid]
        if (
            fl["quest"]
            or fl["resident"]
            or fl["craft"]
            or fl["navi"]
            or fl["evidence"]
            or tid in bound
        ):
            continue
        pos = e.get("Position") or {}
        rows.append(
            {
                "tid": int(tid),
                "x": float(pos.get("X") or 0),
                "y": float(pos.get("Y") or 0),
                "z": float(pos.get("Z") or 0),
                "title": e.get("Title") or "",
            }
        )

    emit(f"SkipRandomPlacement rows: {len(rows)} templates: {len({r['tid'] for r in rows})}")

    by = Counter(r["tid"] for r in rows)

    def bucket(tid: int) -> str:
        fl = flags[tid]
        model = meta[tid]["model"].lower()
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        name = (nm[0] if nm else "").lower()
        if fl["purchase"] or "fence." in model or "fence_" in model or "housing_fence" in model:
            return "purchase_housing_skin"
        if "env.cloud" in model or "env.rain" in model or "env.vortex" in model or "env.thunder" in model:
            return "weather_volume"
        if "area" in name and ("cloud" in name or "rain" in name or "maelstrom" in name or "thunder" in name):
            return "weather_volume"
        if "sunken" in name or "watercase" in model or tid in (10712, 3484):
            return "sunken_treasure"
        if "barrel" in name or "crate" in name or "jar" in name:
            return "ndeco_random_loot"
        if "spyglass" in name or "telescope" in model:
            return "spyglass_quest_prop"
        if "buried" in name or "cargo" in name or "ship" in name:
            return "salvage_wreck"
        if "invisible" in model or "shadow mine" in name:
            return "invisible_skill_volume"
        if fl["fake"] and fl["loot"]:
            return "ndeco_random_loot"
        if fl["fake"]:
            return "fakeuse_other"
        if not fl["types"]:
            return "no_funcs"
        return "other"

    buckets = Counter()
    bucket_tpl = defaultdict(Counter)
    for tid, n in by.items():
        b = bucket(tid)
        buckets[b] += n
        bucket_tpl[b][tid] = n

    emit("--- buckets ---")
    for b, n in buckets.most_common():
        emit(f"  {b}: {n} rows, {len(bucket_tpl[b])} templates")

    for b in (
        "sunken_treasure",
        "weather_volume",
        "ndeco_random_loot",
        "purchase_housing_skin",
        "salvage_wreck",
        "spyglass_quest_prop",
        "invisible_skill_volume",
        "fakeuse_other",
        "no_funcs",
        "other",
    ):
        if b not in bucket_tpl:
            continue
        emit("")
        emit(f"=== {b} ===")
        for tid, n in bucket_tpl[b].most_common(25):
            nm = c.execute(
                "select en_us from localized_texts where tbl_name='doodad_almighties' "
                "and tbl_column_name='name' and idx=?",
                (tid,),
            ).fetchone()
            fl = flags[tid]
            zs = [r["z"] for r in rows if r["tid"] == tid]
            emit(
                f"  {tid} x{n} '{nm[0] if nm else ''}' "
                f"z=[{min(zs):.1f}..{max(zs):.1f}] "
                f"fake={fl['fake']} loot={fl['loot']} purchase={fl['purchase']} "
                f"climate={fl['climate']} respawn={fl['respawn']} ratio={fl['ratio_respawn']}"
            )
            if tid in (10712, 3484, 3225, 3085, 3086, 3226, 10763, 2831, 2828, 10626, 6129) or n >= 30:
                for line in func_detail(tid)[:8]:
                    emit(f"    {line}")

    # sunken chest stand geography
    emit("")
    emit("=== sunken 10712 stand sample (first 15) ===")
    for r in [x for x in rows if x["tid"] == 10712][:15]:
        emit(f"  ({r['x']:.1f}, {r['y']:.1f}, {r['z']:.1f})")

    # FakeUse skill for 10712 — what skill?
    emit("")
    emit("=== doodad_func_fake_uses schema / 10712 ===")
    cols = [r[1] for r in c.execute("pragma table_info(doodad_func_fake_uses)")]
    emit(f"cols {cols}")
    for line in func_detail(10712):
        emit(f"  {line}")
    # also 3484 if exists
    if 3484 in meta:
        emit("3484 detail:")
        for line in func_detail(3484):
            emit(f"  {line}")
        emit(f"3484 in .g? {3484 in g} json count {sum(1 for e in raw if e.get('UnitId')==3484)}")

    # purchase schema
    emit("")
    emit("=== doodad_func_purchases ===")
    cols = [r[1] for r in c.execute("pragma table_info(doodad_func_purchases)")]
    emit(f"cols {cols}")
    # sample 10763
    for line in func_detail(10763)[:3]:
        emit(f"  {line}")

    # climate react — any of weather volumes?
    emit("")
    emit("=== climate react on weather templates? ===")
    for tid in (3225, 3085, 3086, 3226):
        emit(f"  {tid}: climate={flags[tid]['climate']} types={flags[tid]['types']}")
        # zone_climate links?
    # who references climate react
    climate_aids = [tid for tid, fl in flags.items() if fl["climate"]]
    emit(f"templates with ClimateReact: {len(climate_aids)}")
    emit(f"  overlap SkipRandom: {sorted(set(climate_aids) & set(by))[:20]}")

    # ratio/respawn on sunken or barrels
    emit("")
    emit("=== respawn/ratio on top fakeuse templates ===")
    for tid in list(by.keys()):
        if flags[tid]["respawn"] or flags[tid]["ratio_respawn"]:
            emit(f"  {tid} x{by[tid]} respawn={flags[tid]['respawn']} ratio={flags[tid]['ratio_respawn']}")

    # dig treasure vs sunken — groups 55-59
    emit("")
    emit("=== compare dig-treasure groups (already salvaged) vs sunken ===")
    for gid in range(55, 60):
        n = c.execute(
            "select count(*) from doodad_almighties where group_id=?", (gid,)
        ).fetchone()[0]
        emit(f"  g{gid} templates {n}")

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    emit(f"\nwrote {OUT}")


if __name__ == "__main__":
    main()
