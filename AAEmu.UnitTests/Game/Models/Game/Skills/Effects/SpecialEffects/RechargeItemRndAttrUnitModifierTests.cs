using System.Reflection;
using System.Runtime.CompilerServices;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Effects.Enums;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects.SpecialEffects;

[NotInParallel]
public class RechargeItemRndAttrUnitModifierTests
{
    private const uint CleanerTemplateId = 54515;
    private const uint CleanerSkillId = 51047;
    private const uint CostumeTemplateId = 40001;
    private static readonly DateTime OldCharge = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private FieldInfo _skillManagerSingletonField;
    private object _previousSkillManager;
    private FieldInfo _itemManagerSingletonField;
    private object _previousItemManager;
    private FieldInfo _questManagerSingletonField;
    private object _previousQuestManager;

    [Before(Test)]
    public void SetupSkillManager()
    {
        _skillManagerSingletonField = typeof(Singleton<SkillManager>)
            .GetField("s_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        _previousSkillManager = _skillManagerSingletonField.GetValue(null);
        _skillManagerSingletonField.SetValue(null, new SkillManager(
            Mock.Of<IAnimationManager>().Object,
            Mock.Of<IPlotManager>().Object));
        _itemManagerSingletonField = typeof(Singleton<ItemManager>)
            .GetField("s_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        _previousItemManager = _itemManagerSingletonField.GetValue(null);
        var itemManager = new ItemManager(null, null, null, null, null, null);
        typeof(ItemManager).GetField("_allItems", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(itemManager, new Dictionary<ulong, Item>());
        typeof(ItemManager).GetField("_templates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(itemManager, new Dictionary<uint, ItemTemplate>());
        _itemManagerSingletonField.SetValue(null, itemManager);
        _questManagerSingletonField = typeof(Singleton<QuestManager>)
            .GetField("s_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        _previousQuestManager = _questManagerSingletonField.GetValue(null);
        _questManagerSingletonField.SetValue(null,
            new QuestManager(Mock.Of<ITaskManager>().Object, Mock.Of<IZoneManager>().Object));
    }

    [After(Test)]
    public void RestoreSingletons()
    {
        _skillManagerSingletonField.SetValue(null, _previousSkillManager);
        _itemManagerSingletonField.SetValue(null, _previousItemManager);
        _questManagerSingletonField.SetValue(null, _previousQuestManager);
    }

    [Test]
    public async Task ACleanerCastOnItsCostume_RestartsTheChargeAndUsesUpOneCleaner()
    {
        var (skill, player, cleaner, costume, caster, target) = Cast(costumeRestrictId: CleanerTemplateId);

        skill.ApplyEffects(player, caster, player, target, null);

        await Assert.That(skill.Cancelled).IsFalse();
        await Assert.That(costume.ChargeStartTime).IsGreaterThan(OldCharge);
        await Assert.That(cleaner.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ACleanerCastOnAnItemItDoesNotFit_KeepsTheCleanerAndTheCharge()
    {
        var (skill, player, cleaner, costume, caster, target) = Cast(costumeRestrictId: CleanerTemplateId + 1);

        skill.ApplyEffects(player, caster, player, target, null);

        await Assert.That(skill.Cancelled).IsTrue();
        await Assert.That(costume.ChargeStartTime).IsEqualTo(OldCharge);
        await Assert.That(cleaner.Count).IsEqualTo(2);
    }

    private static (Skill Skill, Character Player, Item Cleaner, EquipItem Costume, SkillItem Caster,
        SkillCastItemTarget Target) Cast(uint costumeRestrictId)
    {
        const uint characterId = 71;
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        var player = new Character(new UnitCustomModelParams()) { Id = characterId, ObjId = characterId };
        typeof(Inventory).GetField(nameof(Inventory.Owner))!.SetValue(inventory, player);
        var bag = new ItemContainer(characterId, SlotType.Inventory, false, player)
        {
            Owner = player,
            ContainerId = 501,
            ContainerSize = 50
        };
        var cleaner = new Item(9001, new ItemTemplate { Id = CleanerTemplateId, MaxCount = 100, UseSkillId = CleanerSkillId }, 2)
        {
            OwnerId = characterId,
            SlotType = SlotType.Inventory,
            Slot = 4,
            _holdingContainer = bag
        };
        var costume = new EquipItem(9002, new EquipItemTemplate
        {
            Id = CostumeTemplateId,
            MaxCount = 1,
            RndAttrUnitModifierLifetime = 60,
            RechargeRndAttrRestrictItemId = costumeRestrictId
        }, 1)
        {
            OwnerId = characterId,
            SlotType = SlotType.Inventory,
            Slot = 5,
            ChargeStartTime = OldCharge,
            _holdingContainer = bag
        };
        bag.Items.Add(cleaner);
        bag.Items.Add(costume);
        bag.UpdateFreeSlotCount();
        typeof(Inventory).GetProperty(nameof(Inventory.Bag))!.SetValue(inventory, bag);
        typeof(Inventory).GetProperty(nameof(Inventory._itemContainers))!
            .SetValue(inventory, new Dictionary<SlotType, ItemContainer> { [SlotType.Inventory] = bag });
        player.Inventory = inventory;

        var caster = new SkillItem { ItemTemplateId = CleanerTemplateId };
        typeof(SkillItem).GetField("_itemId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(caster, cleaner.Id);
        var effect = new SkillEffect
        {
            Template = new SpecialEffect { SpecialEffectTypeId = SpecialType.RechargeItemRndAttrUnitModifier },
            ConsumeSourceItem = true,
            ConsumeItemCount = 1,
            ApplicationMethod = SkillEffectApplicationMethod.Target,
            Chance = 100
        };
        var skill = new Skill(new SkillTemplate { Id = CleanerSkillId, Effects = [effect] });
        return (skill, player, cleaner, costume, caster, new SkillCastItemTarget { ObjId = player.ObjId, Id = costume.Id });
    }

    [Test]
    public async Task ARejectedRecharge_CancelsTheCast_SoTheCleanerIsKept()
    {
        // Daru Costume Cleaner 54515 (skill 51047) used without a costume to recharge.
        var skill = new Skill(new SkillTemplate { Id = 51047 });
        var owner = new Character(new UnitCustomModelParams()) { Id = 1 };

        new RechargeItemRndAttrUnitModifier().Execute(owner, new SkillCasterUnit(1), owner, null, null, skill, null,
            DateTime.UtcNow, 0, 0, 0, 0);

        await Assert.That(skill.Cancelled).IsTrue();
    }
}
