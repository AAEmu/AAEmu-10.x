using System.Reflection;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// A regrade moves a piece's grade in place. On a worn piece the bonuses that follow the grade used to stay at the
/// old grade until a re-equip or relog; the regrade now re-totals them, as synthesis and awakening do. Seen through a
/// synthesis line whose strength is authored per grade.
/// </summary>
[NotInParallel]
public sealed class GradeEnchantWornPieceTests
{
    private const uint GroupSetId = 9301;
    private const uint GroupId = 9302;
    private const UnitAttribute Line = UnitAttribute.Str;

    private readonly Dictionary<FieldInfo, object> _savedEnchantData = [];
    private SingletonScope<ItemManager> _itemScope;
    private SingletonScope<BuffGameData> _buffDataScope;
    private SingletonScope<SkillManager> _skillScope;

    [Before(Test)]
    public void Setup()
    {
        // The line gives 5 Strength at grade 2 and 9 at grade 3.
        Seed("_rndAttrGroups", new Dictionary<uint, List<ItemRndAttrUnitModifierGroup>>
        {
            [GroupSetId] = [new()
            {
                Id = GroupId, GroupSetId = GroupSetId, Weight = 1,
                UnitAttributeId = (short)Line, UnitModifierTypeId = (byte)UnitModifierType.Value
            }]
        });
        Seed("_rndAttrModifiers", new Dictionary<uint, List<ItemRndAttrUnitModifierRange>>
        {
            [GroupId] =
            [
                new() { GroupId = GroupId, GradeId = 2, Min = 5, Max = 5 },
                new() { GroupId = GroupId, GradeId = 3, Min = 9, Max = 9 }
            ]
        });

        var items = new ItemManager(Mock.Of<ISkillManager>().Object, Mock.Of<IItemIdManager>().Object,
            Mock.Of<IContainerIdManager>().Object, Mock.Of<ILocalizationManager>().Object,
            Mock.Of<ITaskManager>().Object, Mock.Of<IWorldManager>().Object);
        SetField(items, "_itemUnitModifiers", new Dictionary<uint, List<BonusTemplate>>());
        _itemScope = new SingletonScope<ItemManager>(items);

        var buffData = new BuffGameData();
        SetField(buffData, "_buffModifiers", new Dictionary<uint, List<BuffModifier>>());
        SetField(buffData, "_itemModifiers", new Dictionary<uint, List<BuffModifier>>());
        SetField(buffData, "_gradeModifiers", new Dictionary<uint, List<BuffModifier>>());
        _buffDataScope = new SingletonScope<BuffGameData>(buffData);
        _skillScope = new SingletonScope<SkillManager>(TestManagers.CreateSkillManager());
    }

    [After(Test)]
    public void Teardown()
    {
        _skillScope?.Dispose();
        _buffDataScope?.Dispose();
        _itemScope?.Dispose();
        foreach (var (field, value) in _savedEnchantData)
            field.SetValue(ItemEnchantGameData.Instance, value);
        _savedEnchantData.Clear();
    }

    [Test]
    public async Task AWornPiece_ItsGradeBonusesFollowTheNewGrade()
    {
        var (character, piece) = Wearing(SlotType.Equipment);
        // The total the login makes for what the character wears.
        character.UpdateGearBonuses(null, null);
        await Assert.That(character.CalculateWithBonuses(0, Line)).IsEqualTo(5d);

        // The regrade's success writes the grade in place.
        piece.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, piece);

        await Assert.That(character.CalculateWithBonuses(0, Line)).IsEqualTo(9d);
    }

    [Test]
    public async Task AWornPiece_TheCachedGearScoreIsClearedToo()
    {
        var (character, piece) = Wearing(SlotType.Equipment);
        GearScoreCache.SetValue(character, 1234);

        piece.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, piece);

        await Assert.That(GearScoreCache.GetValue(character)).IsNull();
    }

    [Test]
    public async Task APieceInTheBag_IsInNeitherSum()
    {
        var (character, piece) = Wearing(SlotType.Inventory);
        GearScoreCache.SetValue(character, 1234);

        piece.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, piece);

        await Assert.That(character.CalculateWithBonuses(0, Line)).IsEqualTo(0d);
        await Assert.That(GearScoreCache.GetValue(character)).IsEqualTo((object)1234);
    }

    private static readonly FieldInfo GearScoreCache = typeof(Character)
        .GetField("_gearScoreCache", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>A character holding a grade 2 piece with the line, worn or in the bag.</summary>
    private static (Character Character, EquipItem Piece) Wearing(SlotType where)
    {
        var character = new TestCharacter { Id = 9310, ObjId = 9310, Name = "Regrader" };
        var inventory = DetachedInventory.Create(character);
        var container = where == SlotType.Equipment ? inventory.Equipment : inventory.Bag;
        var piece = new EquipItem(9320, new EquipItemTemplate { Id = 9330 }, 1)
        {
            Grade = 2, SlotType = where, Slot = 0, OwnerId = character.Id, _holdingContainer = container
        };
        piece.RndAttrGroupIds[0] = GroupId;
        container.Items.Add(piece);
        return (character, piece);
    }

    private void Seed(string name, object value)
    {
        var field = typeof(ItemEnchantGameData).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
        _savedEnchantData.TryAdd(field, field.GetValue(ItemEnchantGameData.Instance));
        field.SetValue(ItemEnchantGameData.Instance, value);
    }

    private static void SetField(object owner, string name, object value) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    /// <summary>A character whose maximum health and mana need no formulas (re-totalling reads them).</summary>
    private sealed class TestCharacter() : Character(new UnitCustomModelParams())
    {
        public override int MaxHp => 1000;
        public override int MaxMp => 500;
    }
}
