using System.Numerics;
using System.Reflection;

using AAEmu.Commons.Network;
using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.C2G;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Core.Packets.C2G;

/// <summary>
/// The client's resurrect request, run on a character in a test copy (where the return point is the copy's Temple
/// Priestess). The revive's costs follow the offer that is really used, not the client's in-place flag, and a request
/// from the living is refused and written down. The debuffs themselves are covered by
/// <c>CharacterResurrectionCostsTests</c>; here the empty skill manager has no buff to give.
/// </summary>
[NotInParallel]
public sealed class CSResurrectCharacterPacketTests
{
    private const uint WorldId = 8601;
    private const uint FallenZone = 8602;
    private const uint TempleZone = 8603;
    private const uint PriestessTemplateId = 502;

    private static readonly Vector3 Fallen = new(1000f, 1000f, 100f);
    private static readonly Vector3 Temple = new(4000f, 1000f, 100f);

    private IDisposable _worldScope;
    private SingletonScope<SkillManager> _skillScope;
    private SingletonScope<SusManager> _susScope;
    private SingletonScope<JusticeManager> _justiceScope;
    private RecordingSusManager _sus;
    private WorldInstance _world;

    [Before(Test)]
    public void Setup()
    {
        _worldScope = TestDungeonWorld.InstallWorldManager();
        _skillScope = new SingletonScope<SkillManager>(TestManagers.CreateSkillManager());
        _sus = new RecordingSusManager();
        _susScope = new SingletonScope<SusManager>(_sus);
        _justiceScope = new SingletonScope<JusticeManager>(new JusticeManager());
        _world = TestDungeonWorld.CreateWorld(WorldId, 0, FallenZone, TempleZone);
        AddPriestess(Temple);
    }

    [After(Test)]
    public void Teardown()
    {
        _world?.Dispose();
        _justiceScope?.Dispose();
        _susScope?.Dispose();
        _skillScope?.Dispose();
        _worldScope?.Dispose();
    }

    [Test]
    public async Task AnAcceptedOffer_ComesBackWhereItFell_WithTheOffersShare()
    {
        var character = NewCharacter(8610, hp: 0);
        Offer(character, hpPercent: 30, mpPercent: 20);

        Resurrect(character, inPlace: true);

        await Assert.That(character.IsDead).IsFalse();
        await Assert.That(Vector3.Distance(character.Transform.World.Position, Fallen)).IsLessThan(0.01f);
        await Assert.That((character.Hp, character.Mp)).IsEqualTo((300, 100));
        await Assert.That(character.ResurrectOfferPending).IsFalse();
        await Assert.That((character.ResurrectHpPercent, character.ResurrectMpPercent)).IsEqualTo((1u, 1u));
        await Assert.That(_sus.Reports).IsEmpty();
    }

    /// <summary>A modified client's in-place flag without an offer: the return point, at a temple revive's cost.</summary>
    [Test]
    public async Task InPlaceWithoutAnOffer_IsATempleRevive()
    {
        var character = NewCharacter(8611, hp: 0);

        Resurrect(character, inPlace: true);

        await Assert.That(character.IsDead).IsFalse();
        await Assert.That(Vector3.Distance(character.Transform.World.Position, Temple)).IsLessThan(3f);
        await Assert.That((character.Hp, character.Mp)).IsEqualTo((100, 50));
    }

    /// <summary>
    /// The in-place flag after an offer that was cleared (a new death drops the offer) costs a temple revive, not the
    /// share the old offer carried.
    /// </summary>
    [Test]
    public async Task InPlaceAfterTheOfferWasDropped_DoesNotGetTheOldShare()
    {
        var character = NewCharacter(8612, hp: 0);
        Offer(character, hpPercent: 60, mpPercent: 60);
        character.ResurrectOfferPending = false;

        Resurrect(character, inPlace: true);

        await Assert.That(Vector3.Distance(character.Transform.World.Position, Temple)).IsLessThan(3f);
        await Assert.That((character.Hp, character.Mp)).IsEqualTo((100, 50));
    }

    /// <summary>An honest client that declined the offer and chose the temple: as before.</summary>
    [Test]
    public async Task TheTempleWithAnOfferWaiting_IsATempleRevive_AndSpendsTheOffer()
    {
        var character = NewCharacter(8613, hp: 0);
        Offer(character, hpPercent: 30, mpPercent: 20);

        Resurrect(character, inPlace: false);

        await Assert.That(Vector3.Distance(character.Transform.World.Position, Temple)).IsLessThan(3f);
        await Assert.That((character.Hp, character.Mp)).IsEqualTo((100, 50));
        await Assert.That(character.ResurrectOfferPending).IsFalse();
        await Assert.That((character.ResurrectHpPercent, character.ResurrectMpPercent)).IsEqualTo((1u, 1u));
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task TheDeathFlags_AreClearedEitherWay(bool acceptOffer)
    {
        var character = NewCharacter(8614, hp: 0);
        character.DiedInPvp = true;
        character.DiedInPvpWarZone = true;
        if (acceptOffer)
            Offer(character, hpPercent: 30, mpPercent: 20);

        Resurrect(character, inPlace: acceptOffer);

        await Assert.That(character.DiedInPvp).IsFalse();
        await Assert.That(character.DiedInPvpWarZone).IsFalse();
    }

    /// <summary>
    /// The request from the living is a modified client's jump to its return point: refused, nothing about the
    /// character changes (not even a courthouse summons or an offer it may hold), and it is written down.
    /// </summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FromTheLiving_IsRefusedAndWrittenDown(bool inPlace)
    {
        var character = NewCharacter(8615, hp: 640);
        character.ResurrectOfferPending = true;
        character.ResurrectHpPercent = 30;
        JusticeManager.Instance.OnWantedDeath(character);
        _sus.AnalyzePlayerDeltaMovement(character, 5f);

        Resurrect(character, inPlace);

        await Assert.That(character.Transform.World.Position).IsEqualTo(Fallen);
        await Assert.That(character.Hp).IsEqualTo(640);
        await Assert.That(character.ResurrectOfferPending).IsTrue();
        await Assert.That(character.ResurrectHpPercent).IsEqualTo(30u);
        await Assert.That(Defendants().Contains(character.Id)).IsTrue();
        // The speed check is left as it was: the request cannot clear it.
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out _)).IsTrue();
        await Assert.That(position).IsEqualTo(Fallen);

        var reports = _sus.Reports;
        await Assert.That(reports.Count).IsEqualTo(1);
        await Assert.That(reports[0]).IsEqualTo(
            $"{SusManager.CategoryCheating}|{character.Id}|Player {character.Name} asked to be resurrected while alive (hp 640/1000, inPlace={inPlace}), refused");
    }

    private TestCharacter NewCharacter(uint id, int hp)
    {
        var character = new TestCharacter { Id = id, ObjId = id, Name = $"Fallen{id}", Hp = hp, Mp = 0 };
        character.ParentWorld = _world;
        character.Transform.Local.SetPosition(Fallen.X, Fallen.Y, Fallen.Z);
        character.Transform.KeepZoneQuietly(FallenZone);
        var region = new Region(_world, 1, 1, FallenZone);
        region.AddObject(character);
        character.Region = region;
        TestDungeonWorld.Enter(_world, character);
        character.Connection = new GameConnection(Mock.Of<ISession>().Object) { ActiveChar = character };
        return character;
    }

    private void AddPriestess(Vector3 at)
    {
        var priestess = new Npc { ObjId = 8690, TemplateId = PriestessTemplateId, Name = "Temple Priestess" };
        priestess.Transform.Local.SetPosition(at.X, at.Y, at.Z);
        priestess.Transform.KeepZoneQuietly(TempleZone);
        _world.SetNpc(priestess.ObjId, priestess);
    }

    /// <summary>Another player's resurrection, as its special effect offers it.</summary>
    private static void Offer(Character fallen, int hpPercent, int mpPercent)
    {
        var healer = new Character(new UnitCustomModelParams()) { Id = 8699, ObjId = 8699, Name = "Healer", Hp = 900 };
        new Resurrection().Execute(healer, new SkillCasterUnit(healer.ObjId), fallen, null, null, null, null,
            DateTime.UtcNow, 0, hpPercent, mpPercent, 0);
    }

    private static void Resurrect(Character character, bool inPlace) =>
        new CSResurrectCharacterPacket { Connection = character.Connection }
            .Read(new PacketStream([inPlace ? (byte)1 : (byte)0]));

    private static HashSet<uint> Defendants() =>
        (HashSet<uint>)typeof(JusticeManager)
            .GetField("_defendants", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(JusticeManager.Instance)!;

    /// <summary>A character whose maximum health and mana need no formulas (the resurrection restores a share of them).</summary>
    private sealed class TestCharacter() : Character(new UnitCustomModelParams())
    {
        public override int MaxHp => 1000;
        public override int MaxMp => 500;
    }

    /// <summary>Keeps what is reported instead of writing it to the audit table.</summary>
    private sealed class RecordingSusManager() : SusManager(Mock.Of<IWorldManager>().Object)
    {
        private readonly List<string> _reports = [];

        public List<string> Reports
        {
            get
            {
                lock (_reports)
                    return [.. _reports];
            }
        }

        public override bool LogActivity(string category, Character player, string description)
        {
            lock (_reports)
                _reports.Add($"{category}|{player?.Id}|{description}");
            return true;
        }
    }
}
