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

    /// <summary>
    /// Gather / farm catalog groups. Json leftovers that still Plant are mostly specialty
    /// ripen shelves (Livestock) and marine crops — trees/herbs/mines live in .g or
    /// <c>item_spawn_doodads</c> (already skipped). Dominion farm props are housing-bound.
    /// </summary>
    public static bool IsResourceGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.Deforestation
            or (uint)DoodadGroupId.Picking
            or (uint)DoodadGroupId.Mining
            or (uint)DoodadGroupId.Livestock
            or (uint)DoodadGroupId.Agriculture
            or (uint)DoodadGroupId.Excavation
            or (uint)DoodadGroupId.MarineAgriculture
            or (uint)DoodadGroupId.SportFishing;

    /// <summary>
    /// Compact group 26 is the housing-shop catalog (Mirage showroom fences / chairs /
    /// empty display props), not only merchant stands. Prefer <see cref="IsFishSaleGroup"/>
    /// or sale func signals — do not blanket-plant group 26.
    /// </summary>
    public static bool IsSaleCatalogGroup(uint groupId) =>
        groupId == (uint)DoodadGroupId.Sale;

    public static bool IsFishSaleGroup(uint groupId) =>
        groupId == (uint)DoodadGroupId.FishSale;

    public static bool IsHousingGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.HousingDoorWindow
            or (uint)DoodadGroupId.HousingFurniture
            or (uint)DoodadGroupId.FishFurniture
            or (uint)DoodadGroupId.RelicFurniture;

    /// <summary>
    /// Compact "Test Doodad" / "Trash Doodad" — QA effect statues, null-skill Use, editor
    /// water volumes, empty trap/onion shells. Quest-bearing Test rows (Waterlogged Corpse)
    /// still Plant via <c>hasQuestFunc</c> before this gate.
    /// </summary>
    public static bool IsDevTestOrphanGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.TestDoodad or (uint)DoodadGroupId.TrashDoodad;

    /// <summary>
    /// Nation Binding tomes and dominion statues — other systems own Create.
    /// </summary>
    public static bool IsSystemOwnedOrphanGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.BindingTome or (uint)DoodadGroupId.DominionStatue;

    /// <summary>
    /// Non-mgmt FakeUse salvage that shares a model with a <c>mgmt_spawn</c> twin — the twin
    /// owns the candidate pool and percent cycle (retail sunken chests). Denser json-only
    /// copies must not plant always-on.
    /// </summary>
    public static bool IsSupersededByMgmtSalvageTwin(
        bool hasSalvageGimmickFakeUse,
        bool mgmtSpawn,
        bool sameModelHasMgmtSpawn) =>
        hasSalvageGimmickFakeUse && !mgmtSpawn && sameModelHasMgmtSpawn;

    /// <summary>
    /// Classify one json-only row. <paramref name="templateInLevelPack"/> means any .g cell
    /// already carries this template id somewhere in the world — then .g owns the template and
    /// json must not invent extra stands (worldgate twins, duplicate resources, …).
    /// <paramref name="isHousingBoundTemplate"/> is true when any house lists the template in
    /// <c>housing_binding_doodads</c> (plaza workbench, doors) — house Create owns those.
    /// <paramref name="isItemSpawnedTemplate"/> is true when <c>item_spawn_doodads</c> lists
    /// the template (feast tables, traveler campfire, …) — item use owns Create, not json.
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
        bool hasWorldInteract = false,
        bool isItemSpawnedTemplate = false,
        bool mgmtSpawn = false,
        bool sameModelHasMgmtSpawn = false,
        bool hasBuyFish = false,
        bool hasServiceUi = false)
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
        // Feast tables / campfires / other item-planted props — SummonDoodad on use, not boot.
        if (isItemSpawnedTemplate)
            return MapDoodadPlantDecision.SkipJsonOrphanUnhandled;
        // Nation Binding / dominion statues — other systems own Create (even with Use).
        if (IsSystemOwnedOrphanGroup(groupId))
            return MapDoodadPlantDecision.SkipJsonOrphanUnhandled;
        // Denser non-mgmt sunken-chest pool — level-pack mgmt twin owns activation.
        if (IsSupersededByMgmtSalvageTwin(hasSalvageGimmickFakeUse, mgmtSpawn, sameModelHasMgmtSpawn))
            return MapDoodadPlantDecision.SkipMgmtSalvageTwin;

        if (IsDigTreasureGroup(groupId) || IsResourceGroup(groupId))
            return MapDoodadPlantDecision.Plant;

        // Fish-sale stands (BuyFish / ConvertFish) — real merchant pads, often json-only.
        if (IsFishSaleGroup(groupId) &&
            (hasBuyFish || hasPurchase || hasStoreUi || hasWorldInteract || hasCraftPack ||
             hasServiceUi))
            return MapDoodadPlantDecision.Plant;

        // Sale catalog (26): Mirage housing showroom. Plant buyable / service / craft /
        // Use props; SkipRandom for Attachment seats and empty display furniture (same
        // bar as Interaction cosmetics).
        if (IsSaleCatalogGroup(groupId))
        {
            if (hasPurchase || hasStoreUi || hasBuyFish || hasServiceUi || hasCraftPack ||
                hasWorldInteract || hasQuestFunc || hasNaviMailbox)
                return MapDoodadPlantDecision.Plant;
            return MapDoodadPlantDecision.SkipRandomPlacement;
        }

        // Interaction (24): plant permanent world systems + breakable flavor (barrels /
        // crates with FakeUse + RatioRespawn — Freedich crate pit, sea wrecks via Use/…)
        // and FakeUse that spawns a gimmick/NPC (Ancestor's Altar; mgmt sunken chests live
        // in level-pack — non-mgmt same-model densification is SkipMgmtSalvageTwin above).
        // Maritime weather volumes are WeatherManager, not orphan salvage. Leave SkipRandom
        // for attachment-only seats and empty cosmetic/no-func props.
        if (groupId == (uint)DoodadGroupId.Interaction)
        {
            if (hasResidentTownhall || hasQuestFunc || hasCraftPack || hasNaviMailbox ||
                hasSalvageGimmickFakeUse || hasPurchase || hasStoreUi ||
                hasTransferTelescope || hasFlavorContainer || hasWorldInteract ||
                hasBuyFish || hasServiceUi)
                return MapDoodadPlantDecision.Plant;
            return MapDoodadPlantDecision.SkipRandomPlacement;
        }

        // Quest / skill / level / portal / purification json-only leftovers (trash piles,
        // library boards, worldgates, Mirage exits, skill volumes). Nearly all carry funcs;
        // empty rows are rare noise — still Plant by group so portals/quest accept stay up.
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

        // Fish stand / bank / AH UI outside the Sale groups (rare catalog mis-group).
        if (hasBuyFish || hasServiceUi)
            return MapDoodadPlantDecision.Plant;

        // QA / Test / Trash leftovers (sky-Z returnpoint statues, TEST water volumes, empty
        // trap doors). Seal-stone Test twin 4614 shares a model with Level 14212 already in
        // .g and no quest_act targets it — leave off. Quest Test rows Planted above.
        if (IsDevTestOrphanGroup(groupId))
            return MapDoodadPlantDecision.SkipJsonOrphanUnhandled;

        // Residual SkipUnhandled: empty door shells and similar no-signal noise.
        return MapDoodadPlantDecision.SkipJsonOrphanUnhandled;
    }
}
