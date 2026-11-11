using System.Numerics;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using Microsoft.Extensions.Configuration;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadPurchaseRulesTests
{
    // doodad_func_purchases.coin_item_id of the Mirage Isle design displays, and the max_range of
    // their func skill.
    private const uint GildaStar = 23633;
    private const float SkillRange = 4f;

    private static WorldConfig LoadShippedConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Configurations", "World.json");
        return new ConfigurationBuilder()
            .AddJsonFile(path, optional: false)
            .Build()
            .GetSection("World")
            .Get<WorldConfig>()!;
    }

    [Test]
    public async Task ShippedConfig_GivesExtraReachToGildaStarPurchasesOnly()
    {
        var config = LoadShippedConfig();

        await Assert.That(config.DoodadCoinPurchaseExtraReachMeters.Count).IsEqualTo(1);
        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(
            config.DoodadCoinPurchaseExtraReachMeters, GildaStar, out var extraReach)).IsTrue();
        await Assert.That(extraReach > 0f).IsTrue();
    }

    [Test]
    public async Task UnconfiguredServer_GivesNoExtraReach()
    {
        // The code carries no distance of its own: without the World.json entry nothing is added.
        var config = new WorldConfig();

        await Assert.That(config.DoodadCoinPurchaseExtraReachMeters.Count).IsEqualTo(0);
        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(
            config.DoodadCoinPurchaseExtraReachMeters, GildaStar, out var extraReach)).IsFalse();
        await Assert.That(extraReach).IsEqualTo(0f);
        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(null, GildaStar, out _)).IsFalse();
    }

    [Test]
    public async Task TryGetExtraReach_ReturnsTheConfiguredValueForThatCoinItem()
    {
        var configured = new Dictionary<uint, float> { [GildaStar] = 2.5f };

        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(configured, GildaStar, out var extraReach)).IsTrue();
        await Assert.That(extraReach).IsEqualTo(2.5f);
    }

    [Test]
    public async Task TryGetExtraReach_IgnoresPurchasesPaidInAnotherItemOrInCurrency()
    {
        var configured = new Dictionary<uint, float> { [GildaStar] = 2.5f, [0] = 2.5f };

        // Another token (a different coin_item_id on the same func skill).
        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(configured, 26921, out _)).IsFalse();
        // coin_item_id 0 is a purchase paid in currency: a "0" key must not open those up.
        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(configured, 0, out _)).IsFalse();
    }

    [Test]
    [Arguments(0f)]
    [Arguments(-3f)]
    [Arguments(float.NaN)]
    [Arguments(float.PositiveInfinity)]
    public async Task TryGetExtraReach_IgnoresValuesThatAreNotAPositiveDistance(float value)
    {
        var configured = new Dictionary<uint, float> { [GildaStar] = value };

        await Assert.That(DoodadPurchaseRules.TryGetExtraReach(configured, GildaStar, out var extraReach)).IsFalse();
        await Assert.That(extraReach).IsEqualTo(0f);
    }

    [Test]
    public async Task IsWithinExtraReach_ExtraReachIsTheHorizontalLimit()
    {
        var doodad = new Vector3(100f, 200f, 50f);
        const float ExtraReach = 3f;
        var limit = SkillRange + ExtraReach;

        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(limit - 0.01f, 0f, 0f), doodad, SkillRange, ExtraReach)).IsTrue();
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(limit + 0.01f, 0f, 0f), doodad, SkillRange, ExtraReach)).IsFalse();
        // Diagonal: the flat distance counts, not either axis.
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(5f, 5f, 0f), doodad, SkillRange, ExtraReach)).IsFalse();
    }

    [Test]
    public async Task IsWithinExtraReach_HeightStaysWithinTheSkillRange()
    {
        var doodad = new Vector3(100f, 200f, 50f);
        const float ExtraReach = 3f;

        // 5.5 m out and 1.6 m up: past the 3D skill range, inside the extra reach.
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(5.5f, 0f, 1.6f), doodad, SkillRange, ExtraReach)).IsTrue();

        // The extra reach adds nothing in height: a floor above or below is refused as before.
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(1f, 0f, SkillRange + 0.01f), doodad, SkillRange, ExtraReach)).IsFalse();
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(1f, 0f, -(SkillRange + 0.01f)), doodad, SkillRange, ExtraReach)).IsFalse();
        await Assert.That(DoodadPurchaseRules.IsWithinExtraReach(
            doodad + new Vector3(5.5f, 0f, SkillRange + ExtraReach), doodad, SkillRange, ExtraReach)).IsFalse();
    }
}
