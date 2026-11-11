using System.Numerics;
using AAEmu.Game.Models.Game.World.Interactions;

namespace AAEmu.UnitTests.Game.Models.Game.World;

/// <summary>
/// SummonDoodad places the doodad at its summoner. Invisible spawner NPCs hover a little above uneven ground
/// (Island of Abundance crops floated), so a summon slightly above the terrain is put down on it.
/// </summary>
public class SummonedDoodadPlacementTests
{
    [Test]
    public async Task SlightlyAboveTerrain_SnapsDown()
    {
        await Assert.That(SummonedDoodadPlacement.GroundZ(summonZ: 101.4f, terrainZ: 100.2f)).IsEqualTo(100.2f);
    }

    [Test]
    public async Task FarAboveTerrain_KeepsItsHeight()
    {
        // A deck, a roof or a bridge: not the ground the doodad belongs on.
        await Assert.That(SummonedDoodadPlacement.GroundZ(summonZ: 120f, terrainZ: 100f)).IsEqualTo(120f);
    }

    [Test]
    public async Task BelowTerrain_IsNeverRaised()
    {
        await Assert.That(SummonedDoodadPlacement.GroundZ(summonZ: 99.5f, terrainZ: 100f)).IsEqualTo(99.5f);
    }

    [Test]
    public async Task NoTerrainData_KeepsItsHeight()
    {
        await Assert.That(SummonedDoodadPlacement.GroundZ(summonZ: 101.4f, terrainZ: 0f)).IsEqualTo(101.4f);
    }

    [Test]
    public async Task SummonerStillAtItsZoneSpawn_DropsTheZoneSpawnLift()
    {
        // Island farming spawner: zone spawn Z 101.6 over ground 101.1 (npc_spawners.g points).
        var spawn = new Vector3(10f, 20f, 101.6f);
        await Assert.That(SummonedDoodadPlacement.UnliftedZ(spawn, spawn)).IsEqualTo(101.1f);
    }

    [Test]
    public async Task SummonerBobbingAboveItsZoneSpawn_StillDropsToTheSpawnGround()
    {
        // Fly-model spawners drift up 0.3-0.6 m after the zone places them.
        var spawn = new Vector3(10f, 20f, 106.04f);
        await Assert.That(SummonedDoodadPlacement.UnliftedZ(new Vector3(10f, 20f, 106.66f), spawn))
            .IsEqualTo(105.54f).Within(0.001f);
    }

    [Test]
    public async Task SummonerMovedAwayFromItsSpawn_KeepsItsHeight()
    {
        var spawn = new Vector3(10f, 20f, 101.6f);
        await Assert.That(SummonedDoodadPlacement.UnliftedZ(new Vector3(15f, 20f, 101.6f), spawn)).IsEqualTo(101.6f);
    }

    [Test]
    public async Task SummonerWithoutZoneSpawn_KeepsItsHeight()
    {
        await Assert.That(SummonedDoodadPlacement.UnliftedZ(new Vector3(10f, 20f, 101.6f), null)).IsEqualTo(101.6f);
    }
}
