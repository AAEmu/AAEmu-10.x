using AAEmu.Game.Models.Game.Char;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

/// <summary>
/// Every UI-data section the server accepts from CSSaveUIData has to reach the client again at login, either
/// because the client requests it (CSRequestUIData at the character list) or because character select pushes it.
/// A section that is saved but never delivered looks persistent while the client stays open and resets after a
/// client restart or a server reboot — the quest tracker report of 2026-11-02 (sections 6 and 7 were saved on
/// logout, requested by nobody and pushed by nobody).
/// </summary>
public class UiDataLoginDeliveryTests
{
    private static readonly ushort[] SupportedSections = [1, 2, 3, 4, 5, 6, 7, 20];

    [Test]
    public async Task EverySupportedSection_IsRequestedByClientOrPushedAtCharacterSelect()
    {
        var delivered = UiData.RequestedByClient.Concat(UiData.SentAtCharacterSelect).ToHashSet();

        foreach (var section in SupportedSections)
            await Assert.That(delivered.Contains(section)).IsTrue();
    }

    [Test]
    public async Task DeliveredSections_AreAllSupported()
    {
        foreach (var section in UiData.RequestedByClient.Concat(UiData.SentAtCharacterSelect))
            await Assert.That(UiData.IsSupported(section)).IsTrue();
    }

    [Test]
    public async Task SupportedSections_MatchIsSupported()
    {
        for (var type = 0; type <= 64; type++)
            await Assert.That(UiData.IsSupported((ushort)type)).IsEqualTo(SupportedSections.Contains((ushort)type));
    }
}
