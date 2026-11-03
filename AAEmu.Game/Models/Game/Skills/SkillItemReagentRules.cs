using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// When the item a skill was cast through is itself the reagent (<c>items.use_skill_as_reagent</c>).
/// </summary>
/// <remarks>
/// A skill cast from an item hands its <see cref="SkillItem"/> on to whatever it triggers. Potions
/// 34006 / 34008 (use skills 46252 / 46257) apply a 10 s buff whose 1 s tick is SpecialEffect
/// SkillUse → skill 33065, still carrying the potion. Treating every one of those nested casts as
/// another use of the item spent 11 potions per right-click and counted 11 item uses. Only the
/// item's own <c>use_skill_id</c> is the use; explicit <c>consume_source_item</c> effects are
/// separate and not affected by this.
/// </remarks>
public static class SkillItemReagentRules
{
    /// <summary>
    /// True when <paramref name="skillId"/> is the skill the item runs when used. An item that names
    /// no use skill (8,377 reagent rows), or whose template is unknown, keeps counting every cast, as before.
    /// </summary>
    public static bool IsItemUseSkill(ItemTemplate itemTemplate, uint skillId) =>
        itemTemplate == null || itemTemplate.UseSkillId == 0 || itemTemplate.UseSkillId == skillId;
}
