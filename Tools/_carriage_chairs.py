import sqlite3

c = sqlite3.connect("file:G:/AAchina/client/game/db/compact.sqlite3?mode=ro", uri=True)
print("transfer tables", [r[0] for r in c.execute(
    "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%transfer%'"
)])
print("slave_bindings cols", [r[1] for r in c.execute("PRAGMA table_info(slave_bindings)")])
print("slave_doodad_bindings?", [r[0] for r in c.execute(
    "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%doodad_bind%'"
)])

# doodad bindings for slaves
for t in ("slave_doodad_bindings", "doodad_bindings", "slave_binding_doodads"):
    try:
        n = c.execute(f"SELECT COUNT(*) FROM {t}").fetchone()[0]
        print(t, n)
        print("cols", [r[1] for r in c.execute(f"PRAGMA table_info({t})")])
    except Exception as e:
        print(t, e)

# item_slave_equipments has doodad_id
print("item_slave_equipments cols", [r[1] for r in c.execute("PRAGMA table_info(item_slave_equipments)")])
chairs = c.execute(
    """
SELECT ise.item_id, ise.doodad_id, ise.slave_id, d.name, d.model
FROM item_slave_equipments ise
JOIN doodad_almighties d ON d.id = ise.doodad_id
WHERE ise.doodad_id > 0
  AND (d.name LIKE '%의자%' OR lower(d.name) LIKE '%chair%' OR lower(d.model) LIKE '%chair%'
       OR lower(d.model) LIKE '%seat%')
LIMIT 30
"""
).fetchall()
print("equip chair doodads", len(chairs))
for r in chairs:
    print(r)

# transfers content
try:
    print("transfers cols", [r[1] for r in c.execute("PRAGMA table_info(transfers)")])
    print("sample transfers", c.execute("SELECT id, name, model_id FROM transfers LIMIT 15").fetchall())
except Exception as e:
    print(e)
