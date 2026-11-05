using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Journal lines are Progress components. Their counters are the Progress-local
/// index (first Progress component is slot 0). None-step Use/Gather acts take
/// leftover slots so they do not overwrite those journal counters, and they
/// still read the stored count when the step evaluates.
/// </summary>
public static class QuestObjectiveSlotRules
{
    public const byte NoSlot = 0xFF;
    public const int MaxSlots = 5;

    public static byte SlotForProgressComponent(int progressComponentIndex, int maxSlots)
    {
        if (progressComponentIndex < 0 || progressComponentIndex >= maxSlots)
            return NoSlot;
        return (byte)progressComponentIndex;
    }

    public static byte NextIndex(ref byte actIndex, bool countsAsObjective, int maxSlots)
    {
        if (!countsAsObjective || actIndex >= maxSlots)
            return NoSlot;
        var assigned = actIndex;
        actIndex++;
        return assigned;
    }

    /// <summary>The stored count in <paramref name="index"/>, or 0 for no slot, a slot past the array or no array.</summary>
    public static int CountAt(int[] objectives, byte index) =>
        objectives != null && index != NoSlot && index < objectives.Length ? objectives[index] : 0;

    public static bool RunActUsesStoredCount(QuestComponentKind kind, byte index, int objectivesLength)
    {
        if (index == NoSlot || index >= objectivesLength)
            return false;
        return kind is QuestComponentKind.Progress or QuestComponentKind.None;
    }

    /// <summary>
    /// Accept/orb can play a cinema while the step is still None.
    /// Only the Progress step should tick a cinema journal line.
    /// </summary>
    public static bool CinemaWatchCounts(QuestComponentKind step) =>
        step == QuestComponentKind.Progress;

    public static bool ObjectiveActMet(byte index, int required, bool overrideCompleted, int[] objectives)
    {
        if (overrideCompleted)
            return true;
        if (objectives == null || index == NoSlot || index >= objectives.Length)
            return false;
        return objectives[index] >= required;
    }
}
