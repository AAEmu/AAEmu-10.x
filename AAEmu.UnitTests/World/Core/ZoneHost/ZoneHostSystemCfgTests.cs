using AAEmu.Game.Models.Game.World;
using AAEmu.World.Core.ZoneHost;
using AAEmu.World.Models;

namespace AAEmu.UnitTests.World.Core.ZoneHost;

/// <summary>
/// The native zone host ignores +db_location and reads system.cfg from its save folder
/// (AAEMU_ZONE_SAVE_DIR). Without that file a dungeon copy loads the client DB and fails with
/// "failed to load game data".
/// </summary>
public class ZoneHostSystemCfgTests
{
    private sealed class FakeProcess : IZoneHostProcess
    {
        public int Id => 1;
        public bool HasExited => false;
        public bool WaitForExit(int milliseconds) => false;
        public void KillTree() { }
    }

    private sealed class FakeFactory : IZoneHostProcessFactory
    {
        public List<ZoneHostLaunchSpec> Specs { get; } = [];

        public IZoneHostProcess Start(ZoneHostLaunchSpec spec)
        {
            Specs.Add(spec);
            return new FakeProcess();
        }
    }

    [Test]
    public async Task SystemCfg_CarriesTheDbLocation()
    {
        var text = ZoneHostCommandLine.SystemCfg(new ZoneHostConfig { DbLocation = "game/db/game_decrypted.sqlite3" });

        await Assert.That(text).IsEqualTo("db_location = game/db/game_decrypted.sqlite3\n");
    }

    [Test]
    public async Task TryStart_WritesSystemCfgIntoTheSaveFolderBeforeLaunch()
    {
        var factory = new FakeFactory();
        var writes = new List<(string Path, string Text, int SpecsAtWrite)>();
        var supervisor = new ZoneHostSupervisor(
            new ZoneHostConfig
            {
                Enabled = true,
                Executable = Path.Combine(Path.GetTempPath(), "AAEmu.ZoneHost.exe"),
                WorkingDirectory = Path.GetTempPath(),
                NativeDll = Path.Combine(Path.GetTempPath(), "zone_native.dll"),
                DbLocation = "game/db/game_decrypted.sqlite3"
            },
            factory,
            fileExists: _ => true,
            ensureDirectory: _ => { },
            writeFile: (path, text) => writes.Add((path, text, factory.Specs.Count)));

        var started = supervisor.TryStart(
            new WorldInstance(new WorldTemplate { Name = "instance_life_dungeon_daru" }, 0, true, 100));

        await Assert.That(started).IsTrue();
        var saveDir = factory.Specs[0].Environment[ZoneHostCommandLine.SaveDirectoryEnvironment];
        await Assert.That(writes.Count).IsEqualTo(1);
        await Assert.That(writes[0].Path).IsEqualTo(Path.Combine(saveDir, "system.cfg"));
        await Assert.That(writes[0].Text).IsEqualTo("db_location = game/db/game_decrypted.sqlite3\n");
        await Assert.That(writes[0].SpecsAtWrite).IsEqualTo(0);
    }
}
