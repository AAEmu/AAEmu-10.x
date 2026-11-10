using System.Numerics;
using System.Reflection;

using AAEmu.Commons.Network;
using AAEmu.Commons.Network.Core;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.C2G;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.InstantGame;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Teleport;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.UnitTests.Utils;

using GameTransform = AAEmu.Game.Models.Game.World.Transform.Transform;

namespace AAEmu.UnitTests.Game.Core.Packets.C2G;

/// <summary>
/// The client's teleport confirmation against the landing the server sent it to (<c>SkillTeleportLanding</c>): an
/// honest client a few metres off is taken as it reports, one far off is put at the sent spot and written down, and
/// without a landing on record the confirmation is taken as it comes, as before. Runs the real landing and packet on
/// a character in a test world.
/// </summary>
[NotInParallel]
public sealed class CSTeleportEndedPacketTests
{
    private const uint WorldId = 8701;
    private const uint DepartureZone = 8702;
    private const uint LandingZone = 8703;

    private static readonly Vector3 Departure = new(1000f, 1000f, 100f);
    private static readonly Vector3 Landing = new(4000f, 1000f, 100f);
    private static readonly Vector3 SecondLanding = new(4000f, 3000f, 120f);

    private static readonly FieldInfo SkipCheckTime = typeof(GameTransform)
        .GetField("_skipCheckTime", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private IDisposable _worldScope;
    private SingletonScope<SkillManager> _skillScope;
    private SingletonScope<SusManager> _susScope;
    private RecordingSusManager _sus;
    private WorldInstance _world;

    [Before(Test)]
    public void Setup()
    {
        _worldScope = TestDungeonWorld.InstallWorldManager();
        _skillScope = new SingletonScope<SkillManager>(TestManagers.CreateSkillManager());
        _sus = new RecordingSusManager();
        _susScope = new SingletonScope<SusManager>(_sus);
        _world = TestDungeonWorld.CreateWorld(WorldId, 0, DepartureZone, LandingZone);
        // A landing dismounts the character first.
        _world.MateManager = new MateManager(_world);
        _world.SlaveManager = new SlaveManager(_world);
    }

    [After(Test)]
    public void Teardown()
    {
        _world?.Dispose();
        _susScope?.Dispose();
        _skillScope?.Dispose();
        _worldScope?.Dispose();
    }

    [Test]
    public async Task TheLanding_IsRememberedWithItsWorldAndInstance()
    {
        var character = NewCharacter(8710);

        Land(character, Landing);

        await Assert.That(character.TeleportLandings.Count).IsEqualTo(1);
        await Assert.That(character.TeleportLandings.Take(character.Transform.WorldId, WorldId, Landing, 20f, out var sent))
            .IsEqualTo(TeleportEndedCheck.WithinTolerance);
        await Assert.That(sent).IsEqualTo(Landing);
    }

    /// <summary>An honest client settles a few metres from the spot it was sent to, and stands where it says.</summary>
    [Test]
    public async Task AnHonestConfirmationAFewMetresOff_IsAppliedAsReported()
    {
        var character = NewCharacter(8711);
        Land(character, Landing);
        var reported = Landing + new Vector3(3f, -2f, -0.5f);

        MakeCheckDue(character);
        ConfirmLanding(character, reported);

        await Assert.That(character.DisabledSetPosition).IsFalse();
        await Assert.That(character.Transform.World.Position).IsEqualTo(reported);
        await Assert.That(character.TeleportLandings.Count).IsEqualTo(0);
        await Assert.That(_sus.Reports).IsEmpty();
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out _)).IsTrue();
        await Assert.That(position).IsEqualTo(reported);
    }

    /// <summary>
    /// A confirmation far from the landing puts the character at the spot it was sent to, writes a Cheat line, and the
    /// check that falls on it remembers the sent spot, so what the client does from its own spot is judged from there.
    /// </summary>
    [Test]
    public async Task AFarConfirmation_IsPlacedAtTheSentSpot_AndWrittenDown()
    {
        var character = NewCharacter(8712);
        Land(character, Landing);
        var reported = Landing + new Vector3(300f, 0f, 0f);

        MakeCheckDue(character);
        ConfirmLanding(character, reported);

        await Assert.That(character.DisabledSetPosition).IsFalse();
        await Assert.That(character.Transform.World.Position).IsEqualTo(Landing);
        var reports = _sus.Reports;
        await Assert.That(reports.Count).IsEqualTo(1);
        await Assert.That(reports[0]).IsEqualTo(
            $"{SusManager.CategoryCheating}|{character.Id}|Player {character.Name} confirmed a teleport landing at ({reported.X:F1}, {reported.Y:F1}, {reported.Z:F1}), {300f:F1} m from the spot it was sent to ({Landing.X:F1}, {Landing.Y:F1}, {Landing.Z:F1}) (max {20f:F1}), placed at the sent spot");
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(Landing);
        await Assert.That(skipTime).IsEqualTo(0f);
    }

    /// <summary>
    /// The client of a far confirmation goes on from the spot it claimed: that move is judged from the sent spot
    /// (300 m in the 5 s a check covers is 60 m/s), not only remembered as the first spot after the landing.
    /// </summary>
    [Test]
    public async Task AfterAFarConfirmation_TheClientsNextMoveIsJudgedFromTheSentSpot()
    {
        var character = NewCharacter(8718);
        Land(character, Landing);
        var reported = Landing + new Vector3(300f, 0f, 0f);
        ConfirmLanding(character, reported);

        Walk(character, reported);

        var reports = _sus.Reports;
        await Assert.That(reports.Count).IsEqualTo(2);
        await Assert.That(reports[1]).IsEqualTo(
            $"{SusManager.CategoryCheating}|{character.Id}|Player {character.Name} seems to be moving a bit fast {60f:F1} m/s (max {SusManager.PlayerCheckSpeed:F1})");
    }

    /// <summary>An honest confirmation changes nothing about the check: the first spot after the landing is only remembered.</summary>
    [Test]
    public async Task AfterAnHonestConfirmation_TheCheckGoesOnAsBefore()
    {
        var character = NewCharacter(8719);
        Land(character, Landing);
        ConfirmLanding(character, Landing + new Vector3(3f, 0f, -0.5f));

        Walk(character, Landing + new Vector3(50f, 0f, -0.5f));

        await Assert.That(_sus.Reports).IsEmpty();
    }

    /// <summary>
    /// A move the server makes some other way (a GM command locks the position and sends the client itself) has no
    /// landing on record: its confirmation is taken as it comes, as before.
    /// </summary>
    [Test]
    public async Task WithoutALandingOnRecord_TheConfirmationIsTakenAsItComes()
    {
        var character = NewCharacter(8713);
        character.DisabledSetPosition = true;
        var reported = Landing + new Vector3(0f, 2500f, 0f);

        ConfirmLanding(character, reported);

        await Assert.That(character.DisabledSetPosition).IsFalse();
        await Assert.That(character.Transform.World.Position).IsEqualTo(reported);
        await Assert.That(_sus.Reports).IsEmpty();
    }

    /// <summary>
    /// A second landing before the client confirmed the first: its confirmation of the first is honest, and the
    /// unlock it brings gives up the second, whose confirmation is then taken as it comes - as before the check.
    /// </summary>
    [Test]
    public async Task RepeatedLandingsBeforeAConfirmation_AreNotReported()
    {
        var character = NewCharacter(8714);
        Land(character, Landing);
        Land(character, SecondLanding);

        ConfirmLanding(character, Landing + new Vector3(1f, 1f, -0.5f));
        var afterFirst = character.Transform.World.Position;
        ConfirmLanding(character, SecondLanding + new Vector3(-1f, 0f, -0.5f));

        await Assert.That(afterFirst).IsEqualTo(Landing + new Vector3(1f, 1f, -0.5f));
        await Assert.That(character.Transform.World.Position).IsEqualTo(SecondLanding + new Vector3(-1f, 0f, -0.5f));
        await Assert.That(_sus.Reports).IsEmpty();
    }

    [Test]
    public async Task RepeatedLandings_AConfirmationFarFromBoth_GoesToTheLatest()
    {
        var character = NewCharacter(8715);
        Land(character, Landing);
        Land(character, SecondLanding);

        ConfirmLanding(character, Departure);

        await Assert.That(character.Transform.World.Position).IsEqualTo(SecondLanding);
        await Assert.That(_sus.Reports.Count).IsEqualTo(1);
    }

    /// <summary>
    /// An instance load finishes with the client's load confirmation, which releases the lock: the landing is given up
    /// there, and a teleport confirmation after it is taken as it comes.
    /// </summary>
    [Test]
    public async Task AnInstanceLoadsArrival_GivesTheLandingUp()
    {
        var character = NewCharacter(8716);
        Land(character, Landing);

        InstanceArrival.Complete(character, snapClient: false);
        var reported = Landing + new Vector3(0f, 900f, 0f);
        ConfirmLanding(character, reported);

        await Assert.That(character.Transform.World.Position).IsEqualTo(reported);
        await Assert.That(_sus.Reports).IsEmpty();
    }

    /// <summary>The (0, 0, 0) a same-world load can answer with is ignored as before, and ends the wait too.</summary>
    [Test]
    public async Task AnOriginConfirmation_IsIgnored_AndGivesTheLandingUp()
    {
        var character = NewCharacter(8717);
        Land(character, Landing);

        ConfirmLanding(character, Vector3.Zero);

        await Assert.That(character.DisabledSetPosition).IsFalse();
        await Assert.That(character.Transform.World.Position).IsEqualTo(Landing);
        await Assert.That(character.TeleportLandings.Count).IsEqualTo(0);
        await Assert.That(_sus.Reports).IsEmpty();
    }

    private TestCharacter NewCharacter(uint id)
    {
        var character = new TestCharacter { Id = id, ObjId = id, Name = $"Lander{id}", Hp = 100 };
        character.ParentWorld = _world;
        character.Transform.Local.SetPosition(Departure.X, Departure.Y, Departure.Z);
        character.Transform.KeepZoneQuietly(DepartureZone);
        var region = new Region(_world, 1, 1, DepartureZone);
        region.AddObject(character);
        character.Region = region;
        TestDungeonWorld.Enter(_world, character);
        character.Connection = new GameConnection(Mock.Of<ISession>().Object) { ActiveChar = character };
        return character;
    }

    /// <summary>A portal's landing in the character's own world, which locks the position until the client confirms it.</summary>
    private void Land(Character character, Vector3 at) =>
        SkillTeleportLanding.TryApplyToWorld(character, _world, LandingZone, at.X, at.Y, at.Z, 0f, TeleportReason.Portal);

    /// <summary>The next <c>FinalizeTransform</c> runs the speed check, as five seconds of movement since the last would.</summary>
    private static void MakeCheckDue(Character character) =>
        SkipCheckTime.SetValue(character.Transform, 0f);

    /// <summary>A client move to <paramref name="to"/> on which the 5 s check falls, as <c>CSMoveUnitPacket</c> applies it.</summary>
    private static void Walk(Character character, Vector3 to)
    {
        MakeCheckDue(character);
        character.Transform.Local.SetPosition(to.X, to.Y, to.Z);
        character.Transform.FinalizeTransform();
    }

    /// <summary>The client's <c>CSTeleportEnded</c>, reporting <paramref name="at"/>.</summary>
    private static void ConfirmLanding(Character character, Vector3 at)
    {
        var body = new PacketStream();
        body.Write(Helpers.ConvertLongX(at.X));
        body.Write(Helpers.ConvertLongY(at.Y));
        body.Write(at.Z);
        body.Write(new byte[16]);
        new CSTeleportEndedPacket { Connection = character.Connection }.Read(new PacketStream(body.GetBytes()));
    }

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
