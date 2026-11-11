using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>Combat restrictions of a character transform (Daru Transformation) beyond skill_reqs 417.</summary>
public static class CharTransformCombatRules
{
    public static IReadOnlyList<EquipmentItemSlot> DisarmedSlots { get; } =
        [EquipmentItemSlot.Mainhand, EquipmentItemSlot.Offhand, EquipmentItemSlot.Ranged];

    public static readonly TimeSpan RefusalNoticeInterval = TimeSpan.FromSeconds(5);

    // enum_skill_book_categories 2 'attack'
    public const byte AttackSkillBookCategory = 2;

    public static bool IsSlotDisarmed(bool isCharTransformed, EquipmentItemSlot slot) =>
        isCharTransformed && DisarmedSlots.Contains(slot);

    public static bool ShouldNotifyRefusal(DateTime lastNotifiedUtc, DateTime nowUtc) =>
        nowUtc - lastNotifiedUtc >= RefusalNoticeInterval;

    /// <summary>An attack-tab skill that only runs special effects (escape, report) is not combat; casting another skill may be.</summary>
    public static bool HasOnlySpecialEffects(SkillTemplate skill)
        => skill?.Effects is { Count: > 0 } effects
           && effects.All(effect => effect.Template is SpecialEffect { SpecialEffectTypeId: not SpecialType.SkillUse });

    public static bool BlocksSkill(bool isCharTransformed, uint skillId, byte skillBookCategoryId, bool onlySpecialEffects)
    {
        if (!isCharTransformed)
            return false;
        if (BuffRemoveOnRules.IsAutoAttack(skillId))
            return true;
        return skillBookCategoryId == AttackSkillBookCategory && !onlySpecialEffects;
    }
}
