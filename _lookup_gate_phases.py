import sqlite3
import pathlib
import re

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")

ids = [6605, 8033, 8034, 10520, 10521, 10522, 10523]

print("=== almighties ===")
for tid in ids:
    a = c.execute(
        "select id, group_id, model, mark_model from doodad_almighties where id=?",
        (tid,),
    ).fetchone()
    print(tid, a)

# localized names
print("\n=== localized names (ko/en) ===")
for tid in ids:
    rows = c.execute(
        """
        select en_us, ko, zh_cn from localized_texts
        where tbl_name='doodad_almighties' and tbl_column_name='name' and idx=?
        """,
        (tid,),
    ).fetchall()
    print(tid, rows)

print("\n=== func_groups + funcs + phase_funcs ===")
for tid in ids:
    print(f"\n======== {tid} ========")
    groups = c.execute(
        """
        select id, model, doodad_func_group_kind_id, name, comment, phase_msg
        from doodad_func_groups where doodad_almighty_id=? order by id
        """,
        (tid,),
    ).fetchall()
    for g in groups:
        gid, model, kind, name, comment, pmsg = g
        print(f"  group {gid} kind={kind} model={model!r} name={name!r} comment={comment!r} phase_msg={pmsg!r}")
        for f in c.execute(
            """
            select id, actual_func_type, actual_func_id, next_phase, func_skill_id, act_count
            from doodad_funcs where doodad_func_group_id=? order by id
            """,
            (gid,),
        ):
            print(f"    func {f}")
            # resolve known types
            t, aid = f[1], f[2]
            if t == "DoodadFuncFakeUse":
                print("      fake_use", c.execute(
                    "select * from doodad_func_fake_uses where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncUse":
                print("      use", c.execute(
                    "select * from doodad_func_uses where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncEnterSysInstance":
                print("      enter", c.execute(
                    "select * from doodad_func_enter_sys_instances where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncConditionalUse":
                print("      cond", c.execute(
                    "select * from doodad_func_conditional_uses where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncRequireItem":
                print("      reqitem", c.execute(
                    "select * from doodad_func_require_items where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncTod":
                print("      tod", c.execute(
                    "select * from doodad_func_tods where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadFuncZoneReact":
                print("      zonereact", c.execute(
                    "select * from doodad_func_zone_reacts where id=?", (aid,)
                ).fetchone())
        for pf in c.execute(
            """
            select id, actual_func_type, actual_func_id
            from doodad_phase_funcs where doodad_func_group_id=? order by id
            """,
            (gid,),
        ):
            print(f"    phase {pf}")
            t, aid = pf[1], pf[2]
            if t == "DoodadPhaseFuncTod":
                print("      tod", c.execute(
                    "select * from doodad_phase_func_tods where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadPhaseFuncRatioChange":
                print("      ratio", c.execute(
                    "select * from doodad_phase_func_ratio_changes where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadPhaseFuncWeather":
                print("      weather", c.execute(
                    "select * from doodad_phase_func_weathers where id=?", (aid,)
                ).fetchone())
            elif t == "DoodadPhaseFuncTimeOfDay":
                try:
                    print("      tod2", c.execute(
                        "select * from doodad_phase_func_time_of_days where id=?", (aid,)
                    ).fetchone())
                except Exception:
                    pass

# find tables with condition-ish names
print("\n=== doodad_phase_func* tables ===")
for r in c.execute(
    "select name from sqlite_master where type='table' and name like 'doodad_phase_func%' order by 1"
):
    print(" ", r[0], [x[1] for x in c.execute(f"pragma table_info({r[0]})")])

print("\n=== doodad_func* with cond/tod/require/weather/zone ===")
for r in c.execute(
    """
    select name from sqlite_master where type='table'
    and name like 'doodad_func%'
    and (name like '%cond%' or name like '%tod%' or name like '%require%'
         or name like '%weather%' or name like '%zone%' or name like '%quest%'
         or name like '%ratio%' or name like '%fake%')
    order by 1
    """
):
    cols = [x[1] for x in c.execute(f"pragma table_info({r[0]})")]
    print(" ", r[0], cols)

# Scan doodad.g cells near Diamond Shores (cell = floor(world/1024))
# 11134/1024 ≈ 10, 12046/1024 ≈ 11 → cell 010_011 / 011_011
cells = [
    r"G:\AAchina\client\game\worlds\main_world\level_design\cells\010_011\doodad.g",
    r"G:\AAchina\client\game\worlds\main_world\level_design\cells\011_011\doodad.g",
    r"G:\AAchina\Server\game\worlds\main_world\level_design\cells\010_011\doodad.g",
    r"G:\AAchina\Server\game\worlds\main_world\level_design\cells\011_011\doodad.g",
]
# also search nearby cells
for cx in (10, 11):
    for cy in (11, 12):
        for root in (
            r"G:\AAchina\client\game\worlds\main_world\level_design\cells",
            r"G:\AAchina\Server\game\worlds\main_world\level_design\cells",
        ):
            cells.append(fr"{root}\{cx:03d}_{cy:03d}\doodad.g")

seen = set()
print("\n=== doodad.g hits for gate types near XYZ ===")
pat = re.compile(
    r"doodad\s*\{[^}]*?type\s+(\d+)[^}]*?pos\s*=\s*\{([^}]+)\}",
    re.I | re.S,
)
# better block parser
type_re = re.compile(r"\btype\s+(\d+)")
pos_re = re.compile(r"pos\s*=\s*\{\s*([-\d.]+)\s*,\s*([-\d.]+)\s*,\s*([-\d.]+)")
yaw_re = re.compile(r"\byaw\s*=\s*([-\d.]+)")

for path in cells:
    if path in seen:
        continue
    seen.add(path)
    p = pathlib.Path(path)
    if not p.exists():
        continue
    text = p.read_text(encoding="utf-8", errors="ignore")
    # split on doodad {
    parts = re.split(r"(?=\bdoodad\s*\{)", text)
    for block in parts:
        if not block.lstrip().startswith("doodad"):
            continue
        tm = type_re.search(block)
        pm = pos_re.search(block)
        if not tm or not pm:
            continue
        tid = int(tm.group(1))
        if tid not in ids:
            continue
        lx, ly, lz = map(float, pm.groups())
        # cell local → world
        m = re.search(r"(\\d+)_(\\d+)", str(p.parent.name) if False else p.parent.name)
        # parent name is 010_011
        cx, cy = map(int, p.parent.name.split("_"))
        wx, wy, wz = cx * 1024 + lx, cy * 1024 + ly, lz
        if abs(wx - 11134.5) < 5 and abs(wy - 12046.3) < 5:
            ym = yaw_re.search(block)
            print(f"  {p}: type={tid} local=({lx},{ly},{lz}) world=({wx:.3f},{wy:.3f},{wz:.3f}) yaw={ym.group(1) if ym else None}")
            # print brief block head
            print("   ", " ".join(block.split()[:40]))
