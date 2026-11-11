using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class CharTransformMountRulesTests
{
    [Test]
    [Arguments(true, true, true, true)]
    [Arguments(false, true, true, false)]
    [Arguments(true, false, true, false)]
    [Arguments(true, true, false, false)]
    public async Task ShouldBoost_OnlyForATransformedRiderWithATradePackOnADonkey(
        bool transformed, bool tradePack, bool donkey, bool expected)
    {
        await Assert.That(CharTransformMountRules.ShouldBoost(transformed, tradePack, donkey)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(BackpackType.TradePack, true)]
    [Arguments(BackpackType.TradeGoods, true)]
    [Arguments(BackpackType.Glider, false)]
    [Arguments(BackpackType.Fish, false)]
    public async Task IsTradePack_CountsTradePacksAndCargo(BackpackType type, bool expected)
    {
        await Assert.That(CharTransformMountRules.IsTradePack(new BackpackTemplate { BackpackType = type })).IsEqualTo(expected);
    }

    [Test]
    public async Task IsTradePack_NoBackpack_IsFalse()
    {
        await Assert.That(CharTransformMountRules.IsTradePack(null)).IsFalse();
    }

    [Test]
    [Arguments(5958u, true)] // const_tags donkey_class_to_check_transformation
    [Arguments(2571u, false)]
    public async Task IsDonkey_ByTheDonkeyNpcTag(uint tag, bool expected)
    {
        await Assert.That(CharTransformMountRules.IsDonkey(new HashSet<uint> { tag })).IsEqualTo(expected);
    }
}
