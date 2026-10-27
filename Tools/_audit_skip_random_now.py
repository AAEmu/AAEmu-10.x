#!/usr/bin/env python3
"""Re-classify Interaction json orphans against CURRENT MapDoodadOrphanRules signals."""
from __future__ import annotations

import json
import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
WORLDS = Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Data\Worlds")
OUT = Path(__file__).with_name("_audit_skip_random_now.txt")

INTERACTION = 24
HOUSING = {25, 26}  # guess — filled from enum below


def main() -> None:
    lines: list[str] = []

    def p(*a):
        lines.append(" ".join(str(x) for x in a))

    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)

    # groups from almighties
    group = {r[0]: r[1] for r in c.execute("select id, group_id from doodad_almighties")}
    model = {r[0]: (r[1] or "") for r in c.execute("select id, model from doodad_almighties")}

    # level-pack template set: approximate via .g? Use empty — we only care Interaction SkipRandom path
    # for templates that are json-only Interaction. Census uses templateInLevelPack from .g scan;
    # for this audit we only re-eval Interaction rows that classify previously called SkipRandom.

    # Build per-template func flags mirroring CollectFuncFlags
    gimmick_skills = {
        r[0]
        for r in c.execute(
            """
            select distinct se.skill_id
            from skill_effects se
            join effects e on e.id = se.effect_id
            where e.actual_type = 'SpawnGimmickEffect'
            """
        )
    }

    telescope_skills = set()
    try:
        buffs = {
            r[0]
            for r in c.execute(
                "select id from buffs where transfer_telescope_range is not null "
                "and transfer_telescope_range > 0"
            )
        }
    except Exception:
        buffs = set()
    for sid, aid in c.execute(
        """
        select se.skill_id, e.actual_id
        from skill_effects se
        join effects e on e.id = se.effect_id
        where e.actual_type = 'BuffEffect'
        """
    ):
        if aid in buffs:
            telescope_skills.add(sid)

    print("gimmick_skills", sorted(gimmick_skills)[:20], "n=", len(gimmick_skills))
    print("15341 in gimmick?", 15341 in gimmick_skills)

    flags: dict[int, dict] = {}
    for tid in group:
        has_evidence = False
        has_resident = False
        has_quest = False
        has_craft = False
        has_navi = False
        has_salvage = False
        has_purchase = False
        has_store = False
        has_telescope = False
        has_fake = False
        has_ratio = False
        has_world = False
        has_loot = False
        gids = [
            r[0]
            for r in c.execute(
                "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
            )
        ]
        for gid in gids:
            for ft, fid in c.execute(
                "select actual_func_type, actual_func_id from doodad_funcs "
                "where doodad_func_group_id=?",
                (gid,),
            ):
                if ft == "DoodadFuncEvidenceItemLoot":
                    has_evidence = True
                elif ft == "DoodadFuncResidentTownhallUiOpen":
                    has_resident = True
                elif ft == "DoodadFuncQuest":
                    has_quest = True
                elif ft == "DoodadFuncCraftPack":
                    has_craft = True
                elif ft == "DoodadFuncNaviOpenMailbox":
                    has_navi = True
                elif ft == "DoodadFuncPurchase":
                    has_purchase = True
                elif ft == "DoodadFuncStoreUi":
                    has_store = True
                elif ft in (
                    "DoodadFuncUse",
                    "DoodadFuncSkillHit",
                    "DoodadFuncOpenPaper",
                    "DoodadFuncDevote",
                ):
                    has_world = True
                elif ft in ("DoodadFuncLootPack", "DoodadFuncLootItem"):
                    has_loot = True
                elif ft == "DoodadFuncFakeUse":
                    has_fake = True
                    row = c.execute(
                        "select skill_id, fake_skill_id from doodad_func_fake_uses where id=?",
                        (fid,),
                    ).fetchone()
                    if row:
                        for s in row:
                            if s and s in gimmick_skills:
                                has_salvage = True
                            if s and s in telescope_skills:
                                has_telescope = True
            for ft, _ in c.execute(
                "select actual_func_type, actual_func_id from doodad_phase_funcs "
                "where doodad_func_group_id=?",
                (gid,),
            ):
                if ft == "DoodadFuncRatioRespawn":
                    has_ratio = True
        flags[tid] = {
            "evidence": has_evidence,
            "resident": has_resident,
            "quest": has_quest,
            "craft": has_craft,
            "navi": has_navi,
            "salvage": has_salvage,
            "purchase": has_purchase,
            "store": has_store,
            "telescope": has_telescope,
            "flavor": has_fake and has_ratio,
            "world": has_world,
            "fake": has_fake,
            "ratio": has_ratio,
            "loot": has_loot,
        }

    def decide(tid: int) -> str:
        fl = flags[tid]
        gid = group[tid] or 0
        if fl["evidence"]:
            return "SkipRuntimeEvidence"
        # Dig/resource/sale omitted — not Interaction focus
        if gid == INTERACTION:
            if (
                fl["resident"]
                or fl["quest"]
                or fl["craft"]
                or fl["navi"]
                or fl["salvage"]
                or fl["purchase"]
                or fl["store"]
                or fl["telescope"]
                or fl["flavor"]
                or fl["world"]
            ):
                return "Plant"
            return "SkipRandomPlacement"
        if fl["quest"] or fl["craft"] or fl["navi"]:
            return "Plant"
        return "SkipJsonOrphanUnhandled"

    # Collect Interaction json rows
    rows: list[tuple[int, str, float]] = []
    for jpath in WORLDS.rglob("doodad_spawns.json"):
        world = jpath.parent.name
        data = json.loads(jpath.read_text(encoding="utf-8"))
        for e in data:
            tid = e.get("UnitId") or e.get("unitId") or e.get("TemplateId")
            if not tid:
                continue
            tid = int(tid)
            if group.get(tid) != INTERACTION:
                continue
            pos = e.get("Position") or e.get("position") or {}
            z = float(pos.get("Z") or pos.get("z") or 0)
            rows.append((tid, world, z))

    p("Interaction json rows", len(rows), "templates", len({t for t, _, _ in rows}))
    by_dec = Counter()
    by_dec_tpl: dict[str, Counter] = defaultdict(Counter)
    for tid, _, _ in rows:
        d = decide(tid)
        by_dec[d] += 1
        by_dec_tpl[d][tid] += 1

    p("--- decisions ---")
    for d, n in by_dec.most_common():
        p(f"  {d}: {n} rows, {len(by_dec_tpl[d])} templates")

    for d in ("Plant", "SkipRandomPlacement", "SkipRuntimeEvidence", "SkipJsonOrphanUnhandled"):
        if d not in by_dec_tpl:
            continue
        p("")
        p(f"=== {d} (top) ===")
        for tid, n in by_dec_tpl[d].most_common(40):
            fl = flags[tid]
            nm = c.execute(
                "select en_us from localized_texts where tbl_name='doodad_almighties' "
                "and tbl_column_name='name' and idx=?",
                (tid,),
            ).fetchone()
            sig = []
            for k in (
                "salvage",
                "flavor",
                "purchase",
                "store",
                "telescope",
                "world",
                "quest",
                "craft",
                "fake",
                "ratio",
                "loot",
            ):
                if fl[k]:
                    sig.append(k)
            p(
                f"  {tid} x{n} '{nm[0] if nm else ''}' "
                f"[{','.join(sig) or 'NONE'}] model={model[tid][:60]}"
            )

    # Focus: still SkipRandom and what signal they lack
    p("")
    p("=== SkipRandom residual detail ===")
    for tid, n in by_dec_tpl.get("SkipRandomPlacement", Counter()).most_common():
        fl = flags[tid]
        nm = c.execute(
            "select en_us from localized_texts where tbl_name='doodad_almighties' "
            "and tbl_column_name='name' and idx=?",
            (tid,),
        ).fetchone()
        # list func types
        types = Counter()
        for gid in c.execute(
            "select id from doodad_func_groups where doodad_almighty_id=?", (tid,)
        ):
            for (ft,) in c.execute(
                "select actual_func_type from doodad_funcs where doodad_func_group_id=?",
                (gid[0],),
            ):
                types[ft] += 1
            for (ft,) in c.execute(
                "select actual_func_type from doodad_phase_funcs where doodad_func_group_id=?",
                (gid[0],),
            ):
                types["p:" + ft] += 1
        p(f"  {tid} x{n} '{nm[0] if nm else ''}' funcs={dict(types)}")

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {OUT} lines={len(lines)}")


if __name__ == "__main__":
    main()
