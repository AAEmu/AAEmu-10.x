using AAEmu.Game.Models.Game.Quests;
using AAEmu.World.Core.Relay;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

/// <summary>
/// Starter-chain doodad placement lives in cell <c>doodad.g</c> (LevelPack), not in a repo
/// <c>doodad_spawns.json</c>. These tests pin the aggregate actor set and the two
/// <c>npctype://</c> report-body conversions from their authored cell-local rows.
/// Live presence is audited at World boot by <c>StarterChainActorAudit</c>.
/// </summary>
public class StarterChainSpawnDataTests
{
    [Test]
    public async Task StarterChains_Collect_YieldsEveryStarterChainDoodad()
    {
        var required = StarterChainActorRules.Collect(StarterChainActorRulesTests.AllChains);

        await Assert.That(string.Join(",", required.Doodads.OrderBy(id => id)))
            .IsEqualTo("1427,1473,2881,11624,11641,14178,14206");
    }

    [Test]
    public async Task LevelPack_PlaceElfArenaBodyOnItsCellRow()
    {
        // level_design/cells/010_014/doodad.g: type 14178, pos (27.3228, 931.893, 239.544),
        // ori (0, 0, 0.93358, -0.358368); cell 10,14 → world 10240 + x, 14336 + y.
        var path = Path.Combine(Path.GetTempPath(), $"starter_elf_{Guid.NewGuid():N}.g");
        await File.WriteAllTextAsync(path, """
            doodad
                category 17
                type 14178
                family 0
                vegetation false
                pos ( x 27.3228, y 931.893, z 239.544 )
                ori ( x 0, y 0, z 0.93358, w -0.358368 )
                scale 1
            """);
        try
        {
            var list = ZoneDoodadPlacementCatalog.ParseFile(path, cellX: 10, cellY: 14);
            await Assert.That(list.Count).IsEqualTo(1);
            await Assert.That(list[0].TemplateId).IsEqualTo(14178u);
            await Assert.That(list[0].X).IsEqualTo(10267.3228f).Within(0.01f);
            await Assert.That(list[0].Y).IsEqualTo(15267.893f).Within(0.01f);
            await Assert.That(list[0].Z).IsEqualTo(239.544f).Within(0.01f);
            // Catalog yaw is Atan2(z,w)*360/π → 222° here (same facing as json's -138°).
            await Assert.That(list[0].YawDegrees).IsEqualTo(222f).Within(0.01f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task LevelPack_PlaceDwarfDaughterBodyOnItsCellRow()
    {
        // level_design/cells/008_013/doodad.g: type 14206, pos (265.12, 155.49, 696.871),
        // ori (0, 0, 0.996918, 0.0784556); cell 8,13 → world 8192 + x, 13312 + y.
        var path = Path.Combine(Path.GetTempPath(), $"starter_dwarf_{Guid.NewGuid():N}.g");
        await File.WriteAllTextAsync(path, """
            doodad
                category 17
                type 14206
                family 0
                vegetation false
                pos ( x 265.12, y 155.49, z 696.871 )
                ori ( x 0, y 0, z 0.996918, w 0.0784556 )
                scale 1
            """);
        try
        {
            var list = ZoneDoodadPlacementCatalog.ParseFile(path, cellX: 8, cellY: 13);
            await Assert.That(list.Count).IsEqualTo(1);
            await Assert.That(list[0].TemplateId).IsEqualTo(14206u);
            await Assert.That(list[0].X).IsEqualTo(8457.12f).Within(0.01f);
            await Assert.That(list[0].Y).IsEqualTo(13467.49f).Within(0.01f);
            await Assert.That(list[0].Z).IsEqualTo(696.871f).Within(0.01f);
            await Assert.That(list[0].YawDegrees).IsEqualTo(171f).Within(0.01f);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
