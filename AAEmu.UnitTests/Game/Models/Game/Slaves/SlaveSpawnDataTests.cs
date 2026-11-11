using AAEmu.Commons.Utils;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

/// <summary>
/// Guards the shipped <c>Data/Worlds/*/slave_spawns*.json</c> files (copied next to the test binary by the Game
/// project). The <c>slave_spawns_official*.json</c> files are generated from the client's
/// <c>level_design/zone/*/editor/slave.xml</c> (the same rows as <c>world_server/slave.g</c>); before them the
/// World spawned 24 of the 169 official main-world slaves, which left quest 1401 "Cannons for Questions" without
/// its Andelph Cannons.
/// </summary>
public class SlaveSpawnDataTests
{
    private sealed class SpawnRow
    {
        public uint UnitId { get; set; }
        public PositionRow Position { get; set; }
    }

    private sealed class PositionRow
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
    }

    private static string WorldsFolder => Path.Combine(AppContext.BaseDirectory, "Data", "Worlds");

    /// <summary>Every slave spawn row of one world, read the way SpawnManager.LoadSlaveSpawns reads them.</summary>
    private static List<(string File, SpawnRow Row)> ReadWorld(string world)
    {
        var rows = new List<(string, SpawnRow)>();
        foreach (var path in Directory.GetFiles(Path.Combine(WorldsFolder, world), "slave_spawns*.json"))
        {
            if (!JsonHelper.TryDeserializeObject(File.ReadAllText(path), out List<SpawnRow> spawns, out var error))
                throw new InvalidDataException($"{path} does not parse: {error?.Message}");
            rows.AddRange(spawns.Select(row => (Path.GetFileName(path), row)));
        }

        return rows;
    }

    [Test]
    public async Task EveryShippedSlaveSpawnFile_Parses_WithAPositionOffTheOrigin()
    {
        var worlds = Directory.GetDirectories(WorldsFolder).Select(Path.GetFileName).ToList();
        var total = 0;
        foreach (var world in worlds)
        {
            foreach (var (file, row) in ReadWorld(world))
            {
                total++;
                await Assert.That(row.UnitId).IsNotEqualTo(0u).Because($"{world}/{file}");
                await Assert.That(row.Position).IsNotNull().Because($"{world}/{file} slave {row.UnitId}");
                await Assert.That(row.Position.X != 0f || row.Position.Y != 0f).IsTrue()
                    .Because($"{world}/{file} slave {row.UnitId} sits at the origin");
            }
        }

        await Assert.That(total).IsGreaterThan(0);
    }

    /// <summary>
    /// Rows each world spawns: its official land placements (boats are not shipped) plus, in main_world, the two
    /// hand-placed Cotton Wagons kept in slave_spawns.json (40-57 m from the official ones). Exact, so a lost or
    /// doubled row fails. The Howling Abyss rows are the older hand-written files, which sit on the official spots.
    /// </summary>
    private static readonly Dictionary<string, int> ExpectedRows = new()
    {
        ["main_world"] = 129,
        ["arche_mall_world"] = 4,
        ["instance_challenge_tower"] = 19,
        ["instance_golden_plains"] = 12,
        ["instance_howling_abyss"] = 6,
        ["instance_howling_abyss_2"] = 6,
        ["instance_noryette_battlefield"] = 19,
        ["instance_prologue"] = 4,
        ["instance_sea_of_chaos"] = 104,
        ["instance_sea_survival_2"] = 156,
        ["instance_the_last_day_of_hiramakand"] = 2,
        ["racing_test_world"] = 8
    };

    /// <summary>Tools/SlaveSpawns treats a row of the same slave within this distance as the same placement.</summary>
    private const float SamePlacementRadius = 0.5f;

    private static float Distance(PositionRow a, float x, float y) =>
        MathF.Sqrt(MathF.Pow(a.X - x, 2) + MathF.Pow(a.Y - y, 2));

    [Test]
    public async Task EveryWorld_SpawnsItsPlacements_Once()
    {
        var worlds = Directory.GetDirectories(WorldsFolder).Select(Path.GetFileName)
            .Where(world => ReadWorld(world).Count > 0).ToList();
        await Assert.That(worlds).IsEquivalentTo(ExpectedRows.Keys);

        foreach (var world in worlds)
        {
            var rows = ReadWorld(world);
            await Assert.That(rows.Count).IsEqualTo(ExpectedRows[world]).Because(world);

            // No placement twice: the generated files leave out whatever the hand-written file already spawns.
            for (var i = 0; i < rows.Count; i++)
            {
                for (var j = i + 1; j < rows.Count; j++)
                {
                    var (fileA, a) = rows[i];
                    var (fileB, b) = rows[j];
                    if (a.UnitId != b.UnitId)
                        continue;
                    await Assert.That(Distance(a.Position, b.Position.X, b.Position.Y)).IsGreaterThan(SamePlacementRadius)
                        .Because($"{world}: slave {a.UnitId} at ({a.Position.X}, {a.Position.Y}) is in {fileA} and {fileB}");
                }
            }
        }
    }

    [Test]
    public async Task MainWorld_HasTheBlueglassCannonsAtTheirOfficialSpots()
    {
        // Zone 162 (origin 7,7) slave.xml / world_server slave.g. They used to stand 3-5 m off, which the
        // generator's old 5 m "already spawned" radius hid.
        (float X, float Y)[] official = [(8852.3f, 8224.3f), (8858.4f, 8221.0f), (8855.4f, 8222.6f)];
        var cannons = ReadWorld("main_world").Where(r => r.Row.UnitId == 65).ToList();

        await Assert.That(cannons.Count).IsEqualTo(3);
        foreach (var (x, y) in official)
        {
            var atSpot = cannons.Where(c => Distance(c.Row.Position, x, y) < SamePlacementRadius).ToList();
            await Assert.That(atSpot.Count).IsEqualTo(1).Because($"Blueglass Human Cannon at ({x}, {y})");
            await Assert.That(MathF.Abs(atSpot[0].Row.Position.Yaw - -31f)).IsLessThan(1f);
        }
    }

    [Test]
    public async Task MainWorld_HasTheAndelphCannonsOfCannonsForQuestions()
    {
        var cannons = ReadWorld("main_world").Where(r => r.Row.UnitId == 609).ToList();

        await Assert.That(cannons.Count).IsEqualTo(4);
        foreach (var (_, row) in cannons)
        {
            // Giant's Canyon, Aubre Cradle (zone 150, origin 6,11): within 40 m of the quest's highlight doodads.
            await Assert.That(row.Position.X > 7700f && row.Position.X < 7760f).IsTrue();
            await Assert.That(row.Position.Y > 12250f && row.Position.Y < 12290f).IsTrue();
        }
    }
}
