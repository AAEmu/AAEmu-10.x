namespace AAEmu.Game.Models.Game.Indun;

/// <summary>
/// A rank-backed reward's selection value per player: the copy's roster ordered by total gain points
/// (highest first), ties and players without points broken by character id. Ranks start at 1.
/// </summary>
public static class InstanceRewardRankRules
{
    public static IReadOnlyDictionary<uint, int> Rank(
        IEnumerable<uint> roster, IReadOnlyDictionary<ulong, IReadOnlyDictionary<uint, uint>> points)
    {
        long Total(uint characterId) =>
            points.TryGetValue(characterId, out var byRule) ? byRule.Values.Sum(p => (long)p) : 0;

        return roster
            .Distinct()
            .OrderByDescending(Total)
            .ThenBy(characterId => characterId)
            .Select((characterId, index) => (characterId, rank: index + 1))
            .ToDictionary(x => x.characterId, x => x.rank);
    }
}
