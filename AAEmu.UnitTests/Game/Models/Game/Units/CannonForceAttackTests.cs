using System.Reflection;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World.Zones;
using AAEmu.Game.Models.StaticValues;
using AAEmu.UnitTests.Utils;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

/// <summary>
/// What a seated gunner can shoot through a world cannon (ownerless slave, template faction Friendly).
/// The cannon takes the gunner's side for hostile targets only. On foot the same character can also
/// attack a friendly player with force attack (Ctrl+F) on, or one who is already flagged, and pays for
/// it with the criminal state, assault evidence and murder evidence. None of the three follows a
/// vehicle (they stop at an attacker that is not a character), so the cannon must not open those two
/// doors.
/// </summary>
// CanAttack reads the zone, faction and team managers through their singletons.
[NotInParallel]
public class CannonForceAttackTests
{
    [Test]
    public async Task ForceAttack_ThroughACannon_DoesNotReachAFriendlyPlayer()
    {
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var ally = scene.AddPlayer(2136, scene.Nuian);
        var cannon = scene.AddCannon(984, gunner);
        gunner.ForceAttack = true;

        // On foot it is allowed, and a crime.
        await Assert.That(gunner.GetRelationStateTo(ally)).IsEqualTo(RelationState.Friendly);
        await Assert.That(gunner.CanAttack(ally)).IsTrue();

        await Assert.That(cannon.CanAttack(ally)).IsFalse();
    }

    [Test]
    public async Task FlaggedFriendlyPlayer_IsNotACannonTarget()
    {
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var flagged = scene.AddPlayer(2136, scene.Nuian);
        var cannon = scene.AddCannon(984, gunner);
        scene.Flag(flagged);

        await Assert.That(gunner.CanAttack(flagged)).IsTrue();

        await Assert.That(cannon.CanAttack(flagged)).IsFalse();
    }

    [Test]
    public async Task FriendlyPlayer_WithoutForceAttack_IsNoTargetEitherWay()
    {
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var ally = scene.AddPlayer(2136, scene.Nuian);
        var cannon = scene.AddCannon(984, gunner);

        await Assert.That(gunner.CanAttack(ally)).IsFalse();
        await Assert.That(cannon.CanAttack(ally)).IsFalse();
    }

    [Test]
    public async Task HostileNpc_IsACannonTarget_OnlyWhileSomeoneIsDriving()
    {
        // The bug this side-taking exists for: Friendly (1) is friendly to every faction, so an Andelph
        // cannon's shots found no target among the golems.
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var golem = new Npc { ObjId = 3407, Faction = scene.Monsters };
        var cannon = scene.AddCannon(984, gunner);
        var emptyCannon = scene.AddCannon(985, null);

        await Assert.That(cannon.CanAttack(golem)).IsTrue();
        await Assert.That(emptyCannon.CanAttack(golem)).IsFalse();
    }

    [Test]
    public async Task HostilePlayer_IsACannonTarget_ForceAttackOrNot()
    {
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var enemy = scene.AddPlayer(2136, scene.Harani);
        var cannon = scene.AddCannon(984, gunner);

        await Assert.That(gunner.GetRelationStateTo(enemy)).IsEqualTo(RelationState.Hostile);
        await Assert.That(cannon.CanAttack(enemy)).IsTrue();

        gunner.ForceAttack = true;
        await Assert.That(cannon.CanAttack(enemy)).IsTrue();
    }

    [Test]
    public async Task Cannon_NeverTargetsItselfOrNothing()
    {
        using var scene = new Scene();
        var gunner = scene.AddPlayer(2135, scene.Nuian);
        var cannon = scene.AddCannon(984, gunner);
        gunner.ForceAttack = true;

        await Assert.That(cannon.CanAttack(cannon)).IsFalse();
        await Assert.That(cannon.CanAttack(null)).IsFalse();
    }

    /// <summary>
    /// Two player factions at war with each other and a monster faction, with the managers
    /// <see cref="BaseUnit.CanAttack"/> asks for installed empty: no zones (so no protected home zone),
    /// no system factions, no teams.
    /// </summary>
    private sealed class Scene : IDisposable
    {
        private readonly List<IDisposable> _scopes = [];

        public SystemFaction Nuian { get; } = new() { Id = FactionsEnum.Nuian, Name = "Nuian" };
        public SystemFaction Harani { get; } = new() { Id = FactionsEnum.Harani, Name = "Harani" };
        public SystemFaction Monsters { get; } = new() { Id = FactionsEnum.Hostile, Name = "Hostile" };

        public Scene()
        {
            Nuian.Relations[FactionsEnum.Harani] = new FactionRelation
            {
                Id = FactionsEnum.Nuian, Id2 = FactionsEnum.Harani, State = RelationState.Hostile
            };

            var zones = new ZoneManager(Mock.Of<IWorldManager>().Object);
            typeof(ZoneManager)
                .GetField("_zones", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(zones, new Dictionary<uint, Zone>());
            _scopes.Add(new SingletonScope<ZoneManager>(zones));
            _scopes.Add(new SingletonScope<FactionManager>(
                new FactionManager(Mock.Of<ILocalizationManager>().Object)));
            _scopes.Add(new SingletonScope<TeamManager>(new TeamManager(
                Mock.Of<IWorldManager>().Object,
                Mock.Of<IChatManager>().Object,
                Mock.Of<ITeamIdManager>().Object,
                Mock.Of<ITickManager>().Object)));
        }

        public Character AddPlayer(uint objId, SystemFaction faction) =>
            new CharacterMock { Id = objId, ObjId = objId, Name = $"player{objId}", Faction = faction };

        /// <summary>A world-spawned cannon; <paramref name="gunner"/> null leaves the driver seat empty.</summary>
        public Slave AddCannon(uint objId, Character gunner)
        {
            var cannon = new Slave { ObjId = objId, Faction = new SystemFaction { Id = FactionsEnum.Friendly } };
            if (gunner != null)
                cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);
            return cannon;
        }

        /// <summary>
        /// Puts the criminal flag (Retribution) on a player, as attacking a friendly one does. The buff
        /// goes straight into the list <c>Buffs.CheckBuff</c> reads: <c>AddBuff</c> wants the buff
        /// modifier tables, which are game data.
        /// </summary>
        public void Flag(Character player)
        {
            var retribution = new BuffTemplate { Id = (uint)BuffConstants.Retribution };
            var buff = new Buff(
                player, player, new SkillCasterUnit(player.ObjId), retribution, null, DateTime.UtcNow);
            var effects = (List<Buff>)typeof(Buffs)
                .GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(player.Buffs)!;
            effects.Add(buff);
        }

        public void Dispose()
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
                _scopes[i].Dispose();
        }
    }
}
