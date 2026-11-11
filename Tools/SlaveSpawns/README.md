# Official slave spawns

Generates `AAEmu.Game/Data/Worlds/<world>/slave_spawns_official.json` from the client's
`game/worlds/<world>/level_design/zone/*/editor/slave.xml` (cannons, siege weapons, wagons, turrets and boats
the designers placed; the same rows as `world_server/slave.g`). Run it over every world, so none is left out:

```
python -I -X utf8 slaves_inv.py --all --json rows.json   # inventory + coverage table of every world
python -I -X utf8 slaves_gen.py rows.json                # writes one file per world that has placements
```

`slaves_inv.py` also takes world names instead of `--all`. Boats are left out unless `slaves_gen.py` gets
`--boats`, which writes them to `slave_spawns_official_boats.json`.

Placements already in the hand-written `slave_spawns.json` are left out: a row of the same slave within 0.5 m
counts as that placement. A hand-written row further off is a different object and its official placement is
generated as well, so move such a row onto the official spot or remove it. A generated file that ends up with
no placements is removed, because the World loads every `slave_spawns*.json`. A placement whose slave the
server DB lacks is skipped with a warning.

World position = zone origin (`world.xml` originX/originY) x 1024 + slave.xml Pos; yaw = 2*atan2(Rotate[3], Rotate[0]).
`AAEMU_CLIENT_WORLDS` must point at the client's unpacked `game/worlds` folder. The server DB defaults to
`AAEmu.Game/Data/compact.sqlite3`; set `AAEMU_SERVER_DB` to use another copy.
