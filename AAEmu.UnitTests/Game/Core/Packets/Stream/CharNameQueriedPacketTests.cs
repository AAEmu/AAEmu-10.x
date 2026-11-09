using AAEmu.Game.Core.Packets.S2C;

namespace AAEmu.UnitTests.Game.Core.Packets.Stream;

public class CharNameQueriedPacketTests
{
    [Test]
    public async Task Response_EchoesTheQueryKeyBeforeTheName()
    {
        var key = new NameCacheKey(NameCacheKey.CharacterName, 1002, 0xFF, 0);
        var encoded = new TCCharNameQueriedPacket(key, "Xu").Encode().GetBytes();
        byte[] expected =
        [
            0x1F, 0x00,
            0x09, 0x00,
            0x01, 0x00,
            0xEA, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x58, 0x75
        ];

        await Assert.That(Hex.Of(encoded)).IsEqualTo(Hex.Of(expected));
    }
}
