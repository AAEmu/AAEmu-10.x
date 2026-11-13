using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

// Daru Costume Cleaner: restarts a costume's synthesis effect lifetime.
public class RechargeItemRndAttrUnitModifier : SpecialEffectAction
{
    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int value1,
        int value2,
        int value3,
        int value4)
    {
        // A cancelled cast keeps its source item: the cleaner is only used up by a recharge that happened.
        if (caster is not Character owner ||
            casterObj is not SkillItem sourceCaster ||
            targetObj is not SkillCastItemTarget itemTarget)
        {
            if (skill != null)
                skill.Cancelled = true;
            return;
        }

        var sourceItem = owner.Inventory.GetItemById(sourceCaster.ItemId);
        if (owner.Inventory.GetItemById(itemTarget.Id) is not EquipItem equipItem ||
            !ItemRndAttrLifetimeRules.TryRecharge(equipItem, sourceItem, DateTime.UtcNow))
        {
            Logger.Warn("RechargeItemRndAttrUnitModifier rejected owner={0} source={1} target={2}",
                owner.Id, sourceCaster.ItemId, itemTarget.Id);
            if (skill != null)
                skill.Cancelled = true;
            return;
        }

        owner.SendPacket(new SCItemDetailUpdatedPacket(equipItem));
        owner.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.RechargeRndAttrUnitModifier, [], []));
        if (equipItem.SlotType == SlotType.Equipment)
            owner.UpdateGearBonuses(null, null);
    }
}
