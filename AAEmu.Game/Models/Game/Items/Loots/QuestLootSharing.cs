namespace AAEmu.Game.Models.Game.Items.Loots;

/// <summary>
/// Quest drops (<c>items.loot_quest_id</c>) from an NPC kill go to every player with a claim on the
/// loot who has the quest, one copy each. A single shared copy left the rest of the party without
/// the item their gather step needs ("Captives and Coercion" 7141, "A Moving Target" 7139).
/// </summary>
public static class QuestLootSharing
{
    /// <summary>The players that get their own copy of a drop for <paramref name="lootQuestId"/>.</summary>
    public static List<T> CopiesFor<T>(uint lootQuestId, IEnumerable<T> eligible, Func<T, uint, bool> hasQuest)
    {
        if (lootQuestId == 0 || eligible == null)
            return [];
        return eligible.Where(player => player != null && hasQuest(player, lootQuestId)).ToList();
    }

    /// <summary>Whether the entry shows in (and may be taken from) the bag of <paramref name="characterId"/>.</summary>
    public static bool IsVisibleTo(uint reservedFor, uint characterId) =>
        reservedFor == 0 || reservedFor == characterId;
}
