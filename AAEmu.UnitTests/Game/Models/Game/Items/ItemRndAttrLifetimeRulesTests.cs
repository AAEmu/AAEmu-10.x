using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.UnitTests.Game.Models.Game.Items;

public class ItemRndAttrLifetimeRulesTests
{
    private const uint Cleaner = 54515;
    private static readonly DateTime Now = new(2026, 11, 10, 13, 0, 0, DateTimeKind.Utc);

    private static EquipItem Costume(int lifetimeMinutes = 20160, DateTime? start = null) =>
        new(1, new EquipItemTemplate
        {
            Id = 54460,
            RndAttrUnitModifierLifetime = lifetimeMinutes,
            RechargeRndAttrRestrictItemId = Cleaner
        }, 1)
        { ChargeStartTime = start ?? DateTime.MinValue };

    [Test]
    public async Task NoLifetime_IsAlwaysActive()
    {
        await Assert.That(ItemRndAttrLifetimeRules.IsActive(Costume(lifetimeMinutes: 0), Now)).IsTrue();
    }

    [Test]
    public async Task NeverStarted_IsInactive()
    {
        await Assert.That(ItemRndAttrLifetimeRules.IsActive(Costume(), Now)).IsFalse();
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(20159, true)]
    [Arguments(20160, false)]
    public async Task Window_LastsTheTemplateLifetime(int minutesAgo, bool expected)
    {
        var item = Costume(start: Now.AddMinutes(-minutesAgo));

        await Assert.That(ItemRndAttrLifetimeRules.IsActive(item, Now)).IsEqualTo(expected);
    }

    [Test]
    public async Task StartIfNeeded_StartsOnceOnly()
    {
        var item = Costume();

        ItemRndAttrLifetimeRules.StartIfNeeded(item, Now);
        ItemRndAttrLifetimeRules.StartIfNeeded(item, Now.AddDays(1));

        await Assert.That(item.ChargeStartTime).IsEqualTo(Now);
    }

    [Test]
    public async Task StartIfNeeded_LeavesItemsWithoutLifetimeAlone()
    {
        var item = Costume(lifetimeMinutes: 0);

        ItemRndAttrLifetimeRules.StartIfNeeded(item, Now);

        await Assert.That(item.ChargeStartTime).IsEqualTo(DateTime.MinValue);
    }

    [Test]
    public async Task Recharge_WithTheNamedCleaner_RestartsTheWindow()
    {
        var item = Costume(start: Now.AddDays(-30));
        var cleaner = new Item(2, new ItemTemplate { Id = Cleaner }, 1);

        var applied = ItemRndAttrLifetimeRules.TryRecharge(item, cleaner, Now);

        await Assert.That(applied).IsTrue();
        await Assert.That(item.ChargeStartTime).IsEqualTo(Now);
        await Assert.That(ItemRndAttrLifetimeRules.IsActive(item, Now)).IsTrue();
    }

    [Test]
    public async Task Recharge_WithAnotherItem_IsRejected()
    {
        var item = Costume(start: Now.AddDays(-30));
        var other = new Item(2, new ItemTemplate { Id = 1 }, 1);

        await Assert.That(ItemRndAttrLifetimeRules.TryRecharge(item, other, Now)).IsFalse();
        await Assert.That(item.ChargeStartTime).IsEqualTo(Now.AddDays(-30));
    }
}
