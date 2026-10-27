import json
import sqlite3
from pathlib import Path

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
c.row_factory = sqlite3.Row

# Diamond Shores / continent portal family
print("=== almighty 10521 and name neighbors ===")
for r in c.execute(
    """
    select id, name, group_id, model, client_doodad, percent, mgmt_spawn
    from doodad_almighties
    where id in (10521, 10520, 10522, 10523)
       or lower(name) like '%빛나는 해안%'
       or lower(name) like '%diamond%'
       or model like '%continent_portal%'
    order by id
    limit 40
    """
):
    print(dict(r))

print("\n=== localized diamond shores / continent portal ===")
for r in c.execute(
    """
    select idx, en_us, ko from localized_texts
    where tbl_name='doodad_almighties' and tbl_column_name='name'
      and (
        lower(ifnull(en_us,'')) like '%diamond shores%'
        or lower(ifnull(en_us,'')) like '%worldgate%'
           and lower(ifnull(en_us,'')) like '%diamond%'
        or lower(ifnull(ko,'')) like '%빛나는 해안%'
      )
    limit 40
    """
):
    print(tuple(r))

# group 43
print("\ngroup 43", tuple(c.execute("select id,name from doodad_groups where id=43").fetchone() or ()))

# func groups / phases for 10521
print("\n=== func groups for almighty 10521 ===")
for r in c.execute(
    """
    select id, doodad_almighty_id, doodad_func_group_kind_id, name, is_default, phase_msg_key
    from doodad_func_groups
    where doodad_almighty_id=10521
    order by id
    """
):
    print(tuple(r))

# doodad_funcs on those groups
print("\n=== funcs on 10521 groups ===")
for r in c.execute(
    """
    select f.id, f.doodad_func_group_id, f.actual_func_type, f.actual_func_id, f.next_phase, f.skill_id
    from doodad_funcs f
    join doodad_func_groups g on g.id = f.doodad_func_group_id
    where g.doodad_almighty_id=10521
    order by f.doodad_func_group_id, f.id
    """
):
    print(tuple(r))

# find closed twin by model path pattern
print("\n=== models with continent_portal ===")
for r in c.execute(
    """
    select id, name, group_id, model from doodad_almighties
    where model like '%continent_portal%'
    order by id
    """
):
    print(tuple(r))

# phase / ratio change tables?
tabs = {r[0] for r in c.execute("select name from sqlite_master where type='table'")}
for t in sorted(tabs):
    if "ratio" in t or "phase" in t and "doodad" in t:
        if "doodad" in t:
            print("table", t)

# doodad_func_ratio_changes mentioning these
if "doodad_func_ratio_changes" in tabs:
    print("\nratio_changes sample cols", [r[1] for r in c.execute("pragma table_info(doodad_func_ratio_changes)")])

# json presence near plaza
plaza = (11179.0, 12054.0)
for cand in [
    Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\Data\Worlds\main_world\doodad_spawns.json"),
]:
    data = json.loads(cand.read_text(encoding="utf-8"))
    for tid in (10521, 10520, 10522):
        hits = [d for d in data if d.get("UnitId") == tid]
        near = []
        for d in hits:
            pos = d.get("Position") or {}
            x, y = float(pos.get("X", 0)), float(pos.get("Y", 0))
            if abs(x - plaza[0]) < 120 and abs(y - plaza[1]) < 120:
                near.append((d.get("Id"), d.get("UnitId"), x, y, pos.get("Z"), d.get("funcGroupId")))
        print(f"json UnitId {tid}: total={len(hits)} near_plaza={near}")
