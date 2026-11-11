using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

/// <summary>
/// The instance HUD readout's framing (0x2D8): the copy's zone instance id, then a nested buffer holding
/// <b>three</b> counted arrays — the <c>npcInfo</c> rows (buff id, npc id, broadcasting type, value), a 32-bit
/// keyed point list, then the 64-bit (character id) keyed <c>gainRuleInfo</c> point list. The client only accepts the readout for the copy it is playing and reads
/// the arrays out of the nested buffer, so a plain array tail or a bare world id is discarded.
/// </summary>
public class SCIndunPlayingInfoBroadcastingPacketTests
{
    private const int ZoneInstanceIdSize = 8;

    [Test]
    public async Task Write_EmitsZoneInstanceIdThenNestedArrays()
    {
        // Both point lists are key -> { gain rule id (gainRuleType) -> point }. The 64-bit list is keyed by
        // character id: the client looks the local player up in it to fill its mini scoreboard.
        var packet = new SCIndunPlayingInfoBroadcastingPacket(
            new ZoneInstanceId(280u, 100u),
            [new IndunPlayingInfoNpc(19307u, 621u, 2u, 30u, 60u)],
            new Dictionary<uint, IReadOnlyDictionary<uint, uint>> { [4u] = new Dictionary<uint, uint> { [5u] = 2u } },
            new Dictionary<ulong, IReadOnlyDictionary<uint, uint>>
            {
                [1001ul] = new Dictionary<uint, uint> { [6u] = 1u, [5u] = 3u }
            });

        var stream = new PacketStream();
        packet.Write(stream);
        var bytes = stream.GetBytes();

        // zi = u32 zone key + u32 instance id
        await Assert.That(BitConverter.ToUInt32(bytes, 0)).IsEqualTo(280u);
        await Assert.That(BitConverter.ToUInt32(bytes, 4)).IsEqualTo(100u);

        const int payloadSize = 4 + 16 + 4 + (4 + 2 + 8) + 4 + (8 + 2 + 8 * 2);
        var header = ZoneInstanceIdSize;
        await Assert.That(BitConverter.ToUInt16(bytes, header)).IsEqualTo((ushort)(payloadSize + 4));
        await Assert.That(BitConverter.ToUInt16(bytes, header + 2)).IsEqualTo((ushort)(payloadSize + 4));
        await Assert.That(BitConverter.ToUInt16(bytes, header + 4)).IsEqualTo((ushort)(payloadSize + 2));
        await Assert.That(BitConverter.ToUInt16(bytes, header + 6)).IsEqualTo((ushort)0);

        var p = header + NestedBlobWire.HeaderSize;
        // u32 npcInfo count + 16-byte row: buff id (the client's key), npc id, broadcasting type, value.
        // The timer's whole duration is server-side only.
        await Assert.That(BitConverter.ToUInt32(bytes, p)).IsEqualTo(1u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 4)).IsEqualTo(621u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 8)).IsEqualTo(19307u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 12)).IsEqualTo(2u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 16)).IsEqualTo(30u);
        // u32 count, then per entry: u32 key, u16 n, n x (u32 gain rule id, u32 point)
        await Assert.That(BitConverter.ToUInt32(bytes, p + 20)).IsEqualTo(1u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 24)).IsEqualTo(4u);
        await Assert.That(BitConverter.ToUInt16(bytes, p + 28)).IsEqualTo((ushort)1);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 30)).IsEqualTo(5u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 34)).IsEqualTo(2u);
        // u32 count, then per entry: u64 character id, u16 n, n x (u32 gain rule id, u32 point), ids ascending
        await Assert.That(BitConverter.ToUInt32(bytes, p + 38)).IsEqualTo(1u);
        await Assert.That(BitConverter.ToUInt64(bytes, p + 42)).IsEqualTo(1001ul);
        await Assert.That(BitConverter.ToUInt16(bytes, p + 50)).IsEqualTo((ushort)2);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 52)).IsEqualTo(5u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 56)).IsEqualTo(3u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 60)).IsEqualTo(6u);
        await Assert.That(BitConverter.ToUInt32(bytes, p + 64)).IsEqualTo(1u);
        await Assert.That(bytes.Length).IsEqualTo(p + payloadSize);
    }

    [Test]
    public async Task Write_EmptyReadouts_StillCarriesAllThreeCounts()
    {
        var packet = new SCIndunPlayingInfoBroadcastingPacket(new ZoneInstanceId(280u, 100u), null, null, null);

        var stream = new PacketStream();
        packet.Write(stream);
        var bytes = stream.GetBytes();

        var p = ZoneInstanceIdSize + NestedBlobWire.HeaderSize;
        await Assert.That(bytes.Length).IsEqualTo(p + 12);
        await Assert.That(BitConverter.ToUInt16(bytes, ZoneInstanceIdSize + 4)).IsEqualTo((ushort)14);
        await Assert.That(BitConverter.ToUInt32(bytes, p)).IsEqualTo(0u);     // npcInfo
        await Assert.That(BitConverter.ToUInt32(bytes, p + 4)).IsEqualTo(0u); // 32-bit keyed points
        await Assert.That(BitConverter.ToUInt32(bytes, p + 8)).IsEqualTo(0u); // gainRuleInfo
    }
}
