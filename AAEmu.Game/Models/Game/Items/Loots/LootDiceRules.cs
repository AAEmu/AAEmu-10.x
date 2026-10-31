namespace AAEmu.Game.Models.Game.Items.Loots;

/// <summary>
/// Who wins a loot roll once every eligible member has answered (<c>LootingContainer.FinishRolling</c>): the
/// highest roll, as on the official servers. Rolls are 1-99; a pass (-1) and a roll never answered (0) never win.
/// The client only rolls or passes, so there is no need-before-greed order to respect.
/// </summary>
public static class LootDiceRules
{
    /// <summary>
    /// The member with the highest roll in <paramref name="rolls"/>, or default when nobody rolled (everybody
    /// passed). Members tied on that roll are settled by <paramref name="pickTied"/>, which is given how many are
    /// tied and returns the index of the winner among them, in the order <paramref name="rolls"/> lists them.
    /// </summary>
    public static TMember PickWinner<TMember>(IEnumerable<KeyValuePair<TMember, sbyte>> rolls, Func<int, int> pickTied)
    {
        var tied = HighestRollers(rolls);
        return tied.Count switch
        {
            0 => default,
            1 => tied[0],
            _ => tied[Math.Clamp(pickTied(tied.Count), 0, tied.Count - 1)],
        };
    }

    /// <summary>
    /// The members whose roll is the highest one made, in the order <paramref name="rolls"/> lists them; more than
    /// one only when they rolled the same number, and none when nobody rolled.
    /// </summary>
    public static List<TMember> HighestRollers<TMember>(IEnumerable<KeyValuePair<TMember, sbyte>> rolls)
    {
        var rolled = rolls.Where(r => r.Value > 0).ToList();
        if (rolled.Count == 0)
            return [];

        var highest = rolled.Max(r => r.Value);
        return rolled.Where(r => r.Value == highest).Select(r => r.Key).ToList();
    }
}
