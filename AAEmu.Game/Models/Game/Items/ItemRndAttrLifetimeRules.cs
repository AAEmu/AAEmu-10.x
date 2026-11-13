using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.Game.Models.Game.Items;

/// <summary>Synthesis effects with a lifetime (Daru costumes): active from ChargeStartTime for the template's minutes.</summary>
public static class ItemRndAttrLifetimeRules
{
    public static bool IsActive(EquipItem item, DateTime nowUtc)
    {
        if (item?.Template is not EquipItemTemplate { RndAttrUnitModifierLifetime: > 0 } template)
            return true;
        if (item.ChargeStartTime == DateTime.MinValue)
            return false;
        return nowUtc < item.ChargeStartTime.AddMinutes(template.RndAttrUnitModifierLifetime);
    }

    /// <summary>True for the timer tick in which the effects run out, so a worn piece's bonuses are rebuilt once.</summary>
    public static bool ExpiredBetween(EquipItem item, DateTime previousUtc, DateTime nowUtc) =>
        item?.Template is EquipItemTemplate { RndAttrUnitModifierLifetime: > 0 } &&
        IsActive(item, previousUtc) && !IsActive(item, nowUtc);

    public static void StartIfNeeded(EquipItem item, DateTime nowUtc)
    {
        if (item?.Template is EquipItemTemplate { RndAttrUnitModifierLifetime: > 0 } &&
            item.ChargeStartTime == DateTime.MinValue)
            item.ChargeStartTime = nowUtc;
    }

    public static bool TryRecharge(EquipItem item, Item source, DateTime nowUtc)
    {
        if (item?.Template is not EquipItemTemplate { RndAttrUnitModifierLifetime: > 0 } template ||
            source == null || template.RechargeRndAttrRestrictItemId != source.TemplateId)
            return false;

        item.ChargeStartTime = nowUtc;
        return true;
    }
}
