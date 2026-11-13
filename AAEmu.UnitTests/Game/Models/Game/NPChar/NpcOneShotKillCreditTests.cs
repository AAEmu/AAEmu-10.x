using System.Collections.Concurrent;
using System.Reflection;
using AAEmu.Game;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Team;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.StaticValues;
using AAEmu.UnitTests.Utils;
using AAEmu.UnitTests.Utils.Mocks;
using Party = AAEmu.Game.Models.Game.Team.Team;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

/// <summary>
/// The one-shot kill through the real death path: <see cref="Unit.ReduceCurrentHp"/> takes a real
/// <see cref="Npc"/> from full health to zero and <see cref="Npc.DoDie"/> hands out the kill. Nothing here
/// tags the NPC by hand, so the kill tests fail when the killing-blow tag is taken off the damage path,
/// and the stand-in tests fail when it is put back into <c>DoDie</c>.
/// </summary>
/// <remarks>
/// Quest credit is read where the hunt objectives read it: <c>QuestActObjMonsterHunt</c> subscribes to
/// the character's <c>Events.OnMonsterHunt</c>, which <c>QuestManager.DoOnMonsterHuntEvents</c> raises once
/// per credited character. Loot rights are read where the loot reads them: the tagger and tag team the NPC
/// holds when its death event fires, just before <c>LootingContainer.GenerateLoot</c> runs.
/// </remarks>
// The death path reads the world, team and quest managers through their singletons.
[NotInParallel]
public class NpcOneShotKillCreditTests
{
    private const uint GolemTemplateId = 2301;
    private const int GolemMaxHp = 2900;

    [Test]
    public async Task OneShotCannonKill_CreditsTheGunnerAndTheNearbyPartyMember()
    {
        using var scene = new Scene();
        var gunner = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        var farAway = scene.AddCharacter(2137, 500f);
        var party = scene.FormParty(gunner, nearby, farAway);
        var cannon = scene.AddCannon(984, gunner);
        var golem = scene.AddGolem(3407);

        golem.ReduceCurrentHp(cannon, GolemMaxHp);

        await Assert.That(golem.Hp).IsEqualTo(0);
        await Assert.That(scene.HuntsOf(gunner)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(nearby)).IsEquivalentTo(new[] { GolemTemplateId });
        // Out of looting range (200 m): in the party, but not at the kill.
        await Assert.That(scene.HuntsOf(farAway)).IsEmpty();
        // The loot belongs to the gunner's party.
        await Assert.That(scene.TagAtDeath.Tagger).IsSameReferenceAs(gunner);
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(party.Id);
    }

    [Test]
    public async Task OneShotCannonKill_CreditsAGunnerWithoutAParty()
    {
        using var scene = new Scene();
        var gunner = scene.AddCharacter(2135, 0f);
        var bystander = scene.AddCharacter(2136, 50f);
        var cannon = scene.AddCannon(984, gunner);
        var golem = scene.AddGolem(3407);

        golem.ReduceCurrentHp(cannon, GolemMaxHp);

        await Assert.That(scene.HuntsOf(gunner)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(bystander)).IsEmpty();
        await Assert.That(scene.TagAtDeath.Tagger).IsSameReferenceAs(gunner);
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
    }

    [Test]
    public async Task OneShotKillByACharacter_SharesWithTheNearbyPartyMember()
    {
        // Untagged, the kill fell through to the killer alone and the party got nothing.
        using var scene = new Scene();
        var killer = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        var party = scene.FormParty(killer, nearby);
        var golem = scene.AddGolem(3407);

        golem.ReduceCurrentHp(killer, GolemMaxHp);

        await Assert.That(scene.HuntsOf(killer)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(nearby)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(party.Id);
    }

    [Test]
    public async Task KillingBlowOnATaggedNpc_LeavesTheKillWithItsTagger()
    {
        // Someone else already earned the tag with more than half the health bar; the cannon that lands
        // the last hit does not take the kill from them.
        using var scene = new Scene();
        var tagger = scene.AddCharacter(2135, 0f);
        var gunner = scene.AddCharacter(2136, 0f);
        var cannon = scene.AddCannon(984, gunner);
        var golem = scene.AddGolem(3407);
        golem.CharacterTagging.AddTagger(tagger, GolemMaxHp - 100);
        golem.Hp = 100;

        golem.ReduceCurrentHp(cannon, 100);

        await Assert.That(golem.Hp).IsEqualTo(0);
        await Assert.That(scene.HuntsOf(tagger)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(gunner)).IsEmpty();
        await Assert.That(scene.TagAtDeath.Tagger).IsSameReferenceAs(tagger);
    }

    [Test]
    public async Task KillingBlowOnAWoundedUntaggedNpc_IsTaggedForTheHealthItTook()
    {
        // Something that cannot tag (a guard) did most of the work. The last hit earns the tag, but it is
        // worth what it took, which is short of the half health bar a party needs to share the kill.
        using var scene = new Scene();
        var killer = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        scene.FormParty(killer, nearby);
        var golem = scene.AddGolem(3407);
        golem.Hp = 100;

        golem.ReduceCurrentHp(killer, 100);

        await Assert.That(scene.TagAtDeath.Tagger).IsSameReferenceAs(killer);
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
        await Assert.That(scene.HuntsOf(killer)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(nearby)).IsEmpty();
    }

    [Test]
    public async Task GmKill_GivesTheGmNoLootRights()
    {
        // /kill: ReduceCurrentHp in the GM's name with KillReason.Gm. The GM keeps the quest credit the
        // command always gave, but holds no tag, so no loot rights and nothing for the party.
        using var scene = new Scene();
        var gm = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        scene.FormParty(gm, nearby);
        var golem = scene.AddGolem(3407);

        golem.ReduceCurrentHp(gm, (golem.MaxHp + 1) * 10, KillReason.Gm);

        await Assert.That(golem.Hp).IsEqualTo(0);
        await Assert.That(scene.Deaths).IsEqualTo(1);
        await Assert.That(scene.TagAtDeath.Tagger).IsNull();
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
        await Assert.That(scene.HuntsOf(gm)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(nearby)).IsEmpty();
    }

    [Test]
    public async Task PetCapture_GivesTheCaptorNoLootRights()
    {
        // CapturePet: ReduceCurrentHp for the whole health bar with KillReason.Capture.
        using var scene = new Scene();
        var captor = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        scene.FormParty(captor, nearby);
        var golem = scene.AddGolem(3407);

        golem.ReduceCurrentHp(captor, golem.MaxHp, KillReason.Capture);

        await Assert.That(golem.Hp).IsEqualTo(0);
        await Assert.That(scene.Deaths).IsEqualTo(1);
        await Assert.That(scene.TagAtDeath.Tagger).IsNull();
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
        await Assert.That(scene.HuntsOf(nearby)).IsEmpty();
    }

    [Test]
    public async Task ZoneDeathMirror_GivesItsNearestPlayerStandInNoLootRights()
    {
        // The zone reports a death nobody on the World side is fighting: no abuser, no aggro, no
        // target. MirrorZoneNpcKilled then credits the nearest living character within 80 m, who
        // landed no blow, and calls DoDie in their name.
        using var scene = new Scene();
        var standIn = scene.AddCharacter(2135, 30f);
        var nearby = scene.AddCharacter(2136, 50f);
        scene.FormParty(standIn, nearby);
        var golem = scene.AddGolem(3407);

        var oldAuthority = WorldIntegration.ZoneAuthority;
        var oldKillQuota = WorldIntegration.OnWorldNpcKilled;
        WorldIntegration.ZoneAuthority = true;
        WorldIntegration.OnWorldNpcKilled = null;
        try
        {
            WorldIntegration.MirrorZoneNpcKilled(golem.ObjId);
        }
        finally
        {
            WorldIntegration.ZoneAuthority = oldAuthority;
            WorldIntegration.OnWorldNpcKilled = oldKillQuota;
        }

        await Assert.That(golem.Hp).IsEqualTo(0);
        await Assert.That(scene.Deaths).IsEqualTo(1);
        await Assert.That(scene.TagAtDeath.Tagger).IsNull();
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
        // The stand-in is the nearest one, and still gets the quest credit it got before.
        await Assert.That(scene.HuntsOf(standIn)).IsEquivalentTo(new[] { GolemTemplateId });
        await Assert.That(scene.HuntsOf(nearby)).IsEmpty();
    }

    [Test]
    public async Task DoDieInSomeonesName_DoesNotTagThem()
    {
        // Any other caller that reports a death with a killer of its choosing.
        using var scene = new Scene();
        var named = scene.AddCharacter(2135, 0f);
        var nearby = scene.AddCharacter(2136, 50f);
        scene.FormParty(named, nearby);
        var golem = scene.AddGolem(3407);
        golem.Hp = 0;

        golem.DoDie(named, KillReason.Damage);

        await Assert.That(scene.TagAtDeath.Tagger).IsNull();
        await Assert.That(scene.TagAtDeath.Team).IsEqualTo(0u);
        await Assert.That(scene.HuntsOf(nearby)).IsEmpty();
    }

    /// <summary>
    /// One world with the managers the death path asks for, all empty: no formulas, loot, zones or quest
    /// rows. Everything is put back on dispose.
    /// </summary>
    private sealed class Scene : IDisposable
    {
        private const uint WorldId = 1;
        private const float KillX = 1000f;
        private const float KillY = 1000f;

        private readonly List<IDisposable> _scopes = [];
        private readonly WorldInstance _world;
        private readonly TeamManager _teams;
        private readonly Dictionary<Character, List<uint>> _hunts = [];
        private uint _nextTeamId = 1;

        public Scene()
        {
            _scopes.Add(TestDungeonWorld.InstallWorldManager());
            _world = TestDungeonWorld.CreateWorld(WorldId, 0);
            _world.MateManager = new MateManager(_world);

            _teams = new TeamManager(
                Mock.Of<IWorldManager>().Object,
                Mock.Of<IChatManager>().Object,
                Mock.Of<ITeamIdManager>().Object,
                Mock.Of<ITickManager>().Object);
            _scopes.Add(new SingletonScope<TeamManager>(_teams));
            _scopes.Add(new SingletonScope<QuestManager>(new QuestManager(
                Mock.Of<ITaskManager>().Object,
                Mock.Of<IZoneManager>().Object)));
            // Character.InParty tells the friend list; distances between units ask for model radii.
            _scopes.Add(new SingletonScope<FriendMananger>(new FriendMananger()));
            _scopes.Add(new SingletonScope<ModelManager>(new ModelManager()));
        }

        /// <summary>How many times the golem's death event fired.</summary>
        public int Deaths { get; private set; }

        /// <summary>The tag rights the golem held when it died, which is what its loot is handed out by.</summary>
        public (Character Tagger, uint Team) TagAtDeath { get; private set; }

        /// <summary>A living character standing <paramref name="metresFromKill"/> from where the golem dies.</summary>
        public Character AddCharacter(uint objId, float metresFromKill)
        {
            var character = new CharacterMock
            {
                Id = objId, ObjId = objId, Name = $"player{objId}", Level = 30, Hp = 1000
            };
            TestDungeonWorld.Enter(_world, character);
            WorldManager.Instance.TryAddCharacter(character);
            character.Transform.Local.SetPosition(KillX + metresFromKill, KillY, 0f);

            var hunts = new List<uint>();
            character.Events.OnMonsterHunt += (_, args) => hunts.Add(args.NpcId);
            _hunts[character] = hunts;
            return character;
        }

        /// <summary>The NPC template ids this character got hunt credit for, in order.</summary>
        public IReadOnlyList<uint> HuntsOf(Character character) => _hunts[character];

        /// <summary>
        /// A world-spawned cannon (no summoner, no owner, template faction Friendly) with a gunner in the
        /// driver seat.
        /// </summary>
        public Slave AddCannon(uint objId, Character gunner)
        {
            var cannon = new SilentSlave
            {
                ObjId = objId, Faction = new SystemFaction { Id = FactionsEnum.Friendly }
            };
            TestDungeonWorld.Attach(cannon, _world);
            cannon.Transform.Local.SetPosition(KillX, KillY, 0f);
            cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);
            return cannon;
        }

        /// <summary>A golem at full health that nobody has hit yet.</summary>
        public Npc AddGolem(uint objId)
        {
            var golem = new Golem
            {
                ObjId = objId,
                TemplateId = GolemTemplateId,
                Level = 30,
                // No kill XP: the amount comes from unit_formulas, which is game data.
                Template = new NpcTemplate { Id = GolemTemplateId, NoExp = true }
            };
            TestDungeonWorld.Attach(golem, _world);
            _world.AddObject(golem);
            golem.Transform.Local.SetPosition(KillX, KillY, 0f);
            golem.Hp = golem.MaxHp;
            golem.Events.OnDeath += (_, _) =>
            {
                Deaths++;
                TagAtDeath = (golem.CharacterTagging.Tagger, golem.CharacterTagging.TagTeam);
            };
            return golem;
        }

        public Party FormParty(params Character[] members)
        {
            var team = new Party { Id = _nextTeamId++, OwnerId = members[0].Id, IsParty = true };
            for (var i = 0; i < members.Length; i++)
            {
                team.Members[i] = new TeamMember(members[i]);
                members[i].InParty = true;
            }

            var activeTeams = (ConcurrentDictionary<uint, Party>)typeof(TeamManager)
                .GetField("_activeTeams", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_teams)!;
            activeTeams[team.Id] = team;
            return team;
        }

        public void Dispose()
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
                _scopes[i].Dispose();
            _world.Dispose();
        }
    }

    /// <summary>A real NPC with its health pinned (the formula is game data) and no one to broadcast to.</summary>
    private sealed class Golem : Npc
    {
        public override int MaxHp => GolemMaxHp;
        public override void BroadcastPacket(GamePacket packet, bool self) { }
    }

    private sealed class SilentSlave : Slave
    {
        public override void BroadcastPacket(GamePacket packet, bool self) { }
    }
}
