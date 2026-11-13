using System.Collections.Concurrent;
using System.Reflection;

using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.C2G;

namespace AAEmu.UnitTests.Game.Core.Network;

public class GameNetworkInstancePacketRegistrationTests
{
    [Test]
    public async Task AddInstanceVisitCount_IsRoutedToItsHandler()
    {
        // The Island of Abundance Pass is registered through this packet; unregistered, it was dropped as UNKNOWN.
        var handler = (GameProtocolHandler)typeof(GameNetwork)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(GameNetwork.Instance)!;
        var packets = (ConcurrentDictionary<byte, ConcurrentDictionary<uint, Type>>)
            typeof(GameProtocolHandler)
                .GetField("_packets", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(handler)!;

        await Assert.That(packets[1].TryGetValue(CSOffsets.CSAddInstanceVisitCountPacket, out var registered)).IsTrue();
        await Assert.That(registered).IsEqualTo(typeof(CSAddInstanceVisitCountPacket));
    }
}
