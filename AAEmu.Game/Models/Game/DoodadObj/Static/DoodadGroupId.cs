namespace AAEmu.Game.Models.Game.DoodadObj.Static;

/// <summary>
/// Values from the content database's <c>doodad_groups.id</c> column.
/// Vocation badge groups are the gather set; <see cref="Unused"/> is the shipped "미사용" bucket
/// that map plant must not author from level files.
/// </summary>
public enum DoodadGroupId : uint
{
    Deforestation = 2,
    Picking = 3,
    Mining = 4,
    Livestock = 5,
    Quest = 11,
    Agriculture = 12,
    SkillVolume = 13,
    Interaction = 24,
    Level = 25,
    Sale = 26,
    Excavation = 39,
    MarineAgriculture = 40,
    PortalWorldgate = 43,
    PortalMirage = 44,
    PortalInstance = 45,
    /// <summary>Compact <c>doodad_groups</c> "Test Doodad" — QA / editor props.</summary>
    TestDoodad = 34,
    /// <summary>Compact <c>doodad_groups</c> "Trash Doodad" — empty / discarded shells.</summary>
    TrashDoodad = 35,
    /// <summary>Compact <c>doodad_groups</c> "door" — empty door shells / leftover gates.</summary>
    Door = 36,
    /// <summary>Compact <c>doodad_groups</c> "제작대-기계" — mechanical craft stations.</summary>
    MechanicalWorkbench = 16,
    /// <summary>Compact <c>doodad_groups</c> "제작대-특산품" — specialty craft stations.</summary>
    SpecialtyWorkbench = 46,
    /// <summary>Compact <c>doodad_groups</c> "기타-공간 기록서" — nation Binding / space recorders.</summary>
    BindingTome = 53,
    /// <summary>Compact <c>doodad_groups</c> "기타 - 퀘스트(사냥터 푯말)" — hunting-ground signposts.</summary>
    HuntingSignpost = 54,
    /// <summary>Compact <c>doodad_groups</c> "영지 석상" — dominion statues.</summary>
    DominionStatue = 80,
    /// <summary>Compact <c>doodad_groups</c> "collide check" — feast/collision props (often item-spawned).</summary>
    CollideCheck = 72,
    /// <summary>Compact <c>doodad_groups</c> "미사용" — never plant from <c>doodad.g</c>.</summary>
    Unused = 47,
    DigTreasureNormal = 55,
    DigTreasureGrand = 56,
    DigTreasureRare = 57,
    DigTreasureArcane = 58,
    DigTreasureHeroic = 59,
    SportFishing = 65,
    HousingDoorWindow = 68,
    HousingFurniture = 69,
    /// <summary>Compact <c>doodad_groups</c> "물고기 가구" — fish-theme housing furniture.</summary>
    FishFurniture = 70,
    FishSale = 71,
    /// <summary>Compact <c>doodad_groups</c> "일반 유물" — housing relic furniture.</summary>
    RelicFurniture = 98,
    /// <summary>Compact <c>doodad_groups</c> "제작대 - 존 전용" — zone craft stations.</summary>
    ZoneWorkbench = 86,
    /// <summary>
    /// Compact <c>doodad_groups</c> "원대륙 정화 컨텐츠 지역 중심 표시" — Auroria purification
    /// area markers (most live in <c>doodad.g</c>; Sungold/Exeloch are json-only).
    /// </summary>
    PurificationArea = 101,
}
