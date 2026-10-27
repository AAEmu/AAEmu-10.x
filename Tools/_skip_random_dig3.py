#!/usr/bin/env python3
from __future__ import annotations

import sqlite3
from pathlib import Path

DB = Path(r"G:\AAchina\client\game\db\compact.sqlite3")
OUT = Path(__file__).with_name("_skip_random_dig3.txt")


def main() -> None:
    c = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
    lines: list[str] = []

    def p(*a) -> None:
        lines.append(" ".join(str(x) for x in a))

    for did in (649, 650, 750, 751):
        row = c.execute("select * from housing_decorations where id=?", (did,)).fetchone()
        cols = [x[1] for x in c.execute("pragma table_info(housing_decorations)")]
        p("design", did, dict(zip(cols, row)) if row else None)

    # FakeUse skill -> SpawnGimmickEffect
    fakes = c.execute(
        "select f.id, f.skill_id, f.fake_skill_id, g.doodad_almighty_id, a.name, a.group_id "
        "from doodad_func_fake_uses f "
        "join doodad_funcs df on df.actual_func_type='DoodadFuncFakeUse' and df.actual_func_id=f.id "
        "join doodad_func_groups g on g.id=df.doodad_func_group_id "
        "join doodad_almighties a on a.id=g.doodad_almighty_id"
    ).fetchall()
    p("fakeuse rows linked", len(fakes))
    spawn_gimmick = []
    for fid, sid, fsid, aid, name, gid in fakes:
        if not sid:
            continue
        eids = [
            r[0]
            for r in c.execute(
                "select effect_id from skill_effects where skill_id=?", (sid,)
            )
        ]
        for eid in eids:
            row = c.execute(
                "select actual_type, actual_id from effects where id=?", (eid,)
            ).fetchone()
            if row and row[0] == "SpawnGimmickEffect":
                spawn_gimmick.append((aid, name, gid, sid, fid))
                break
    p("FakeUse+SpawnGimmickEffect templates:")
    for row in sorted(set(spawn_gimmick)):
        p(" ", row)

    # purchase stands: are they arche_mall / housing plaza?
    # design doodad vs purchase vendor doodad
    p(
        "purchase vendor 10734 vs design doodad",
        c.execute("select id, name, doodad_id from housing_decorations where id=650").fetchone(),
    )

    # Count SkipRandom purchase templates that sell item_housing_decorations
    purch = c.execute(
        "select a.id, a.name, p.item_id, ihd.design_id, hd.doodad_id, hd.name "
        "from doodad_funcs df "
        "join doodad_func_groups g on g.id=df.doodad_func_group_id "
        "join doodad_almighties a on a.id=g.doodad_almighty_id "
        "join doodad_func_purchases p on p.id=df.actual_func_id "
        "left join item_housing_decorations ihd on ihd.item_id=p.item_id "
        "left join housing_decorations hd on hd.id=ihd.design_id "
        "where df.actual_func_type='DoodadFuncPurchase' and a.id between 10730 and 10780 "
        "limit 20"
    ).fetchall()
    p("purchase->decoration:")
    for row in purch:
        p(" ", row)
        # vendor doodad id == placed doodad id?
        if row[0] == row[4]:
            p("   SAME doodad as decoration")
        else:
            p("   vendor", row[0], "places decoration doodad", row[4])

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(OUT.read_text(encoding="utf-8"))


if __name__ == "__main__":
    main()
