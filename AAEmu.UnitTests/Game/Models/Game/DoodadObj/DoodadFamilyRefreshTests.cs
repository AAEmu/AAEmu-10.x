using System.Net;
using System.Net.Sockets;
using AAEmu.Commons.Network;
using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

/// <summary>
/// The client reads a doodad's family from the create packet only, so after a family membership change
/// the owner's family-gated doodads are removed from their viewers' clients and created again.
/// </summary>
public sealed class DoodadFamilyRefreshTests
{
    private const uint OwnerId = 1006;
    private const uint FamilyGroupId = 5;
    private const uint PublicGroupId = 6;

    private sealed class RecordingSession : ISession
    {
        public List<byte[]> Packets { get; } = [];
        public IPAddress Ip => IPAddress.Loopback;
        public uint SessionId => 1;
        public Socket Socket => null!;

        public void SendPacket(byte[] packet) => Packets.Add(packet);
        public void AddAttribute(string name, object attribute) { }
        public object GetAttribute(string name) => null;
        public void ClearAttribute(string name) { }
        public void Close() { }
    }

    private static Mock<IDoodadManager> Funcs()
    {
        var doodads = Mock.Of<IDoodadManager>();
        doodads.GetFuncsForGroup(FamilyGroupId).Returns(
        [
            new DoodadFunc { GroupId = FamilyGroupId, PermId = (uint)DoodadFuncPermission.Owner },
            new DoodadFunc { GroupId = FamilyGroupId, PermId = (uint)DoodadFuncPermission.Family }
        ]);
        doodads.GetFuncsForGroup(PublicGroupId).Returns(
            [new DoodadFunc { GroupId = PublicGroupId, PermId = (uint)DoodadFuncPermission.Public }]);
        return doodads;
    }

    private static Doodad OwnedDoodad(params uint[] funcGroupIds) => new()
    {
        ObjId = 4242,
        OwnerType = DoodadOwnerType.Character,
        OwnerId = OwnerId,
        IsVisible = true,
        Template = new DoodadTemplate
        {
            FuncGroups = funcGroupIds.Select(id => new DoodadFuncGroups { Id = id }).ToList()
        }
    };

    [Test]
    public async Task OwnersDoodadWithAFamilyFuncIsSentAgain()
    {
        // The family func sits in a later phase than the current one: the whole template counts.
        var doodad = OwnedDoodad(PublicGroupId, FamilyGroupId);

        await Assert.That(DoodadFamilyRefresh.NeedsResend(doodad, [OwnerId], Funcs().Object)).IsTrue();
    }

    [Test]
    public async Task DoodadWithoutAFamilyFuncIsLeftAlone()
    {
        var doodad = OwnedDoodad(PublicGroupId);

        await Assert.That(DoodadFamilyRefresh.NeedsResend(doodad, [OwnerId], Funcs().Object)).IsFalse();
    }

    [Test]
    public async Task OtherOwnersSystemAndHiddenDoodadsAreLeftAlone()
    {
        var doodads = Funcs().Object;
        var system = OwnedDoodad(FamilyGroupId);
        system.OwnerType = DoodadOwnerType.System;
        var hidden = OwnedDoodad(FamilyGroupId);
        hidden.IsVisible = false;

        await Assert.That(DoodadFamilyRefresh.NeedsResend(OwnedDoodad(FamilyGroupId), [OwnerId + 1], doodads)).IsFalse();
        await Assert.That(DoodadFamilyRefresh.NeedsResend(system, [OwnerId], doodads)).IsFalse();
        await Assert.That(DoodadFamilyRefresh.NeedsResend(hidden, [OwnerId], doodads)).IsFalse();
        await Assert.That(DoodadFamilyRefresh.NeedsResend(null, [OwnerId], doodads)).IsFalse();
    }

    [Test]
    public async Task ResendRemovesTheDoodadThenCreatesItAgain()
    {
        var wire = new RecordingSession();
        var viewer = new Character(new UnitCustomModelParams()) { Id = 7, ObjId = 77, Name = "Viewer" };
        viewer.Connection = new GameConnection(wire) { ActiveChar = viewer };

        DoodadFamilyRefresh.Resend(OwnedDoodad(FamilyGroupId), viewer);

        await Assert.That(wire.Packets.Count).IsEqualTo(2);
        await Assert.That(TypeIdOf(wire.Packets[0])).IsEqualTo(SCOffsets.SCDoodadRemovedPacket);
        await Assert.That(TypeIdOf(wire.Packets[1])).IsEqualTo(SCOffsets.SCDoodadCreatedPacket);
    }

    private static ushort TypeIdOf(byte[] bytes)
    {
        var stream = new PacketStream(bytes);
        stream.ReadUInt16(); // total length
        stream.ReadByte();   // 0xDD flag
        stream.ReadByte();   // level
        stream.ReadByte();   // crc
        stream.ReadByte();   // counter
        return stream.ReadUInt16();
    }
}
