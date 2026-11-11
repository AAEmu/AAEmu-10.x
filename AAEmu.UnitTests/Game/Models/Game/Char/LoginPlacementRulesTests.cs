using AAEmu.Game.Models.Game.Char;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class LoginPlacementRulesTests
{
    private const uint MainWorld = 0;

    [Test]
    public async Task SavedInADungeonWorld_LeavesIt()
    {
        // A character was saved in zone 410 (instance_life_dungeon_daru, world 22) after its copy was gone.
        await Assert.That(LoginPlacementRules.MustLeaveSavedZone(22, MainWorld)).IsTrue();
    }

    [Test]
    public async Task SavedInAZoneWithNoWorld_LeavesIt()
    {
        // Zone key 249 (w_tornado_mea) maps to no world; the lookup answers uint.MaxValue.
        await Assert.That(LoginPlacementRules.MustLeaveSavedZone(uint.MaxValue, MainWorld)).IsTrue();
    }

    [Test]
    public async Task SavedInTheMainWorld_Stays()
    {
        await Assert.That(LoginPlacementRules.MustLeaveSavedZone(MainWorld, MainWorld)).IsFalse();
    }
}
