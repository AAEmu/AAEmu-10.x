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
    /// <summary>Compact <c>doodad_groups</c> "제작대-특산품" — specialty craft stations.</summary>
    SpecialtyWorkbench = 46,
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
    FishSale = 71,
    /// <summary>Compact <c>doodad_groups</c> "제작대 - 존 전용" — zone craft stations.</summary>
    ZoneWorkbench = 86,
    /// <summary>
    /// Compact <c>doodad_groups</c> "원대륙 정화 컨텐츠 지역 중심 표시" — Auroria purification
    /// area markers (most live in <c>doodad.g</c>; Sungold/Exeloch are json-only).
    /// </summary>
    PurificationArea = 101,
}
