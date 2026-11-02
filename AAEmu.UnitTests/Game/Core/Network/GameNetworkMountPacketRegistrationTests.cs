using System.Collections.Concurrent;
using System.Reflection;

using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.C2G;

namespace AAEmu.UnitTests.Game.Core.Network;

public class GameNetworkMountPacketRegistrationTests
{
    [Test]
    public async Task RightClickDismount_IsRoutedToInterruptSkill()
    {
        var handler = (GameProtocolHandler)typeof(GameNetwork)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(GameNetwork.Instance)!;
        var packets = (ConcurrentDictionary<byte, ConcurrentDictionary<uint, Type>>)typeof(GameProtocolHandler)
            .GetField("_packets", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(handler)!;

        // 0x089 is the client's InterrupSkillPacket (x2game.dll constructor writes type 0x89);
        // a right-click while riding sends it, and it was dropped as unknown before.
        await Assert.That(CSOffsets.CSInterruptSkillPacket).IsEqualTo((ushort)0x089);
        await Assert.That(packets[1].TryGetValue(CSOffsets.CSInterruptSkillPacket, out var registeredType)).IsTrue();
        await Assert.That(registeredType).IsEqualTo(typeof(CSInterruptSkillPacket));
    }
}
