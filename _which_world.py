import json
from pathlib import Path

import subprocess

out = subprocess.check_output(
    [
        "powershell",
        "-NoProfile",
        "-Command",
        "Get-CimInstance Win32_Process -Filter \"name='AAEmu.World.exe'\" | "
        "ForEach-Object { $_.ProcessId.ToString() + '|' + $_.ExecutablePath }",
    ],
    text=True,
)
print("procs:")
print(out)

for p in [
    Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.Game\bin\Debug\net10.0\Configurations\World.json"),
    Path(r"G:\AAchina\repos\AAEmu-map-doodad-pr\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Configurations\World.json"),
    Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.Game\bin\Debug\net10.0\Configurations\World.json"),
    Path(r"G:\AAchina\repos\AAEmu-zone-10.0.2\AAEmu.WorldServer\AAEmu.World\bin\Debug\net10.0\Configurations\World.json"),
]:
    if not p.exists():
        print("missing", p)
        continue
    d = json.loads(p.read_text(encoding="utf-8-sig"))
    w = d.get("World") or d
    print(p)
    print("  MapDoodadSource=", w.get("MapDoodadSource"), "SpawnDoodads=", w.get("SpawnDoodads"))
