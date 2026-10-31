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
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Teleport;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.UnitTests.Utils;

using GameTransform = AAEmu.Game.Models.Game.World.Transform.Transform;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>
/// The movement speed check against the moves the server makes itself: a teleport landing and a resurrection at a
/// return point are not movement and are not reported, while what the client walks is still checked as before.
/// The check that falls on such a move only remembers the new spot. Runs the real landing and the real packets on a
/// character in a test world, with a <see cref="SusManager"/> that keeps its reports instead of writing them.
/// </summary>
[NotInParallel]
public sealed class SusServerMoveTests
{
    private const uint WorldId = 8501;
    private const uint DepartureZone = 8502;
    private const uint LandingZone = 8503;
    private const uint PriestessTemplateId = 502;

    private static readonly Vector3 Departure = new(1000f, 1000f, 100f);
    // 3 km from the departure: 600 m/s if the jump were measured over the 5 s a check covers.
    private static readonly Vector3 Landing = new(4000f, 1000f, 100f);

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
    public async Task CrossZoneLanding_IsNotReported_AndTheConfirmedLandingIsWhereChecksStartAgain()
    {
        var character = NewCharacter(8510);
        // The check last saw the character at the departure, and the next one falls on the landing.
        _sus.AnalyzePlayerDeltaMovement(character, 5f);
        MakeCheckDue(character);

        var landed = SkillTeleportLanding.TryApplyToWorld(
            character, _world, LandingZone, Landing.X, Landing.Y, Landing.Z, 0f, TeleportReason.Portal);

        await Assert.That(landed).IsTrue();
        await Assert.That(character.Transform.World.Position).IsEqualTo(Landing);
        await Assert.That(character.DisabledSetPosition).IsTrue();
        await Assert.That(_sus.Reports).IsEmpty();

        // The client reports where it stands at the landing and the check that falls on it remembers that spot.
        MakeCheckDue(character);
        ConfirmLanding(character, OnTheGround(Landing));

        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(OnTheGround(Landing));
        await Assert.That(skipTime).IsEqualTo(0f);

        // Walking on from there is judged from the landing: 100 m in 5 s is 20 m/s.
        Walk(character, OnTheGround(Landing) + new Vector3(0f, 100f, 0f));

        await Assert.That(_sus.Reports).IsEmpty();
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out _, out skipTime)).IsTrue();
        await Assert.That(skipTime).IsEqualTo(-5f);
    }

    /// <summary>
    /// A landing while the position is already locked (a GM freeze, an earlier landing nobody has confirmed yet, such
    /// as a GM summon of a bot) cannot write the position, so the check that falls on it remembers the spot the
    /// character still holds. The client's confirmation then moves it to the landing.
    /// </summary>
    [Test]
    public async Task LandingUnderAHeldLock_TheConfirmedLandingIsNotReported()
    {
        var character = NewCharacter(8511);
        character.DisabledSetPosition = true;
        _sus.AnalyzePlayerDeltaMovement(character, 5f);
        MakeCheckDue(character);

        var landed = SkillTeleportLanding.TryApplyToWorld(
            character, _world, LandingZone, Landing.X, Landing.Y, Landing.Z, 0f, TeleportReason.Gm);

        await Assert.That(landed).IsTrue();
        MakeCheckDue(character);
        ConfirmLanding(character, OnTheGround(Landing));

        await Assert.That(_sus.Reports).IsEmpty();
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(OnTheGround(Landing));
        await Assert.That(skipTime).IsEqualTo(0f);
    }

    [Test]
    public async Task ReturnPointResurrection_IsNotReported_AndTheCheckRemembersTheReturnPoint()
    {
        var returnPoint = Landing;
        AddPriestess(returnPoint);
        var character = NewCharacter(8512, hp: 0);
        _sus.AnalyzePlayerDeltaMovement(character, 5f);
        MakeCheckDue(character);

        Resurrect(character);

        await Assert.That(character.IsDead).IsFalse();
        await Assert.That(_sus.Reports).IsEmpty();
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out var skipTime)).IsTrue();
        // The packet lands one or two metres off the priestess on each axis.
        await Assert.That(Vector3.Distance(position, returnPoint)).IsLessThan(3f);
        await Assert.That(position).IsEqualTo(character.Transform.World.Position);
        await Assert.That(skipTime).IsEqualTo(0f);
    }

    /// <summary>
    /// The resurrect request is the client's and nothing stops a living one from sending it. It still moves the
    /// character, but cannot clear the check: the jump is measured as before.
    /// </summary>
    [Test]
    public async Task ResurrectRequestFromTheLiving_StillHasItsJumpJudged()
    {
        AddPriestess(Landing);
        var character = NewCharacter(8513, hp: 100);
        _sus.AnalyzePlayerDeltaMovement(character, 5f);
        MakeCheckDue(character);

        Resurrect(character);

        var reports = _sus.Reports;
        await Assert.That(reports.Count).IsEqualTo(1);
        await Assert.That(reports[0]).StartsWith(
            $"{SusManager.CategoryCheating}|{character.Id}|Player {character.Name} seems to be moving a bit fast ");
    }

    [Test]
    public async Task AStepOverTheLimit_IsStillReportedAsBefore()
    {
        var character = NewCharacter(8514);
        _sus.AnalyzePlayerDeltaMovement(character, 5f);

        // 200 m in the 5 s a check covers is 40 m/s.
        Walk(character, Departure + new Vector3(200f, 0f, 0f));

        var reports = _sus.Reports;
        await Assert.That(reports.Count).IsEqualTo(1);
        await Assert.That(reports[0]).IsEqualTo(
            $"{SusManager.CategoryCheating}|{character.Id}|Player {character.Name} seems to be moving a bit fast 40.0 m/s (max 25.0)");
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(Departure + new Vector3(200f, 0f, 0f));
        // 15 s left alone after a report, less the 5 s of this check.
        await Assert.That(skipTime).IsEqualTo(SusMovementTracker.SkipTimeAfterFlag - 5f);
    }

    [Test]
    public async Task Locking_ForgetsTheCheckEveryTime_UnlockingNever()
    {
        var character = NewCharacter(8515);
        character.DisabledSetPosition = true;
        _sus.AnalyzePlayerDeltaMovement(character, 5f);

        // Locked again while locked (a landing under a GM freeze).
        character.DisabledSetPosition = true;
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out _, out _)).IsFalse();

        // Unlocking is what the client's teleport confirmation does: it never clears the check.
        _sus.AnalyzePlayerDeltaMovement(character, 5f);
        character.DisabledSetPosition = false;
        character.DisabledSetPosition = false;
        await Assert.That(_sus.LastPlayerPositions.TryGet(character.Id, out _, out _)).IsTrue();
    }

    [Test]
    public async Task LockingAMate_ForgetsItsCheckEveryTime()
    {
        var mount = new Mate { Id = 8516, ObjId = 8516, Name = "Mount" };
        mount.Transform.Local.SetPosition(Departure.X, Departure.Y, Departure.Z);
        mount.DisabledSetPosition = true;
        _sus.AnalyzeMountDeltaMovement(mount, 5f);

        mount.DisabledSetPosition = true;

        await Assert.That(_sus.LastPetPositions.TryGet(mount.Id, out _, out _)).IsFalse();
    }

    private TestCharacter NewCharacter(uint id, int hp = 100)
    {
        var character = new TestCharacter { Id = id, ObjId = id, Name = $"Lander{id}", Hp = hp };
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

    /// <summary>Inside an instance the resurrection goes to the copy's Temple Priestess.</summary>
    private void AddPriestess(Vector3 at)
    {
        var priestess = new Npc { ObjId = 8590, TemplateId = PriestessTemplateId, Name = "Temple Priestess" };
        priestess.Transform.Local.SetPosition(at.X, at.Y, at.Z);
        priestess.Transform.KeepZoneQuietly(LandingZone);
        _world.SetNpc(priestess.ObjId, priestess);
    }

    /// <summary>Where a client stands after a landing: the server's spot, settled half a metre onto the ground.</summary>
    private static Vector3 OnTheGround(Vector3 landing) => landing with { Z = landing.Z - 0.5f };

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

    /// <summary>The client's <c>CSTeleportEnded</c> for a landing at <paramref name="at"/>.</summary>
    private static void ConfirmLanding(Character character, Vector3 at)
    {
        var body = new PacketStream();
        body.Write(Helpers.ConvertLongX(at.X));
        body.Write(Helpers.ConvertLongY(at.Y));
        body.Write(at.Z);
        body.Write(new byte[16]);
        new CSTeleportEndedPacket { Connection = character.Connection }.Read(new PacketStream(body.GetBytes()));
    }

    /// <summary>The client's <c>CSResurrectCharacter</c> asking for the return point (not in place).</summary>
    private static void Resurrect(Character character) =>
        new CSResurrectCharacterPacket { Connection = character.Connection }.Read(new PacketStream([0]));

    /// <summary>A character whose maximum health and mana need no formulas (the resurrection restores a share of them).</summary>
    private sealed class TestCharacter() : Character(new UnitCustomModelParams())
    {
        public override int MaxHp => 1000;
        public override int MaxMp => 500;
    }

    /// <summary>Keeps what the movement checks report instead of writing it to the audit table.</summary>
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
