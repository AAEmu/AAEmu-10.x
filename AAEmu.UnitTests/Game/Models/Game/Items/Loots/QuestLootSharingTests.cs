using AAEmu.Game.Models.Game.Items.Loots;

namespace AAEmu.UnitTests.Game.Models.Game.Items.Loots;

public class QuestLootSharingTests
{
    /// <summary>
    /// "Captives and Coercion" (7141) needs Scorpion's Tears Antidote from NPC 15585. A party of two
    /// on the quest killed it and only one got the antidote and the gather step (live 2026-11-02).
    /// </summary>
    [Test]
    public async Task CopiesFor_GivesEveryEligibleQuestHolderACopy()
    {
        var quests = new Dictionary<string, uint[]>
        {
            ["killer"] = [7141],
            ["partner"] = [7141],
            ["off quest"] = [7139],
        };

        var copies = QuestLootSharing.CopiesFor(7141, quests.Keys, (name, questId) => quests[name].Contains(questId));

        await Assert.That(copies).IsEquivalentTo(["killer", "partner"]);
    }

    [Test]
    public async Task CopiesFor_NonQuestItemGetsNoCopies()
    {
        var copies = QuestLootSharing.CopiesFor(0, ["killer"], (_, _) => true);

        await Assert.That(copies).IsEmpty();
    }

    [Test]
    [Arguments(0u, 7u, true)]
    [Arguments(7u, 7u, true)]
    [Arguments(8u, 7u, false)]
    public async Task IsVisibleTo_SharedLootOrOwnCopyOnly(uint reservedFor, uint characterId, bool expected)
    {
        await Assert.That(QuestLootSharing.IsVisibleTo(reservedFor, characterId)).IsEqualTo(expected);
    }
}
