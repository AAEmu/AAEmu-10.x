using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncConsumeChanger : DoodadFuncTemplate
{
    // doodad_func_consume_changers
    // 10.0.2.13: slot_id/count removed; the source item is now identified by tag_id.
    public uint TagId { get; set; }

    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        Logger.Trace("DoodadFuncConsumeChanger");
        // Store trade-pack into a trade-pack storage
        if (caster is not Character player)
            return;
        // 10.0.2.13: the consumed source is the equipped trade pack (Backpack slot).
        var sourceItem = player.Equipment.GetItemBySlot((int)EquipmentItemSlot.Backpack);
        if (sourceItem == null)
        {
            player.SendErrorMessage(ErrorMessageType.UnknownItem);
            return;
        }

        // The tag (3124 "backpack storage" for cart storage) is what 10.0.2.13 gates on. The old
        // doodad_func_consume_changer_items list stops at the 1.x packs, so newer packs were refused.
        var allowedByTag = TagId > 0 &&
                           TagsGameData.Instance.GetIdsByTagId(TagsGameData.TagType.Items, TagId).Contains(sourceItem.TemplateId);
        if (!allowedByTag && !DoodadManager.Instance.GetDoodadFuncConsumeChangerItemList(Id).Contains(sourceItem.TemplateId))
        {
            player.SendErrorMessage(ErrorMessageType.StoreInvalidItem);
            return;
        }

        owner.ItemId = sourceItem.Id;
        owner.ItemTemplateId = sourceItem.TemplateId;

        // Move the actual item to the owner's SystemContainer
        player.Inventory.SystemContainer.AddOrMoveExistingItem(ItemTaskType.DoodadItemChanger, sourceItem);
        owner.ToNextPhase = true;
    }
}
