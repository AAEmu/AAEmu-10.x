using AAEmu.Commons.Network;
using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.C2G;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.StaticValues;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Core.Packets.C2G;

/// <summary>
/// Right-clicking a mount arrives as CSStartInteraction on the mate. A rider of that mate gets off the
/// seat they occupy; anyone else is still offered the ride skill.
/// </summary>
[NotInParallel]
public sealed class CSStartInteractionPacketMountTests : IDisposable
{
    private const uint OwnerObjId = 0x7101;
    private const uint GuestObjId = 0x7102;
    private const uint MountObjId = 0x7201;
    private const uint OtherMountObjId = 0x7202;

    private readonly IDisposable _worldScope;
    private readonly IDisposable _characterScope;
    private readonly WorldInstance _world;

    public CSStartInteractionPacketMountTests()
    {
        _worldScope = TestDungeonWorld.InstallWorldManager();
        // Leaving a seat changes the rider's transform parent, and that asks the character manager whether
        // the rider may read debug chat. With no account behind them the answer is no.
        _characterScope = new SingletonScope<CharacterManager>(new CharacterManager(
            Mock.Of<IWorldManager>().Object,
            Mock.Of<IAccountManager>().Object,
            Mock.Of<INameManager>().Object,
            Mock.Of<ICharacterIdManager>().Object,
            Mock.Of<IFactionManager>().Object,
            Mock.Of<ISkillManager>().Object,
            Mock.Of<IItemManager>().Object,
            Mock.Of<IHousingManager>().Object,
            Mock.Of<IFamilyManager>().Object,
            Mock.Of<IMailManager>().Object,
            Mock.Of<ITaskManager>().Object));
        _world = TestDungeonWorld.CreateWorld(9921, 0);
        _world.MateManager = new MateManager(_world);
        _world.SlaveManager = new SlaveManager(_world);
    }

    public void Dispose()
    {
        _world.Dispose();
        _characterScope.Dispose();
        _worldScope.Dispose();
    }

    [Test]
    public async Task RiderClickingTheirMount_LeavesTheSeatTheyOccupy()
    {
        var owner = AddPlayer(OwnerObjId);
        var mount = AddMount(MountObjId, owner.Character);
        Seat(mount, owner.Character, AttachPointKind.Driver);

        RightClick(owner, mount);

        await Assert.That(mount.Passengers[AttachPointKind.Driver]._objId).IsEqualTo(0u);
        await Assert.That(owner.Character.IsRiding).IsFalse();
        await Assert.That(owner.Character.AttachedPoint).IsEqualTo(AttachPointKind.None);
        await Assert.That(Opcodes(owner)).Contains(SCOffsets.SCUnitDetachedPacket);
        // Answering with the ride skill again is what kept the rider in the saddle.
        await Assert.That(Opcodes(owner)).DoesNotContain(SCOffsets.SCNpcInteractionSkillListPacket);
    }

    [Test]
    public async Task PassengerClickingTheMount_LeavesOnlyTheirOwnSeat()
    {
        var owner = AddPlayer(OwnerObjId);
        var guest = AddPlayer(GuestObjId);
        var mount = AddMount(MountObjId, owner.Character);
        Seat(mount, owner.Character, AttachPointKind.Driver);
        Seat(mount, guest.Character, AttachPointKind.Passenger0);

        RightClick(guest, mount);

        await Assert.That(mount.Passengers[AttachPointKind.Passenger0]._objId).IsEqualTo(0u);
        await Assert.That(guest.Character.IsRiding).IsFalse();
        await Assert.That(Opcodes(guest)).DoesNotContain(SCOffsets.SCNpcInteractionSkillListPacket);

        // The driver did not ask to get off.
        await Assert.That(mount.Passengers[AttachPointKind.Driver]._objId).IsEqualTo(OwnerObjId);
        await Assert.That(owner.Character.IsRiding).IsTrue();
        await Assert.That(owner.Character.AttachedPoint).IsEqualTo(AttachPointKind.Driver);
    }

    [Test]
    public async Task PlayerOnFoot_IsStillOfferedTheRideSkill()
    {
        var owner = AddPlayer(OwnerObjId);
        var guest = AddPlayer(GuestObjId);
        var mount = AddMount(MountObjId, owner.Character);
        Seat(mount, owner.Character, AttachPointKind.Driver);

        RightClick(guest, mount);

        await Assert.That(OfferedSkills(guest, mount)).IsEquivalentTo(new[] { SkillsEnum.SlaveMounting });
        await Assert.That(Opcodes(guest)).DoesNotContain(SCOffsets.SCUnitDetachedPacket);
        await Assert.That(mount.Passengers[AttachPointKind.Driver]._objId).IsEqualTo(OwnerObjId);
        await Assert.That(owner.Character.IsRiding).IsTrue();
    }

    [Test]
    public async Task RiderClickingAnotherMount_StaysSeatedAndIsOfferedTheRideSkill()
    {
        var owner = AddPlayer(OwnerObjId);
        var guest = AddPlayer(GuestObjId);
        var mount = AddMount(MountObjId, owner.Character);
        var otherMount = AddMount(OtherMountObjId, guest.Character);
        Seat(mount, owner.Character, AttachPointKind.Driver);

        RightClick(owner, otherMount);

        await Assert.That(OfferedSkills(owner, otherMount)).IsEquivalentTo(new[] { SkillsEnum.SlaveMounting });
        await Assert.That(Opcodes(owner)).DoesNotContain(SCOffsets.SCUnitDetachedPacket);
        await Assert.That(mount.Passengers[AttachPointKind.Driver]._objId).IsEqualTo(OwnerObjId);
        await Assert.That(owner.Character.IsRiding).IsTrue();
        await Assert.That(otherMount.Passengers[AttachPointKind.Driver]._objId).IsEqualTo(0u);
    }

    private sealed record Player(Character Character, GameConnection Connection, List<byte[]> Sent);

    private Player AddPlayer(uint objId)
    {
        var sent = new List<byte[]>();
        var session = Mock.Of<ISession>();
        session.SendPacket(Any<byte[]>()).Callback((byte[] bytes) => sent.Add(bytes));

        var connection = new GameConnection(session.Object);
        var character = new Character(new UnitCustomModelParams())
        {
            Id = objId,
            ObjId = objId,
            Name = $"Rider{objId:X}",
            Connection = connection,
        };
        connection.ActiveChar = character;
        TestDungeonWorld.Enter(_world, character);
        // The unmount looks its rider up server-wide, not in the instance.
        WorldManager.Instance.TryAddCharacter(character);
        return new Player(character, connection, sent);
    }

    private Mate AddMount(uint objId, Character owner)
    {
        var mount = new Mate
        {
            ObjId = objId,
            TlId = (ushort)objId,
            OwnerId = owner.Id,
            OwnerObjId = owner.ObjId,
            Name = $"Mount{objId:X}",
            Template = new NpcTemplate { Scale = 1f },
        };
        mount.InitializeSeatTopology([AttachPointKind.Driver, AttachPointKind.Passenger0]);
        mount.ParentWorld = _world;
        _world.AddObject(mount);
        _world.MateManager.SetActiveMate(owner.Id, mount);
        return mount;
    }

    /// <summary>Puts the character on the seat the way MountMate leaves them, without its broadcasts.</summary>
    private static void Seat(Mate mount, Character rider, AttachPointKind seat)
    {
        mount.Passengers[seat]._objId = rider.ObjId;
        rider.Transform.Parent = mount.Transform;
        rider.IsRiding = true;
        rider.AttachedPoint = seat;
    }

    private static void RightClick(Player player, Mate mount)
    {
        var body = new PacketStream()
            .WriteBc(mount.ObjId) // npcObjId
            .WriteBc(0)           // objId
            .Write(0)             // extraInfo
            .Write(-1)            // pickId
            .Write((byte)2)       // mouseButton
            .Write(0);            // modifierKeys

        player.Sent.Clear();
        new CSStartInteractionPacket { Connection = player.Connection }.Read(new PacketStream(body.GetBytes()));
    }

    private static List<ushort> Opcodes(Player player) =>
        player.Sent.Select(packet => SentPacket.Read(packet).Opcode).ToList();

    /// <summary>The skills of the one interaction list sent to the player for the mount.</summary>
    private static uint[] OfferedSkills(Player player, Mate mount)
    {
        var lists = player.Sent
            .Where(packet => SentPacket.Read(packet).Opcode == SCOffsets.SCNpcInteractionSkillListPacket)
            .ToList();
        if (lists.Count != 1)
            throw new InvalidOperationException($"Expected one interaction skill list, got {lists.Count}.");

        var stream = SentPacket.Body(lists[0]);
        if (stream.ReadBc() != mount.ObjId)
            throw new InvalidOperationException("The interaction skill list names another unit.");
        stream.ReadBc();    // objId
        stream.ReadInt32(); // extraInfo
        stream.ReadInt32(); // pickId
        stream.ReadByte();  // mouseButton
        var skills = new uint[stream.ReadInt32()];
        for (var i = 0; i < skills.Length; i++)
            skills[i] = stream.ReadUInt32();
        return skills;
    }
}
