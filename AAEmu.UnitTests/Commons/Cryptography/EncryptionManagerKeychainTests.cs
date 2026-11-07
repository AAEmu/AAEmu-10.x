using AAEmu.Commons.Cryptography;
using AAEmu.Commons.Network;

namespace AAEmu.UnitTests.Commons.Cryptography;

public class EncryptionManagerKeychainTests
{
    private const ulong AccountId = 7;
    private const uint LiveConnection = 11;
    private const uint OtherConnection = 12;

    [Test]
    public async Task SendOnAnotherConnectionOfTheAccount_KeepsTheLiveKeychain()
    {
        var manager = new EncryptionManager();
        manager.PrepareEnterWorldKeys(LiveConnection, AccountId);
        var before = PublicKey(manager, LiveConnection);

        manager.NextSCMessageCount(OtherConnection, AccountId);

        await Assert.That(PublicKey(manager, LiveConnection)).IsEqualTo(before);
    }

    [Test]
    public async Task SendCounters_AreTrackedPerConnection()
    {
        var manager = new EncryptionManager();
        manager.NextSCMessageCount(LiveConnection, AccountId);
        manager.NextSCMessageCount(LiveConnection, AccountId);

        manager.NextSCMessageCount(OtherConnection, AccountId);

        await Assert.That(manager.GetSCMessageCount(LiveConnection, AccountId)).IsEqualTo((byte)2);
        await Assert.That(manager.GetSCMessageCount(OtherConnection, AccountId)).IsEqualTo((byte)1);
    }

    [Test]
    public async Task RemoveConnection_DropsItsKeychain()
    {
        var manager = new EncryptionManager();
        manager.PrepareEnterWorldKeys(LiveConnection, AccountId);
        var before = PublicKey(manager, LiveConnection);

        manager.RemoveConnection(LiveConnection);

        await Assert.That(PublicKey(manager, LiveConnection)).IsNotEqualTo(before);
    }

    private static string PublicKey(EncryptionManager manager, uint connectionId) =>
        Convert.ToHexString(manager.WriteKeyParams(connectionId, AccountId, new PacketStream()).GetBytes());
}
