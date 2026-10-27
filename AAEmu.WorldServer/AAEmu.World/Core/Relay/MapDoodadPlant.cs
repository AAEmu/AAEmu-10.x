using AAEmu.Commons.IO;
using AAEmu.Commons.Utils;
using AAEmu.Game;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Weather;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Utils;

using NLog;

namespace AAEmu.World.Core.Relay;

/// <summary>
/// World-authors open-world map doodads from <c>cells/*/doodad.g</c> when
/// <c>World.MapDoodadSource</c> is a level-pack mode. Coordinates come from
/// <see cref="ZoneDoodadPlacementCatalog"/> only; classification is
/// <see cref="MapDoodadPlantRules"/>. LevelPack also salvages approved
/// json-only orphans (sale / dig treasure / fish schools / …) without loading
/// <c>doodad_spawns.json</c> as map authority.
/// </summary>
public static class MapDoodadPlant
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static bool Disabled =>
        Environment.GetEnvironmentVariable("AAEMU_DISABLE_MAP_DOODAD_PLANT") == "1" ||
        Environment.GetEnvironmentVariable("AAEMU_DISABLE_LEVEL_PACK_DOODADS") == "1";

    private static bool OrphanSalvageDisabled =>
        Environment.GetEnvironmentVariable("AAEMU_DISABLE_MAP_DOODAD_ORPHAN_SALVAGE") == "1";

    private static bool MaritimeWeatherVolumesDisabled =>
        Environment.GetEnvironmentVariable("AAEMU_DISABLE_MARITIME_WEATHER_VOLUMES") == "1";

    private static string Source =>
        AppConfiguration.Instance.World.MapDoodadSource ?? MapDoodadSourceKind.Json;

    public static void EnsureAllWorlds()
    {
        if (Disabled || !MapDoodadPlantRules.ShouldPlantFromLevelPack(Source))
            return;

        var worlds = WorldManager.Instance.GetWorlds();
        if (worlds == null)
            return;

        foreach (var world in worlds)
            Ensure(world);
    }

    /// <summary>
    /// WeatherManager-owned: plant json-only maritime weather FX volumes
    /// (<see cref="MaritimeWeatherRules"/>). Called from
    /// <see cref="WeatherManager.EnsureMaritimeVolumes"/> after orphan salvage so weather
    /// stands are not classified by <see cref="MapDoodadOrphanRules"/>.
    /// </summary>
    public static void EnsureMaritimeWeatherVolumesAllWorlds()
    {
        if (Disabled || MaritimeWeatherVolumesDisabled ||
            !MapDoodadPlantRules.ShouldPlantFromLevelPack(Source) ||
            !MapDoodadPlantRules.IsLevelPackOnly(Source))
            return;

        var worlds = WorldManager.Instance.GetWorlds();
        if (worlds == null)
            return;

        foreach (var world in worlds)
            PlantMaritimeWeatherVolumes(world);
    }

    public static void Ensure(WorldInstance world)
    {
        if (Disabled || world?.Template == null || !AppConfiguration.Instance.World.SpawnDoodads)
            return;
        if (!MapDoodadPlantRules.ShouldPlantFromLevelPack(Source))
            return;

        var catalog = ZoneDoodadPlacementCatalog.GetAll(world.Template.Name);
        PlantCatalog(world, catalog, copy: false);
        if (MapDoodadPlantRules.IsLevelPackOnly(Source) && !OrphanSalvageDisabled)
            SalvageJsonOrphans(world, catalog);
    }

    /// <summary>
    /// Dungeon copies take their permanent doodad set from their own cells. Same classification
    /// as overworld; rows already spawned at the same spot are skipped.
    /// </summary>
    public static void EnsureInstanceCopy(WorldInstance world)
    {
        if (Disabled || world?.Template == null || !AppConfiguration.Instance.World.SpawnDoodads)
            return;

        // Instance copies always need a full cell plant regardless of overworld MapDoodadSource:
        // json boot never reaches a copy created later. When source is Json, QuestTalkDoodads
        // still owns the copy path; when level-pack is on, this path replaces it.
        if (!MapDoodadPlantRules.ShouldPlantFromLevelPack(Source))
            return;

        PlantCatalog(world, ZoneDoodadPlacementCatalog.GetAll(world.Template.Name), copy: true);
    }

    private static void PlantCatalog(
        WorldInstance world,
        IReadOnlyList<ZoneDoodadPlacementCatalog.DoodadPlacement> catalog,
        bool copy)
    {
        var worldName = world.Template.Name;
        if (catalog.Count == 0)
        {
            Logger.Warn(
                "MapDoodadPlant{0} world={1} id={2}: no cell doodad rows",
                copy ? " copy" : "", worldName, world.Id);
            return;
        }

        var towerAlmighty = TowerDefGameData.Instance.GetDoodadAlmightyTargetIds();
        var places = new List<MapDoodadPlantRules.Placement>(catalog.Count);
        var scheduleArm = new List<MapDoodadPlantRules.Placement>();
        var skipUnknown = 0;
        var skipIgnored = 0;
        var skipTower = 0;
        var skipSchedule = 0;
        var skipUnused = 0;

        foreach (var p in catalog)
        {
            var exists = DoodadManager.Instance.Exist(p.TemplateId);
            var groupId = 0u;
            if (exists)
            {
                var template = DoodadManager.Instance.GetTemplate(p.TemplateId);
                groupId = template?.GroupId ?? 0;
            }

            var schedule = ToSchedulePeriod(
                GameScheduleManager.Instance.GetPeriodStatusDoodad((int)p.TemplateId));
            var decision = MapDoodadPlantRules.Decide(
                exists,
                p.IgnoredPermanent,
                towerAlmighty?.Contains(p.TemplateId) == true,
                schedule,
                groupId);

            var place = new MapDoodadPlantRules.Placement(
                p.TemplateId,
                p.X,
                p.Y,
                p.Z,
                p.YawDegrees,
                p.Scale,
                groupId,
                InvisibleStart: TemplateStartsInvisible(p.TemplateId));

            switch (decision)
            {
                case MapDoodadPlantDecision.Plant:
                    places.Add(place);
                    break;
                case MapDoodadPlantDecision.SkipSchedule:
                    skipSchedule++;
                    // Only NotStarted can wake via cron. Ended-only festival rows (stale windows)
                    // would Create then no-op in DoSpawn — skip arming those.
                    if (schedule == MapDoodadSchedulePeriod.NotStarted)
                        scheduleArm.Add(place);
                    break;
                case MapDoodadPlantDecision.SkipUnknownTemplate:
                    skipUnknown++;
                    break;
                case MapDoodadPlantDecision.SkipIgnoredPermanent:
                    skipIgnored++;
                    break;
                case MapDoodadPlantDecision.SkipTowerAlmighty:
                    skipTower++;
                    break;
                case MapDoodadPlantDecision.SkipUnusedGroup:
                    skipUnused++;
                    break;
            }
        }

        var existing = ListAllExisting(world);
        var cross = MapDoodadPlantRules.IsLevelPackOnly(Source) || copy;
        var planned = MapDoodadPlantRules.Plan(places, existing, crossTemplateOccupancy: cross);
        var dupSkipped = places.Count - planned.Count;
        var spawned = planned.Count == 0 ? 0 : SpawnPlanned(world, planned, registerSpawner: true);

        // NotStarted / Ended schedule rows: keep a spawner so cron can wake them when the window opens.
        var armed = 0;
        if (!copy && scheduleArm.Count > 0)
        {
            existing = ListAllExisting(world);
            var armPlan = MapDoodadPlantRules.Plan(scheduleArm, existing, crossTemplateOccupancy: cross);
            armed = ArmScheduleSpawners(world, armPlan);
        }

        Logger.Info(
            "MapDoodadPlant{0} world={1} id={2} .g={3} plantable={4} spawned={5} " +
            "dupSkipped={6} scheduleArmed={7} skipUnknown={8} skipIgnored={9} skipTower={10} " +
            "skipSchedule={11} skipUnused={12}",
            copy ? " copy" : "",
            worldName,
            world.Id,
            catalog.Count,
            places.Count,
            spawned,
            dupSkipped,
            armed,
            skipUnknown,
            skipIgnored,
            skipTower,
            skipSchedule,
            skipUnused);
    }

    /// <summary>
    /// LevelPack-only: read <c>doodad_spawns.json</c> as a salvage list for templates the cells
    /// never carried. Never loads that file through <see cref="SpawnManager"/> as authority.
    /// </summary>
    private static void SalvageJsonOrphans(
        WorldInstance world,
        IReadOnlyList<ZoneDoodadPlacementCatalog.DoodadPlacement> levelPackCatalog)
    {
        var worldName = world.Template.Name;
        var jsonPath = Path.Combine(FileManager.AppPath, "Data", "Worlds", worldName, "doodad_spawns.json");
        if (!File.Exists(jsonPath))
        {
            Logger.Info("MapDoodadOrphan world={0}: no doodad_spawns.json to salvage", worldName);
            return;
        }

        var contents = FileManager.GetFileContents(jsonPath);
        if (string.IsNullOrWhiteSpace(contents) ||
            !JsonHelper.TryDeserializeObject(contents, out List<DoodadSpawner> spawners, out _))
        {
            Logger.Warn("MapDoodadOrphan world={0}: failed to parse {1}", worldName, jsonPath);
            return;
        }

        var inLevelPack = new HashSet<uint>();
        foreach (var row in levelPackCatalog)
            inLevelPack.Add(row.TemplateId);

        var candidates = new List<MapDoodadPlantRules.Placement>();
        var skipCovered = 0;
        var skipOccupied = 0;
        var skipOther = 0;
        var existing = ListAllExisting(world);

        foreach (var spawner in spawners)
        {
            if (spawner?.Position == null || spawner.UnitId == 0)
                continue;

            var exists = DoodadManager.Instance.Exist(spawner.UnitId);
            var template = exists ? DoodadManager.Instance.GetTemplate(spawner.UnitId) : null;
            // WeatherManager plants maritime FX volumes — leave them out of orphan salvage.
            if (MaritimeWeatherRules.IsVolumeModel(template?.Model))
            {
                skipOther++;
                continue;
            }

            var groupId = template?.GroupId ?? 0;
            CollectFuncFlags(
                template,
                out var evidence,
                out var resident,
                out var quest,
                out var craftPack,
                out var naviMail,
                out var salvageGimmick,
                out var purchase,
                out var storeUi,
                out var transferTelescope,
                out var flavorContainer,
                out var worldInteract);
            var housingBound = HousingGameData.Instance.IsHousingBoundDoodadTemplate(spawner.UnitId);

            var place = new MapDoodadPlantRules.Placement(
                spawner.UnitId,
                spawner.Position.X,
                spawner.Position.Y,
                spawner.Position.Z,
                spawner.Position.Yaw,
                spawner.Scale > 0f ? spawner.Scale : 1f,
                groupId,
                InvisibleStart: TemplateStartsInvisible(spawner.UnitId));

            var occupied = MapDoodadPlantRules.IsStandOccupied(existing, place, crossTemplateOccupancy: true);
            var decision = MapDoodadOrphanRules.Decide(
                exists,
                inLevelPack.Contains(spawner.UnitId),
                occupied,
                groupId,
                evidence,
                resident,
                quest,
                craftPack,
                naviMail,
                housingBound,
                salvageGimmick,
                purchase,
                storeUi,
                transferTelescope,
                flavorContainer,
                worldInteract);

            switch (decision)
            {
                case MapDoodadPlantDecision.Plant:
                    candidates.Add(place);
                    break;
                case MapDoodadPlantDecision.SkipJsonOrphanCoveredByLevelPack:
                    skipCovered++;
                    break;
                case MapDoodadPlantDecision.SkipStandOccupied:
                    skipOccupied++;
                    break;
                default:
                    skipOther++;
                    break;
            }
        }

        var planned = MapDoodadPlantRules.Plan(candidates, existing, crossTemplateOccupancy: true);
        var spawned = planned.Count == 0 ? 0 : SpawnPlanned(world, planned, registerSpawner: true);

        Logger.Info(
            "MapDoodadOrphan world={0} id={1} jsonRows={2} candidates={3} spawned={4} " +
            "skipCovered={5} skipOccupied={6} skipOther={7}",
            worldName,
            world.Id,
            spawners.Count,
            candidates.Count,
            spawned,
            skipCovered,
            skipOccupied,
            skipOther);
    }

    /// <summary>
    /// Json-only maritime weather FX volumes for one world. WeatherManager owns this path.
    /// </summary>
    private static void PlantMaritimeWeatherVolumes(WorldInstance world)
    {
        if (world?.Template == null || !AppConfiguration.Instance.World.SpawnDoodads)
            return;

        var worldName = world.Template.Name;
        var jsonPath = Path.Combine(FileManager.AppPath, "Data", "Worlds", worldName, "doodad_spawns.json");
        if (!File.Exists(jsonPath))
            return;

        var contents = FileManager.GetFileContents(jsonPath);
        if (string.IsNullOrWhiteSpace(contents) ||
            !JsonHelper.TryDeserializeObject(contents, out List<DoodadSpawner> spawners, out _))
        {
            Logger.Warn("MaritimeWeather world={0}: failed to parse {1}", worldName, jsonPath);
            return;
        }

        var levelPack = ZoneDoodadPlacementCatalog.GetAll(worldName);
        var inLevelPack = new HashSet<uint>();
        foreach (var row in levelPack)
            inLevelPack.Add(row.TemplateId);

        var candidates = new List<MapDoodadPlantRules.Placement>();
        var existing = ListAllExisting(world);
        var skipCovered = 0;
        var skipOccupied = 0;

        foreach (var spawner in spawners)
        {
            if (spawner?.Position == null || spawner.UnitId == 0)
                continue;
            if (!DoodadManager.Instance.Exist(spawner.UnitId))
                continue;
            var template = DoodadManager.Instance.GetTemplate(spawner.UnitId);
            if (!MaritimeWeatherRules.IsVolumeModel(template?.Model))
                continue;
            if (inLevelPack.Contains(spawner.UnitId))
            {
                skipCovered++;
                continue;
            }

            var place = new MapDoodadPlantRules.Placement(
                spawner.UnitId,
                spawner.Position.X,
                spawner.Position.Y,
                spawner.Position.Z,
                spawner.Position.Yaw,
                spawner.Scale > 0f ? spawner.Scale : 1f,
                template?.GroupId ?? 0,
                InvisibleStart: TemplateStartsInvisible(spawner.UnitId));

            if (MapDoodadPlantRules.IsStandOccupied(existing, place, crossTemplateOccupancy: true))
            {
                skipOccupied++;
                continue;
            }

            candidates.Add(place);
        }

        var planned = MapDoodadPlantRules.Plan(candidates, existing, crossTemplateOccupancy: true);
        var spawned = planned.Count == 0 ? 0 : SpawnPlanned(world, planned, registerSpawner: true);
        Logger.Info(
            "MaritimeWeather world={0} id={1} candidates={2} spawned={3} skipCovered={4} skipOccupied={5}",
            worldName, world.Id, candidates.Count, spawned, skipCovered, skipOccupied);
    }

    private static void CollectFuncFlags(
        DoodadTemplate template,
        out bool hasEvidenceLoot,
        out bool hasResidentTownhall,
        out bool hasQuestFunc,
        out bool hasCraftPack,
        out bool hasNaviMailbox,
        out bool hasSalvageGimmickFakeUse,
        out bool hasPurchase,
        out bool hasStoreUi,
        out bool hasTransferTelescope,
        out bool hasFlavorContainer,
        out bool hasWorldInteract)
    {
        hasEvidenceLoot = false;
        hasResidentTownhall = false;
        hasQuestFunc = false;
        hasCraftPack = false;
        hasNaviMailbox = false;
        hasSalvageGimmickFakeUse = false;
        hasPurchase = false;
        hasStoreUi = false;
        hasTransferTelescope = false;
        hasFlavorContainer = false;
        hasWorldInteract = false;
        if (template?.FuncGroups == null)
            return;

        var hasFakeUse = false;
        var hasRatioRespawn = false;

        foreach (var group in template.FuncGroups)
        {
            foreach (var func in DoodadManager.Instance.GetFuncsForGroup(group.Id))
            {
                if (func.FuncType == nameof(DoodadFuncEvidenceItemLoot))
                    hasEvidenceLoot = true;
                else if (func.FuncType == nameof(DoodadFuncResidentTownhallUiOpen))
                    hasResidentTownhall = true;
                else if (func.FuncType == nameof(DoodadFuncQuest))
                    hasQuestFunc = true;
                else if (func.FuncType == nameof(DoodadFuncCraftPack))
                    hasCraftPack = true;
                else if (func.FuncType == nameof(DoodadFuncNaviOpenMailbox))
                    hasNaviMailbox = true;
                else if (func.FuncType == nameof(DoodadFuncPurchase))
                    hasPurchase = true;
                else if (func.FuncType == nameof(DoodadFuncStoreUi))
                    hasStoreUi = true;
                else if (func.FuncType is nameof(DoodadFuncUse)
                         or nameof(DoodadFuncSkillHit)
                         or nameof(DoodadFuncOpenPaper)
                         or nameof(DoodadFuncDevote))
                    hasWorldInteract = true;
                else if (func.FuncType == nameof(DoodadFuncFakeUse))
                {
                    hasFakeUse = true;
                    if (DoodadManager.Instance.GetFuncTemplate(func.FuncId, func.FuncType)
                            is DoodadFuncFakeUse fakeUse)
                    {
                        if (SkillSpawnsGimmick(fakeUse.SkillId) || SkillSpawnsGimmick(fakeUse.FakeSkillId))
                            hasSalvageGimmickFakeUse = true;
                        if (SkillGrantsTransferTelescope(fakeUse.SkillId) ||
                            SkillGrantsTransferTelescope(fakeUse.FakeSkillId))
                            hasTransferTelescope = true;
                    }
                }
            }

            foreach (var phase in DoodadManager.Instance.GetPhaseFunc(group.Id))
            {
                if (phase.FuncType == nameof(DoodadFuncRatioRespawn))
                    hasRatioRespawn = true;
            }
        }

        // Breakable world flavor: barrels / crates that FakeUse-break then RatioRespawn
        // (Freedich crate pit, seabed wreck props). Spawner registration keeps the cycle alive.
        hasFlavorContainer = hasFakeUse && hasRatioRespawn;
    }

    /// <summary>
    /// Sunken-treasure FakeUse rows arm a skill whose payload is <c>SpawnGimmickEffect</c>.
    /// </summary>
    private static bool SkillSpawnsGimmick(uint skillId)
    {
        if (skillId == 0)
            return false;
        var skill = SkillManager.Instance.GetSkillTemplate(skillId);
        if (skill?.Effects == null)
            return false;
        foreach (var effect in skill.Effects)
        {
            if (effect?.Template is SpawnGimmickEffect)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Manager spyglass FakeUse → buff with <c>transfer_telescope_range</c> (public transport radar).
    /// </summary>
    private static bool SkillGrantsTransferTelescope(uint skillId)
    {
        if (skillId == 0)
            return false;
        var skill = SkillManager.Instance.GetSkillTemplate(skillId);
        if (skill?.Effects == null)
            return false;
        foreach (var effect in skill.Effects)
        {
            if (effect?.Template is not BuffEffect buffEffect)
                continue;
            var buff = buffEffect.Buff ?? SkillManager.Instance.GetBuffTemplate(buffEffect.BuffId);
            if (buff is { TransferTelescopeRange: > 0 })
                return true;
        }
        return false;
    }

    private static MapDoodadSchedulePeriod ToSchedulePeriod(GameScheduleManager.PeriodStatus status) =>
        status switch
        {
            GameScheduleManager.PeriodStatus.NotFound => MapDoodadSchedulePeriod.NotScheduled,
            GameScheduleManager.PeriodStatus.NotStarted => MapDoodadSchedulePeriod.NotStarted,
            GameScheduleManager.PeriodStatus.InProgress => MapDoodadSchedulePeriod.InProgress,
            GameScheduleManager.PeriodStatus.Ended => MapDoodadSchedulePeriod.Ended,
            _ => MapDoodadSchedulePeriod.NotScheduled
        };

    private static int ArmScheduleSpawners(
        WorldInstance world,
        IReadOnlyList<MapDoodadPlantRules.Placement> planned)
    {
        var armed = 0;
        foreach (var place in planned)
        {
            if (!TryBuildSpawner(world, place, out var spawner))
                continue;
            if (world.SpawnManager.AddDoodadSpawner(spawner) == 0)
                continue;
            // Spawn() → DoSpawn schedule branch registers cron (NotStarted) or no-ops (Ended).
            try
            {
                spawner.Spawn(0);
                armed++;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "MapDoodadPlant schedule arm failed tpl={0}", place.TemplateId);
            }
        }

        return armed;
    }

    private static int SpawnPlanned(
        WorldInstance world,
        IReadOnlyList<MapDoodadPlantRules.Placement> planned,
        bool registerSpawner)
    {
        var spawned = 0;
        foreach (var place in planned)
        {
            if (!TryBuildSpawner(world, place, out var spawner))
                continue;

            if (registerSpawner)
                world.SpawnManager.AddDoodadSpawner(spawner);

            Doodad doodad;
            try
            {
                doodad = spawner.Spawn(0);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "MapDoodadPlant spawn failed tpl={0}", place.TemplateId);
                continue;
            }

            // Schedule NotStarted returns before Create — ObjId may stay 0; that is OK for arming.
            if (doodad?.ObjId > 0)
                spawned++;
        }

        return spawned;
    }

    private static bool TryBuildSpawner(
        WorldInstance world,
        MapDoodadPlantRules.Placement place,
        out DoodadSpawner spawner)
    {
        spawner = null;
        if (!DoodadManager.Instance.Exist(place.TemplateId))
        {
            Logger.Warn("MapDoodadPlant unknown doodad template {0}", place.TemplateId);
            return false;
        }

        var zoneId = WorldManager.Instance.GetZoneId(world.Template, place.X, place.Y);
        if (zoneId == 0)
        {
            Logger.Warn(
                "MapDoodadPlant tpl={0} at {1:0.##},{2:0.##} has no zone — not spawning",
                place.TemplateId, place.X, place.Y);
            return false;
        }

        spawner = new DoodadSpawner
        {
            ParentWorld = world,
            Id = 0,
            UnitId = place.TemplateId,
            Scale = place.Scale > 0f ? place.Scale : 1f,
            Position = new WorldSpawnPosition
            {
                WorldId = world.Id,
                X = place.X,
                Y = place.Y,
                Z = place.Z,
                ZoneId = zoneId,
                Yaw = place.YawDegrees.DegToRad()
            }
        };
        return true;
    }

    private static List<MapDoodadPlantRules.Existing> ListAllExisting(WorldInstance world)
    {
        var list = new List<MapDoodadPlantRules.Existing>();
        foreach (var doodad in world.GetAllDoodads())
        {
            var pos = doodad.Transform?.World;
            if (pos == null)
                continue;
            list.Add(new MapDoodadPlantRules.Existing(
                doodad.TemplateId,
                pos.Position.X,
                pos.Position.Y,
                pos.Position.Z,
                doodad.Template?.GroupId ?? 0,
                InvisibleStart: TemplateStartsInvisible(doodad.TemplateId)));
        }

        return list;
    }

    /// <summary>
    /// Start-phase (or almighty) model never draws — Ethereal Haven wait, etc.
    /// </summary>
    private static bool TemplateStartsInvisible(uint templateId)
    {
        var template = DoodadManager.Instance.GetTemplate(templateId);
        if (template == null)
            return false;

        string model = null;
        if (template.FuncGroups != null)
        {
            foreach (var group in template.FuncGroups)
            {
                if (group.GroupKindId == DoodadFuncGroups.DoodadFuncGroupKind.Start)
                {
                    model = group.Model;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(model))
            model = template.Model;

        return MapDoodadPlantRules.IsInvisibleStartModel(model);
    }
}
