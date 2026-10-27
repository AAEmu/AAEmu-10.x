import sqlite3

c = sqlite3.connect(r"G:\AAchina\client\game\db\compact.sqlite3")
for tid in (8033, 10521):
    a = c.execute(
        "select id,name,group_id,model,client_doodad from doodad_almighties where id=?",
        (tid,),
    ).fetchone()
    loc = c.execute(
        "select en_us, ko from localized_texts where tbl_name='doodad_almighties' "
        "and tbl_column_name='name' and idx=?",
        (tid,),
    ).fetchone()
    print("TPL", tid, "almighty", a)
    print("  loc", loc)
    print("  func_groups:")
    for g in c.execute(
        "select id, name, doodad_func_group_kind_id, is_default from doodad_func_groups "
        "where doodad_almighty_id=? order by id",
        (tid,),
    ):
        print("   ", g)
    # phase models via doodad_func_fake_model / change model?
    print("  phase funcs:")
    for f in c.execute(
        """
        select f.id, f.doodad_func_group_id, f.actual_func_type, f.actual_func_id, f.next_phase
        from doodad_funcs f
        join doodad_func_groups g on g.id=f.doodad_func_group_id
        where g.doodad_almighty_id=?
        order by f.doodad_func_group_id, f.id
        """,
        (tid,),
    ):
        print("   ", f)

# same mark_model / family?
print("\nmark/model overlap:")
for tid in (8033, 10521):
    print(
        c.execute(
            "select id, mark_model, model, model_kind_id from doodad_almighties where id=?",
            (tid,),
        ).fetchone()
    )

# unit_reqs on enter / doodad
print("\nunit_reqs referencing 8033/10521:")
# try common tables
tabs = {r[0] for r in c.execute("select name from sqlite_master where type='table'")}
for t in ("unit_reqs", "doodad_func_require_items", "sphere_doodad_bindings"):
    if t in tabs:
        print(" has", t)

# search name twin closed/open
print("\nlocalized closed/open near diamond:")
for r in c.execute(
    """
    select idx, en_us, ko from localized_texts
    where tbl_name='doodad_almighties' and tbl_column_name='name'
      and (
        lower(ifnull(en_us,'')) like '%diamond shores%'
        or idx in (8033,10521)
      )
    """
):
    print(r)
