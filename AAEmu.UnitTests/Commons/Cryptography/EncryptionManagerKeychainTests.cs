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

        manager.TryNextSCMessageCount(OtherConnection, AccountId, out _);

        await Assert.That(PublicKey(manager, LiveConnection)).IsEqualTo(before);
    }

    [Test]
    public async Task SendCounters_AreTrackedPerConnection()
    {
        var manager = new EncryptionManager();
        manager.PrepareEnterWorldKeys(LiveConnection, AccountId);
        manager.PrepareEnterWorldKeys(OtherConnection, AccountId);
        manager.TryNextSCMessageCount(LiveConnection, AccountId, out _);
        manager.TryNextSCMessageCount(LiveConnection, AccountId, out _);

        manager.TryNextSCMessageCount(OtherConnection, AccountId, out _);

        manager.TryGetSCMessageCount(LiveConnection, AccountId, out var live);
        manager.TryGetSCMessageCount(OtherConnection, AccountId, out var other);
        await Assert.That(live).IsEqualTo((byte)2);
        await Assert.That(other).IsEqualTo((byte)1);
    }

    [Test]
    public async Task SendWithoutKeychain_FailsClosedAndCreatesNone()
    {
        var manager = new EncryptionManager();

        var allocated = manager.TryNextSCMessageCount(OtherConnection, AccountId, out _);

        await Assert.That(allocated).IsFalse();
        await Assert.That(manager.TryGetSCMessageCount(OtherConnection, AccountId, out _)).IsFalse();
    }

    [Test]
    public async Task SendAfterRemoveConnection_DoesNotRecreateTheKeychain()
    {
        var manager = new EncryptionManager();
        manager.PrepareEnterWorldKeys(LiveConnection, AccountId);
        manager.TryNextSCMessageCount(LiveConnection, AccountId, out _);

        manager.RemoveConnection(LiveConnection);
        var allocated = manager.TryNextSCMessageCount(LiveConnection, AccountId, out _);

        await Assert.That(allocated).IsFalse();
        await Assert.That(manager.TryGetSCMessageCount(LiveConnection, AccountId, out _)).IsFalse();
        await Assert.That(() => PublicKey(manager, LiveConnection)).Throws<InvalidOperationException>();
    }

    private static string PublicKey(EncryptionManager manager, uint connectionId) =>
        Convert.ToHexString(manager.WriteKeyParams(connectionId, AccountId, new PacketStream()).GetBytes());
}
