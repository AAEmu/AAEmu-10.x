using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Weather;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class MapDoodadPlantRulesTests
{
    [Test]
    public async Task Decide_UnscheduledInteraction_Plants()
    {
        // Whalesong curse mine shape: known template, group interaction, not scheduled.
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotScheduled,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Decide_InProgressSchedule_Plants()
    {
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.InProgress,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Decide_EndedOrNotStartedSchedule_Skipped()
    {
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.Ended,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipSchedule);
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotStarted,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipSchedule);
    }

    [Test]
    public async Task Decide_TowerIgnoreUnusedUnknown_Skipped()
    {
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: false,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotScheduled,
            groupId: 0)).IsEqualTo(MapDoodadPlantDecision.SkipUnknownTemplate);
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: true,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotScheduled,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipIgnoredPermanent);
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: true,
            schedule: MapDoodadSchedulePeriod.NotScheduled,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipTowerAlmighty);
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: false,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotScheduled,
            groupId: (uint)DoodadGroupId.Unused)).IsEqualTo(MapDoodadPlantDecision.SkipUnusedGroup);
    }

    [Test]
    public async Task Decide_IgnoredPermanent_OffWindowSchedule_ArmsCron()
    {
        // ignore_*doodad* + game_schedule_doodads: must SkipSchedule (arm), not SkipIgnored.
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: true,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.NotStarted,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipSchedule);
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: true,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.Ended,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.SkipSchedule);
    }

    [Test]
    public async Task Decide_IgnoredPermanent_InProgress_Plants()
    {
        await Assert.That(MapDoodadPlantRules.Decide(
            templateExists: true,
            ignoredPermanent: true,
            towerAlmighty: false,
            schedule: MapDoodadSchedulePeriod.InProgress,
            groupId: 24)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Source_LevelPackFill_LoadsJsonAndPlants()
    {
        await Assert.That(MapDoodadPlantRules.ShouldPlantFromLevelPack(MapDoodadSourceKind.LevelPackFill))
            .IsTrue();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.LevelPackFill, "doodad_spawns.json")).IsTrue();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.LevelPackFill, "doodad_spawns_overlay.json")).IsTrue();
    }

    [Test]
    public async Task Source_LevelPack_OnlyOverlayJson()
    {
        await Assert.That(MapDoodadPlantRules.ShouldPlantFromLevelPack(MapDoodadSourceKind.LevelPack))
            .IsTrue();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.LevelPack, "doodad_spawns.json")).IsFalse();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.LevelPack, "doodad_spawns_overlay.json")).IsTrue();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.LevelPack, "doodad_spawns_overlay_instance.json")).IsTrue();
    }

    [Test]
    public async Task Source_Json_NoLevelPackPlant()
    {
        await Assert.That(MapDoodadPlantRules.ShouldPlantFromLevelPack(MapDoodadSourceKind.Json))
            .IsFalse();
        await Assert.That(MapDoodadPlantRules.ShouldPlantFromLevelPack(null)).IsFalse();
        await Assert.That(MapDoodadPlantRules.ShouldLoadJsonSpawnFile(
            MapDoodadSourceKind.Json, "doodad_spawns.json")).IsTrue();
    }

    [Test]
    public async Task Plan_SkipsSameTemplateWithinTwoMetres()
    {
        var catalog = new[]
        {
            new MapDoodadPlantRules.Placement(12229, 100, 200, 10, 0, 1),
            new MapDoodadPlantRules.Placement(12229, 101.5f, 200, 10, 0, 1),
            new MapDoodadPlantRules.Placement(12229, 110, 200, 10, 0, 2.5f),
        };
        var existing = new[] { new MapDoodadPlantRules.Existing(12229, 100.5f, 200, 10) };
        var planned = MapDoodadPlantRules.Plan(catalog, existing);
        await Assert.That(planned.Count).IsEqualTo(1);
        await Assert.That(planned[0].X).IsEqualTo(110f).Within(0.01f);
        await Assert.That(planned[0].Scale).IsEqualTo(2.5f).Within(0.01f);
    }

    [Test]
    public async Task Plan_CrossTemplate_OccupiesStand_WorldgateTwin()
    {
        // Marianople: .g 10521 closed gate already planted; json 8033 always-open must not stack.
        const uint portal = (uint)DoodadGroupId.PortalWorldgate;
        var catalog = new[]
        {
            new MapDoodadPlantRules.Placement(8033, 11134.49f, 12046.35f, 145.74f, 0, 1, portal),
        };
        var existing = new[]
        {
            new MapDoodadPlantRules.Existing(10521, 11134.482f, 12046.345f, 145.74f, portal),
        };
        var planned = MapDoodadPlantRules.Plan(catalog, existing, crossTemplateOccupancy: true);
        await Assert.That(planned.Count).IsEqualTo(0);

        var sameTemplateOnly = MapDoodadPlantRules.Plan(catalog, existing, crossTemplateOccupancy: false);
        await Assert.That(sameTemplateOnly.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Plan_LayeredInteraction_AllowsStatueAndVortexOnSameXyz()
    {
        // Sea of Graves / Whirlpool: 8351 vortex + 8532 Ezi statue share XYZ; both are
        // Interaction (24). Event SkillHit toggles vortex whirlpool↔invisible — not a portal twin.
        const uint interaction = (uint)DoodadGroupId.Interaction;
        var catalog = new[]
        {
            new MapDoodadPlantRules.Placement(8351, 20106.36f, 21027.22f, 102f, 0, 1, interaction),
            new MapDoodadPlantRules.Placement(8532, 20106.36f, 21027.22f, 102f, 0, 1, interaction),
        };
        var planned = MapDoodadPlantRules.Plan(catalog, existing: [], crossTemplateOccupancy: true);
        await Assert.That(planned.Count).IsEqualTo(2);
        await Assert.That(planned.Any(p => p.TemplateId == 8351)).IsTrue();
        await Assert.That(planned.Any(p => p.TemplateId == 8532)).IsTrue();
    }

    [Test]
    public async Task Plan_PortalOrphan_AllowedBesideInvisibleEtherealHaven()
    {
        // Ethereal Haven 16739 starts a://invalid (invisible wait). Golden Plains battle
        // closed 8583 is a separate json twin on the same pad — must plant; the pad looks empty.
        const uint portal = (uint)DoodadGroupId.PortalWorldgate;
        var catalog = new[]
        {
            new MapDoodadPlantRules.Placement(
                8583, 11183.46f, 12069.52f, 145.46f, 0, 1, portal, InvisibleStart: false),
        };
        var existing = new[]
        {
            new MapDoodadPlantRules.Existing(
                16739, 11183.553f, 12068.783f, 145.441f, portal, InvisibleStart: true),
        };
        var planned = MapDoodadPlantRules.Plan(catalog, existing, crossTemplateOccupancy: true);
        await Assert.That(planned.Count).IsEqualTo(1);
        await Assert.That(planned[0].TemplateId).IsEqualTo(8583u);
    }

    [Test]
    public async Task Plan_TwoVisiblePortals_StillOccupyStand()
    {
        const uint portal = (uint)DoodadGroupId.PortalWorldgate;
        var catalog = new[]
        {
            new MapDoodadPlantRules.Placement(8583, 11183.46f, 12069.52f, 145.46f, 0, 1, portal),
        };
        var existing = new[]
        {
            new MapDoodadPlantRules.Existing(10521, 11183.55f, 12068.78f, 145.44f, portal),
        };
        var planned = MapDoodadPlantRules.Plan(catalog, existing, crossTemplateOccupancy: true);
        await Assert.That(planned.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IsInvisibleStartModel_InvalidAndEnvInvisible()
    {
        await Assert.That(MapDoodadPlantRules.IsInvisibleStartModel("a://invalid")).IsTrue();
        await Assert.That(MapDoodadPlantRules.IsInvisibleStartModel("prefab://env.invisible")).IsTrue();
        await Assert.That(MapDoodadPlantRules.IsInvisibleStartModel("prefab://continent_portal_red")).IsFalse();
        await Assert.That(MapDoodadPlantRules.IsInvisibleStartModel(null)).IsFalse();
        await Assert.That(MapDoodadPlantRules.IsInvisibleStartModel("")).IsFalse();
    }

    [Test]
    public async Task Orphan_DigTreasureAndSale_Plant()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.DigTreasureNormal,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.Plant);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Sale,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.Plant);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.SportFishing,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Orphan_InteractionSystems_Plant_EmptyCosmeticsSkipped()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasSalvageGimmickFakeUse: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasPurchase: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasTransferTelescope: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        // Freedich crate pit / seabed barrels: FakeUse + RatioRespawn flavor.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasFlavorContainer: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        // Sea wrecks / spyglass pads: Use / SkillHit / OpenPaper / Devote.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasWorldInteract: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        // Attachment seats / empty cosmetics: Interaction with no salvage signal.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipRandomPlacement);
    }

    [Test]
    public async Task MaritimeWeatherRules_VolumeModels()
    {
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(
            "prefab://prefabs/interaction.xml/env.cloud")).IsTrue();
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(
            "prefab://prefabs/interaction.xml/env.rainstorm")).IsTrue();
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(
            "prefab://prefabs/interaction.xml/env.vortex")).IsTrue();
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(
            "prefab://prefabs/interaction.xml/env.raincloud")).IsTrue();
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(
            "prefab://prefabs/interaction.xml/env.invisible")).IsFalse();
        await Assert.That(MaritimeWeatherRules.IsVolumeModel(null)).IsFalse();
        await Assert.That(MapDoodadPlantRules.IsMaritimeWeatherVolumeModel(
            "prefab://prefabs/interaction.xml/env.cloud")).IsTrue();
    }

    [Test]
    public async Task Orphan_ResidentBoard_Plants()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: false,
            hasResidentTownhall: true,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Orphan_EvidenceAndCoveredAndHousing_Skipped()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.Interaction,
            hasEvidenceLoot: true,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipRuntimeEvidence);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: true,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.PortalWorldgate,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipJsonOrphanCoveredByLevelPack);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.HousingFurniture,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipHousingOrphan);
    }

    [Test]
    public async Task Orphan_QuestFunc_OutsideKnownGroups_StillPlants()
    {
        // Hunting-ground signposts (compact group 54) and Waterlogged Corpse (group 34) are
        // json-only and carry DoodadFuncQuest — salvage by func, not by group id.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: 54,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: 34,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: true)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Orphan_HousingWithQuestFunc_StillSkipped()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.HousingFurniture,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: true)).IsEqualTo(MapDoodadPlantDecision.SkipHousingOrphan);
    }

    [Test]
    public async Task Orphan_CraftPackWorkbench_Plants_UnlessHousingBound()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.SpecialtyWorkbench,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasCraftPack: true)).IsEqualTo(MapDoodadPlantDecision.Plant);

        // Plaza specialty workbench — housing_binding_doodads owns it.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.SpecialtyWorkbench,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasCraftPack: true,
            isHousingBoundTemplate: true)).IsEqualTo(MapDoodadPlantDecision.SkipHousingOrphan);
    }

    [Test]
    public async Task Orphan_PurificationAreaAndNaviMailbox_Plant()
    {
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: (uint)DoodadGroupId.PurificationArea,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.Plant);

        // Any-Post Owl is collide-check group 72 with DoodadFuncNaviOpenMailbox.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: 72,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false,
            hasNaviMailbox: true)).IsEqualTo(MapDoodadPlantDecision.Plant);
    }

    [Test]
    public async Task Orphan_NationTomeAndTerritoryStatue_StillUnhandled()
    {
        // Player-nation Binding tome / dominion statues stay system-owned.
        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: 53,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipJsonOrphanUnhandled);

        await Assert.That(MapDoodadOrphanRules.Decide(
            templateExists: true,
            templateInLevelPack: false,
            standOccupied: false,
            groupId: 80,
            hasEvidenceLoot: false,
            hasResidentTownhall: false,
            hasQuestFunc: false)).IsEqualTo(MapDoodadPlantDecision.SkipJsonOrphanUnhandled);
    }
}
