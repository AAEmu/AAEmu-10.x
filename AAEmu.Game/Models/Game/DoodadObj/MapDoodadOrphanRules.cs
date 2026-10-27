using AAEmu.Game.Models.Game.DoodadObj.Static;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// Whether a <c>doodad_spawns.json</c> row that has <b>no</b> level-pack twin may still be
/// salvaged after LevelPack plant. Json is not map authority — only a filler for templates
/// the cells never listed (sale stands, dig-treasure markers, fish schools, resources, …).
/// </summary>
public static class MapDoodadOrphanRules
{
    /// <summary>
    /// Dig-treasure markers live only in json (groups 55–59); TreasureMap reads their spawners.
    /// </summary>
    public static bool IsDigTreasureGroup(uint groupId) =>
        groupId is >= (uint)DoodadGroupId.DigTreasureNormal and <= (uint)DoodadGroupId.DigTreasureHeroic;

    public static bool IsResourceGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.Deforestation
            or (uint)DoodadGroupId.Picking
            or (uint)DoodadGroupId.Mining
            or (uint)DoodadGroupId.Livestock
            or (uint)DoodadGroupId.Agriculture
            or (uint)DoodadGroupId.Excavation
            or (uint)DoodadGroupId.MarineAgriculture
            or (uint)DoodadGroupId.SportFishing;

    public static bool IsSaleGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.Sale or (uint)DoodadGroupId.FishSale;

    public static bool IsHousingGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.HousingDoorWindow or (uint)DoodadGroupId.HousingFurniture;

    /// <summary>
    /// Classify one json-only row. <paramref name="templateInLevelPack"/> means any .g cell
    /// already carries this template id somewhere in the world — then .g owns the template and
    /// json must not invent extra stands (worldgate twins, duplicate resources, …).
    /// <paramref name="isHousingBoundTemplate"/> is true when any house lists the template in
    /// <c>housing_binding_doodads</c> (plaza workbench, doors) — house Create owns those.
    /// </summary>
    public static MapDoodadPlantDecision Decide(
        bool templateExists,
        bool templateInLevelPack,
        bool standOccupied,
        uint groupId,
        bool hasEvidenceLoot,
        bool hasResidentTownhall,
        bool hasQuestFunc,
        bool hasCraftPack = false,
        bool hasNaviMailbox = false,
        bool isHousingBoundTemplate = false,
        bool hasSalvageGimmickFakeUse = false,
        bool hasPurchase = false,
        bool hasStoreUi = false,
        bool hasTransferTelescope = false,
        bool hasFlavorContainer = false,
        bool hasWorldInteract = false)
    {
        if (!templateExists)
            return MapDoodadPlantDecision.SkipUnknownTemplate;
        if (templateInLevelPack)
            return MapDoodadPlantDecision.SkipJsonOrphanCoveredByLevelPack;
        if (standOccupied)
            return MapDoodadPlantDecision.SkipStandOccupied;
        if (groupId == (uint)DoodadGroupId.Unused)
            return MapDoodadPlantDecision.SkipUnusedGroup;
        if (IsHousingGroup(groupId) || isHousingBoundTemplate)
            return MapDoodadPlantDecision.SkipHousingOrphan;
        if (hasEvidenceLoot)
            return MapDoodadPlantDecision.SkipRuntimeEvidence;

        if (IsDigTreasureGroup(groupId) || IsResourceGroup(groupId) || IsSaleGroup(groupId))
            return MapDoodadPlantDecision.Plant;

        // Interaction (24): plant permanent world systems + breakable flavor (barrels /
        // crates with FakeUse + RatioRespawn — Freedich crate pit, sea wrecks via Use/…).
        // Maritime weather volumes are WeatherManager, not orphan salvage. Leave SkipRandom
        // for attachment-only seats and empty cosmetic/no-func props.
        if (groupId == (uint)DoodadGroupId.Interaction)
        {
            if (hasResidentTownhall || hasQuestFunc || hasCraftPack || hasNaviMailbox ||
                hasSalvageGimmickFakeUse || hasPurchase || hasStoreUi ||
                hasTransferTelescope || hasFlavorContainer || hasWorldInteract)
                return MapDoodadPlantDecision.Plant;
            return MapDoodadPlantDecision.SkipRandomPlacement;
        }

        // Quest / skill / level / portal / purification json-only leftovers.
        if (groupId is (uint)DoodadGroupId.Quest
            or (uint)DoodadGroupId.SkillVolume
            or (uint)DoodadGroupId.Level
            or (uint)DoodadGroupId.PortalWorldgate
            or (uint)DoodadGroupId.PortalMirage
            or (uint)DoodadGroupId.PortalInstance
            or (uint)DoodadGroupId.PurificationArea)
            return MapDoodadPlantDecision.Plant;

        // Any other group that still carries DoodadFuncQuest (hunting-ground signposts /
        // Waterlogged Corpse, etc.) — content named the quest accept, so salvage the stand.
        if (hasQuestFunc)
            return MapDoodadPlantDecision.Plant;

        // Permanent craft stations crafts.req_doodad_id needs (Golden Ruins / Whalesong /
        // Hasla / Mirage salvage / Thunderwing …). Housing-bound plaza benches already returned.
        if (hasCraftPack)
            return MapDoodadPlantDecision.Plant;

        // Navi mail owl — same role as town mailboxes already in .g.
        if (hasNaviMailbox)
            return MapDoodadPlantDecision.Plant;

        return MapDoodadPlantDecision.SkipJsonOrphanUnhandled;
    }
}
