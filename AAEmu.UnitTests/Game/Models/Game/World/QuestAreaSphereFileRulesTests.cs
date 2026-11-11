using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.World;

public class QuestAreaSphereFileRulesTests
{
    private const string Pak243 = "game/worlds/main_world/level_design/zone/243/world_server/quest_area_sphere.g";
    private const string Loose243 = @"D:\Games\ArcheAge\game\worlds\main_world\level_design\zone\243\world_server\quest_area_sphere.g";
    private const string Loose244 = @"D:\Games\ArcheAge\game\worlds\main_world\level_design\zone\244\world_server\quest_area_sphere.g";

    [Test]
    public async Task SameZoneListedByPakAndLooseScan_LoadsOnce_PakCopyFirst()
    {
        // Live 2026-11-04 loaded 206 files for 103 zones, so sphere 1290 (quest 3766) logged ENTER twice.
        var files = QuestAreaSphereFileRules.OnePerZone([Pak243, Loose243, Loose244]);

        await Assert.That(files).IsEquivalentTo(new[] { Pak243, Loose244 });
    }

    [Test]
    public async Task LooseScanStillFillsZonesThePakLacks()
    {
        var files = QuestAreaSphereFileRules.OnePerZone([Loose243, Loose244]);

        await Assert.That(files.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PathWithoutZoneId_IsKeptForTheLoaderToReport()
    {
        const string odd = "game/worlds/main_world/level_design/zone/quest_area_sphere.g";

        var files = QuestAreaSphereFileRules.OnePerZone([odd, odd]);

        await Assert.That(files.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments(Pak243, 243u)]
    [Arguments(Loose244, 244u)]
    public async Task ZoneId_ParsesFromEitherSlash(string path, uint expected)
    {
        await Assert.That(QuestAreaSphereFileRules.TryParseZoneId(path, out var zoneId)).IsTrue();
        await Assert.That(zoneId).IsEqualTo(expected);
    }
}
