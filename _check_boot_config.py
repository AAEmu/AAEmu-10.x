import pathlib

path = r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Logs\Server.log"
print("=== log boot markers ===")
with open(path, "r", encoding="utf-8", errors="ignore") as f:
    for line in f:
        if not (line.startswith("23:08") or line.startswith("23:09") or line.startswith("23:10") or line.startswith("23:11")):
            continue
        if (
            ("skipping" in line and "doodad" in line.lower())
            or "MapDoodad" in line
            or ("Spawning" in line and "Doodad" in line)
            or "Loading Doodad spawn" in line
            or "doodad_spawns" in line
        ):
            if "WARN" in line and "no zone" in line:
                continue
            print(line.rstrip()[-240:])

for p in [
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\Configurations\World.json",
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\bin\Debug\net10.0\Configurations\World.json",
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Configurations\World.json",
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\bin\Debug\net10.0\Config.Local.json",
    r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Config.Local.json",
]:
    path = pathlib.Path(p)
    print(path.exists(), p)
    if not path.exists():
        continue
    t = path.read_text(encoding="utf-8")
    for line in t.splitlines():
        if "MapDoodad" in line:
            print(" ", line.strip())
