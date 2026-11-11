using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

public class SCInstanceVisitCountsPacketTests
{
    [Test]
    public async Task MoreRowsThanTheClientHolds_AreCutToItsFifty()
    {
        var rows = Enumerable.Range(1, 60).Select(i => new InstanceVisitCountRecord(i, 0, 0, 0, 1)).ToList();

        var bytes = new SCInstanceVisitCountsPacket(rows).Write(new PacketStream()).GetBytes();

        await Assert.That(BitConverter.ToInt32(bytes, 0)).IsEqualTo(SCInstanceVisitCountsPacket.MaxRows);
        await Assert.That(bytes.Length).IsEqualTo(4 + SCInstanceVisitCountsPacket.MaxRows * 20);
        await Assert.That(SCInstanceVisitCountsPacket.MaxRows).IsEqualTo(50);
    }
}
