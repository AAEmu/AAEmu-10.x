using System.Reflection;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Buffs;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils;
using Microsoft.Data.Sqlite;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// A regrade moves a piece's grade in place. On a worn piece the bonuses that follow the grade used to stay at the
/// old grade until a re-equip or relog; the regrade now re-totals them, as synthesis and awakening do. Seen through a
/// synthesis line whose strength is authored per grade, and through per-grade item buffs shaped like the eleven
/// pieces whose grade buffs are untagged and independent (41026 Cold Flames Hood, 39724 Prime Stealthfang Spear).
/// </summary>
[NotInParallel]
public sealed class GradeEnchantWornPieceTests
{
    private const uint GroupSetId = 9301;
    private const uint GroupId = 9302;
    private const UnitAttribute Line = UnitAttribute.Str;

    // The hood: its own buff (41026's 12733) and a buff per grade (18243, 18244), none tagged 156.
    private const uint HoodTemplateId = 9340;
    private const uint HoodOwnBuffId = 9350;
    private const uint HoodGrade2BuffId = 9351;
    private const uint HoodGrade3BuffId = 9352;

    // The spear: no row at grade 2, so its own buff (39724's 16763) stands in; a row at grade 3. Both stack to 5.
    private const uint SpearTemplateId = 9341;
    private const uint SpearOwnBuffId = 9360;
    private const uint SpearGrade3BuffId = 9361;

    // Something tagged 156, so the re-total's own clean-up has a tag to read (the armour grade buffs' tag has none).
    private const uint TaggedEquipmentBuffId = 9370;

    private readonly Dictionary<FieldInfo, object> _savedEnchantData = [];
    private SingletonScope<ItemManager> _itemScope;
    private SingletonScope<BuffGameData> _buffDataScope;
    private SingletonScope<SkillManager> _skillScope;
    private SingletonScope<ItemGameData> _itemDataScope;
    private SingletonScope<TaskManager> _taskScope;
    private SingletonScope<EffectTaskManager> _effectTaskScope;
    private SqliteConnection _itemGradeBuffs;

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
        SetField(buffData, "_buffTolerances", new Dictionary<uint, BuffTolerance>());
        SetField(buffData, "_buffTolerancesById", new Dictionary<uint, BuffTolerance>());
        _buffDataScope = new SingletonScope<BuffGameData>(buffData);

        // The item buffs are permanent (duration 0) and independent (stack rule 6), as all eleven pieces' are.
        var skills = TestManagers.CreateSkillManager();
        SetField(skills, "_buffs", new Dictionary<uint, BuffTemplate>
        {
            [HoodOwnBuffId] = ItemBuff(HoodOwnBuffId, 1),
            [HoodGrade2BuffId] = ItemBuff(HoodGrade2BuffId, 1),
            [HoodGrade3BuffId] = ItemBuff(HoodGrade3BuffId, 1),
            [SpearOwnBuffId] = ItemBuff(SpearOwnBuffId, 5),
            [SpearGrade3BuffId] = ItemBuff(SpearGrade3BuffId, 5),
            [TaggedEquipmentBuffId] = ItemBuff(TaggedEquipmentBuffId, 1)
        });
        SetField(skills, "_taggedBuffs", new Dictionary<uint, List<uint>>
        {
            [(uint)BuffConstants.EquipmentBuffTag] = [TaggedEquipmentBuffId],
            [(uint)BuffConstants.ArmorBuffTag] = []
        });
        _skillScope = new SingletonScope<SkillManager>(skills);

        var taskManager = new TaskManager(Mock.Of<ITickManager>().Object);
        _taskScope = new SingletonScope<TaskManager>(taskManager);
        _effectTaskScope = new SingletonScope<EffectTaskManager>(new EffectTaskManager(taskManager));

        _itemGradeBuffs = new SqliteConnection("Data Source=:memory:");
        _itemGradeBuffs.Open();
        using (var command = _itemGradeBuffs.CreateCommand())
        {
            command.CommandText = $"""
                CREATE TABLE item_grade_buffs (item_id INTEGER, item_grade_id INTEGER, buff_id INTEGER, num_pieces INTEGER);
                INSERT INTO item_grade_buffs VALUES ({HoodTemplateId}, 2, {HoodGrade2BuffId}, 1);
                INSERT INTO item_grade_buffs VALUES ({HoodTemplateId}, 3, {HoodGrade3BuffId}, 1);
                INSERT INTO item_grade_buffs VALUES ({SpearTemplateId}, 3, {SpearGrade3BuffId}, 1);
                """;
            command.ExecuteNonQuery();
        }

        var itemData = new ItemGameData();
        itemData.Load(_itemGradeBuffs);
        _itemDataScope = new SingletonScope<ItemGameData>(itemData);
    }

    [After(Test)]
    public void Teardown()
    {
        _itemDataScope?.Dispose();
        _itemGradeBuffs?.Dispose();
        _effectTaskScope?.Dispose();
        _taskScope?.Dispose();
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

    [Test]
    public async Task AWornPiecesUntaggedGradeBuff_IsSwappedForTheNewGrades()
    {
        var character = NewCharacter();
        var hood = Wear(character, HoodTemplateId, HoodOwnBuffId, grade: 2, slot: 0);
        character.UpdateGearBonuses(null, null);
        var before = character.Buffs.CheckBuff(HoodGrade2BuffId);

        hood.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, hood);

        await Assert.That(before).IsTrue();
        await Assert.That(character.Buffs.CheckBuff(HoodGrade2BuffId)).IsFalse();
        await Assert.That(character.Buffs.GetBuffCountById(HoodGrade3BuffId)).IsEqualTo(1);
        await Assert.That(character.Buffs.CheckBuff(HoodOwnBuffId)).IsFalse();

        // Taking the hood off takes the current grade's buff, and nothing of an older grade is left behind.
        character.Equipment.Items.Remove(hood);
        character.UpdateGearBonuses(null, hood);

        await Assert.That(character.Buffs.CheckBuff(HoodGrade2BuffId)).IsFalse();
        await Assert.That(character.Buffs.CheckBuff(HoodGrade3BuffId)).IsFalse();
    }

    [Test]
    public async Task AGradeWithNoRow_ItsItemsOwnBuffGoesWhenARowTakesOver()
    {
        var character = NewCharacter();
        var spear = Wear(character, SpearTemplateId, SpearOwnBuffId, grade: 2, slot: 0);
        character.UpdateGearBonuses(null, null);
        var before = character.Buffs.CheckBuff(SpearOwnBuffId);

        spear.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, spear);

        await Assert.That(before).IsTrue();
        await Assert.That(character.Buffs.CheckBuff(SpearOwnBuffId)).IsFalse();
        await Assert.That(character.Buffs.GetBuffCountById(SpearGrade3BuffId)).IsEqualTo(1);
    }

    [Test]
    public async Task ARegradeOfAnotherPiece_DoesNotStackAWornPiecesBuff()
    {
        var character = NewCharacter();
        var hood = Wear(character, HoodTemplateId, HoodOwnBuffId, grade: 2, slot: 0);
        Wear(character, SpearTemplateId, SpearOwnBuffId, grade: 3, slot: 1);
        character.UpdateGearBonuses(null, null);

        hood.Grade = 3;
        GradeEnchant.RefreshWornPiece(character, hood);
        hood.Grade = 2;
        GradeEnchant.RefreshWornPiece(character, hood);

        var spearBuff = character.Buffs.GetEffectFromBuffId(SpearGrade3BuffId);
        await Assert.That(spearBuff).IsNotNull();
        await Assert.That(spearBuff!.Stack).IsEqualTo(1);
        await Assert.That(character.Buffs.GetBuffCountById(SpearGrade3BuffId)).IsEqualTo(1);
        await Assert.That(character.Buffs.GetBuffCountById(HoodGrade2BuffId)).IsEqualTo(1);
        await Assert.That(character.Buffs.CheckBuff(HoodGrade3BuffId)).IsFalse();
    }

    private static readonly FieldInfo GearScoreCache = typeof(Character)
        .GetField("_gearScoreCache", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>A character holding a grade 2 piece with the line, worn or in the bag.</summary>
    private static (Character Character, EquipItem Piece) Wearing(SlotType where)
    {
        var character = NewCharacter();
        var container = where == SlotType.Equipment ? character.Inventory.Equipment : character.Inventory.Bag;
        var piece = new EquipItem(9320, new EquipItemTemplate { Id = 9330 }, 1)
        {
            Grade = 2, SlotType = where, Slot = 0, OwnerId = character.Id, _holdingContainer = container
        };
        piece.RndAttrGroupIds[0] = GroupId;
        container.Items.Add(piece);
        return (character, piece);
    }

    private static Character NewCharacter()
    {
        var character = new TestCharacter { Id = 9310, ObjId = 9310, Name = "Regrader" };
        DetachedInventory.Create(character);
        return character;
    }

    /// <summary>A worn piece whose item row names <paramref name="ownBuffId"/>, as the eleven pieces' rows do.</summary>
    private static EquipItem Wear(Character character, uint templateId, uint ownBuffId, byte grade, byte slot)
    {
        var container = character.Inventory.Equipment;
        var piece = new EquipItem(templateId + 100, new EquipItemTemplate { Id = templateId, BuffId = ownBuffId, Level = 1 }, 1)
        {
            Grade = grade, SlotType = SlotType.Equipment, Slot = slot, OwnerId = character.Id, _holdingContainer = container
        };
        container.Items.Add(piece);
        return piece;
    }

    private static BuffTemplate ItemBuff(uint id, int maxStack) =>
        new() { Id = id, Duration = 0, Kind = BuffKind.Good, StackRule = BuffStackRule.Independent, MaxStack = maxStack };

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
