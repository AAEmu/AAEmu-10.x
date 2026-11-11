using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.DoodadObj.Static;

namespace AAEmu.Game.Models.Game.Units;

/// <summary>Daru Transformation: +50% donkey move speed while the rider carries a trade pack.</summary>
public static class CharTransformMountRules
{
    // const_tags donkey_class_to_check_transformation
    public const uint DonkeyNpcTag = 5958;

    // "Burning Additives": a single MoveSpeedMul +250 (+25%) modifier. Its own 4 min is stretched to the form's 30 min.
    public const uint DonkeySpeedBuffId = 20584;
    public const int DonkeySpeedBuffDurationMs = 1_800_000;

    public static bool ShouldBoost(bool riderTransformed, bool riderWearsTradePack, bool mountIsDonkey) =>
        riderTransformed && riderWearsTradePack && mountIsDonkey;

    public static bool IsTradePack(ItemTemplate backpack) =>
        backpack is BackpackTemplate { BackpackType: BackpackType.TradePack or BackpackType.TradeGoods };

    public static bool IsDonkey(IReadOnlySet<uint> npcTags) => npcTags?.Contains(DonkeyNpcTag) ?? false;

    /// <summary>Adds or removes the speed buff on the donkey this character drives.</summary>
    public static void Refresh(Character character)
    {
        if (character == null)
            return;

        foreach (var mate in character.ParentWorld?.MateManager?.GetActiveMates(character.Id) ?? [])
        {
            var driving = character.IsRiding &&
                          mate.Passengers.TryGetValue(AttachPointKind.Driver, out var seat) &&
                          seat._objId == character.ObjId;
            var boost = driving && ShouldBoost(
                character.IsCharTransformed,
                IsTradePack(character.Equipment?.GetItemBySlot((int)EquipmentItemSlot.Backpack)?.Template),
                IsDonkey(TagsGameData.Instance.GetTagsByTargetId(TagsGameData.TagType.Npcs, mate.TemplateId)));
            SetBoost(mate, boost);
        }
    }

    public static void SetBoost(Mate mate, bool boost)
    {
        var has = mate.Buffs.CheckBuff(DonkeySpeedBuffId);
        if (boost && !has)
        {
            var template = SkillManager.Instance.GetBuffTemplate(DonkeySpeedBuffId);
            if (template != null)
                mate.Buffs.AddBuff(new Buff(mate, mate, new SkillCasterUnit(mate.ObjId), template, null, DateTime.UtcNow),
                    forcedDuration: DonkeySpeedBuffDurationMs);
        }
        else if (!boost && has)
            mate.Buffs.RemoveBuff(DonkeySpeedBuffId);
    }
}
