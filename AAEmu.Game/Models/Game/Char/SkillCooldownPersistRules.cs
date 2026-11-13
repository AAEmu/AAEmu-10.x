using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>Which skill cooldowns outlive a logout, and how much of one is left at login.</summary>
public static class SkillCooldownPersistRules
{
    /// <summary>A running cooldown is saved; the run-once marker (<see cref="uint.MaxValue"/>) is not.</summary>
    public static bool ShouldSave(UnitCooldowns.CooldownSnapshot snapshot)
        => snapshot.Remaining > 0 && snapshot.Duration != uint.MaxValue;

    /// <summary>Cooldowns keep running while the character is offline.</summary>
    public static uint RemainingMs(DateTime endsAtUtc, DateTime nowUtc)
    {
        if (endsAtUtc <= nowUtc)
            return 0;

        var remaining = Math.Ceiling((endsAtUtc - nowUtc).TotalMilliseconds);
        return remaining >= uint.MaxValue ? uint.MaxValue : (uint)remaining;
    }
}
