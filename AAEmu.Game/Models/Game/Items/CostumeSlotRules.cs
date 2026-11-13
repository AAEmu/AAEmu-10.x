namespace AAEmu.Game.Models.Game.Items;

/// <summary>Appearance costume slots never add stats; the Daru performance slot only while transformed.</summary>
public static class CostumeSlotRules
{
    public static bool CountsTowardStats(EquipmentItemSlot slot, bool isCharTransformed) => slot switch
    {
        EquipmentItemSlot.CosplayLooks or EquipmentItemSlot.RaceCosplayLooks => false,
        EquipmentItemSlot.RaceCosplay => isCharTransformed,
        _ => true
    };
}
