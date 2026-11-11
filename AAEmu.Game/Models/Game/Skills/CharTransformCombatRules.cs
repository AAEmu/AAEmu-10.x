using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>Combat restrictions of a character transform (Daru Transformation) beyond skill_reqs 417.</summary>
public static class CharTransformCombatRules
{
    public static IReadOnlyList<EquipmentItemSlot> DisarmedSlots { get; } =
        [EquipmentItemSlot.Mainhand, EquipmentItemSlot.Offhand, EquipmentItemSlot.Ranged];

    public static readonly TimeSpan RefusalNoticeInterval = TimeSpan.FromSeconds(5);

    // enum_skill_book_categories 2 'attack'
    public const byte AttackSkillBookCategory = 2;

    // Escape, Rider's Escape and Report Suspicious User sit on the attack tab but are not combat.
    public static IReadOnlySet<uint> AttackBookUtilitySkillIds { get; } = new HashSet<uint> { 20256, 20297, 22143 };

    public static bool IsSlotDisarmed(bool isCharTransformed, EquipmentItemSlot slot) =>
        isCharTransformed && DisarmedSlots.Contains(slot);

    public static bool ShouldNotifyRefusal(DateTime lastNotifiedUtc, DateTime nowUtc) =>
        nowUtc - lastNotifiedUtc >= RefusalNoticeInterval;

    /// <summary>Cooldown left by a transform buff that runs out (plot 7031 does the same, but dies on relog).</summary>
    public static (uint SkillId, uint DurationMs) ExpiryCooldown(uint buffId) => buffId switch
    {
        32459 => (50650, 14_400_000),
        _ => (0, 0)
    };

    public static bool BlocksSkill(bool isCharTransformed, uint skillId, byte skillBookCategoryId)
    {
        if (!isCharTransformed)
            return false;
        if (BuffRemoveOnRules.IsAutoAttack(skillId))
            return true;
        return skillBookCategoryId == AttackSkillBookCategory && !AttackBookUtilitySkillIds.Contains(skillId);
    }
}
