using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Weather;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// Whether a level-pack <c>doodad.g</c> row should be World-authored as a permanent map doodad.
/// Coordinates always come from the catalog; this only classifies the row.
/// </summary>
public enum MapDoodadPlantDecision
{
    Plant = 0,
    SkipUnknownTemplate,
    SkipIgnoredPermanent,
    SkipTowerAlmighty,
    /// <summary>
    /// Scheduled and outside the live window — register a spawner for cron wake, do not Create now.
    /// </summary>
    SkipSchedule,
    SkipUnusedGroup,
    SkipStandOccupied,
    SkipJsonOrphanCoveredByLevelPack,
    SkipHousingOrphan,
    SkipRuntimeEvidence,
    SkipRandomPlacement,
    SkipJsonOrphanUnhandled,
}

/// <summary>
/// Schedule window for a doodad template, mirrored from
/// <c>GameScheduleManager.PeriodStatus</c> without pulling the manager into this rules type.
/// <see cref="NotScheduled"/> means the template is not in <c>game_schedule_doodads</c>.
/// </summary>
public enum MapDoodadSchedulePeriod
{
    NotScheduled = 0,
    NotStarted,
    InProgress,
    Ended,
}

/// <summary>
/// Configured plant source for open-world map doodads. Values match
/// <c>World.MapDoodadSource</c> in <c>Configurations/World.json</c>.
/// </summary>
public static class MapDoodadSourceKind
{
    /// <summary><c>doodad_spawns*.json</c> only (plus the narrow quest-talk .g fill).</summary>
    public const string Json = "Json";

    /// <summary>
    /// Legacy dual source: load every <c>doodad_spawns*.json</c>, then plant allowed .g rows
    /// (same-template 2 m dedup). Prefer <see cref="LevelPack"/>.
    /// </summary>
    public const string LevelPackFill = "LevelPackFill";

    /// <summary>
    /// Authority: plant from .g, arm off-window schedule spawners, salvage approved json-only
    /// orphans. SpawnManager loads json only when the filename contains <c>overlay</c>.
    /// </summary>
    public const string LevelPack = "LevelPack";
}

/// <summary>
/// Fail-closed decision table for map doodads from <c>cells/*/doodad.g</c>.
/// </summary>
public static class MapDoodadPlantRules
{
    /// <summary>
    /// Same-template distance used when merging json boot rows with level-pack placements.
    /// </summary>
    public const float DedupMetres = 2f;

    public readonly record struct Placement(
        uint TemplateId,
        float X,
        float Y,
        float Z,
        float YawDegrees,
        float Scale = 1f,
        uint GroupId = 0,
        /// <summary>
        /// Start-phase model is <c>a://invalid</c> / <c>env.invisible</c> — pad looks empty until
        /// an event SkillHit. Does not occupy the stand for other portal templates.
        /// </summary>
        bool InvisibleStart = false);

    public readonly record struct Existing(
        uint TemplateId,
        float X,
        float Y,
        float Z,
        uint GroupId = 0,
        bool InvisibleStart = false);

    /// <summary>
    /// Cross-template stand occupancy is only for portal pads (closed↔open / json twin
    /// supersession). Landmark + event FX can share XYZ on purpose (Whirlpool statue + vortex).
    /// Ethereal Haven (invisible start) + Golden Plains battle closed (json) share a pad — the
    /// invisible wait portal must not block the visible battle twin.
    /// </summary>
    public static bool IsPortalStandGroup(uint groupId) =>
        groupId is (uint)DoodadGroupId.PortalWorldgate
            or (uint)DoodadGroupId.PortalMirage
            or (uint)DoodadGroupId.PortalInstance;

    /// <summary>
    /// Start / almighty model that never draws a mesh until a later phase.
    /// </summary>
    public static bool IsInvisibleStartModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return false;
        return model.Contains("a://invalid", StringComparison.OrdinalIgnoreCase)
               || model.Contains("env.invisible", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maritime local-weather FX volumes — owned by <c>WeatherManager</c> /
    /// <see cref="MaritimeWeatherRules"/>, not orphan salvage.
    /// </summary>
    public static bool IsMaritimeWeatherVolumeModel(string model) =>
        MaritimeWeatherRules.IsVolumeModel(model);

    /// <summary>
    /// Classify one placement. <paramref name="groupId"/> is <c>doodad_almighties.group_id</c>
    /// when the template exists; ignored when <paramref name="templateExists"/> is false.
    /// </summary>
    public static MapDoodadPlantDecision Decide(
        bool templateExists,
        bool ignoredPermanent,
        bool towerAlmighty,
        MapDoodadSchedulePeriod schedule,
        uint groupId)
    {
        if (!templateExists)
            return MapDoodadPlantDecision.SkipUnknownTemplate;
        if (towerAlmighty)
            return MapDoodadPlantDecision.SkipTowerAlmighty;
        if (groupId == (uint)DoodadGroupId.Unused)
            return MapDoodadPlantDecision.SkipUnusedGroup;

        // Schedule window first — ignore_* lists mark event stands, but those same templates
        // often sit in game_schedule_doodads. Off-window → arm cron (SkipSchedule); live window
        // → plant even when IgnoredPermanent. Pure ignore (no schedule row) stays SkipIgnored.
        if (schedule is MapDoodadSchedulePeriod.NotStarted or MapDoodadSchedulePeriod.Ended)
            return MapDoodadPlantDecision.SkipSchedule;
        if (ignoredPermanent && schedule == MapDoodadSchedulePeriod.NotScheduled)
            return MapDoodadPlantDecision.SkipIgnoredPermanent;

        // NotScheduled (non-ignore) and InProgress (including ignore-listed festival stands) plant.
        return MapDoodadPlantDecision.Plant;
    }

    public static bool ShouldPlantFromLevelPack(string mapDoodadSource) =>
        IsLevelPackFill(mapDoodadSource) || IsLevelPackOnly(mapDoodadSource);

    public static bool IsLevelPackFill(string mapDoodadSource) =>
        string.Equals(mapDoodadSource, MapDoodadSourceKind.LevelPackFill, StringComparison.OrdinalIgnoreCase);

    public static bool IsLevelPackOnly(string mapDoodadSource) =>
        string.Equals(mapDoodadSource, MapDoodadSourceKind.LevelPack, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a <c>doodad_spawns*.json</c> file under the world folder should be loaded.
    /// LevelPack keeps only overlay files; Json and LevelPackFill load every match.
    /// </summary>
    public static bool ShouldLoadJsonSpawnFile(string mapDoodadSource, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        if (!IsLevelPackOnly(mapDoodadSource))
            return true;
        return fileName.Contains("overlay", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSamePlacement(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return dx * dx + dy * dy + dz * dz <= DedupMetres * DedupMetres;
    }

    /// <summary>
    /// Catalog rows to spawn after classification.
    /// Same-template rows within <see cref="DedupMetres"/> always dedup.
    /// When <paramref name="crossTemplateOccupancy"/> is true (LevelPack default), a live
    /// <b>portal</b> doodad also occupies the stand for other portal templates — so a closed
    /// worldgate from .g is not stacked with a legacy always-open json twin. Non-portal
    /// groups (statue + vortex, etc.) may share XYZ; event status drives visibility via phases.
    /// </summary>
    public static IReadOnlyList<Placement> Plan(
        IReadOnlyList<Placement> catalog,
        IReadOnlyList<Existing> existing,
        bool crossTemplateOccupancy = true)
    {
        if (catalog == null || catalog.Count == 0)
            return [];

        var live = existing ?? [];
        var planned = new List<Placement>(catalog.Count);
        foreach (var place in catalog)
        {
            if (place.TemplateId == 0)
                continue;
            if (IsStandOccupied(live, place, crossTemplateOccupancy))
                continue;
            planned.Add(place);
            // Later catalog rows must see this stand as taken (two .g types on one pad).
            live = AppendExisting(live, place);
        }

        return planned;
    }

    public static bool IsStandOccupied(
        IReadOnlyList<Existing> existing,
        Placement place,
        bool crossTemplateOccupancy)
    {
        foreach (var row in existing)
        {
            if (!IsSamePlacement(row.X, row.Y, row.Z, place.X, place.Y, place.Z))
                continue;
            if (row.TemplateId == place.TemplateId)
                return true;
            if (!crossTemplateOccupancy)
                continue;
            // Portal pads only — layered Interaction FX on the same XYZ must both plant.
            if (!IsPortalStandGroup(row.GroupId) || !IsPortalStandGroup(place.GroupId))
                continue;
            // Invisible wait portals (Haven start) leave the pad empty — a visible battle twin
            // (Golden Plains closed) may still plant. Two visible portals still collide.
            if (row.InvisibleStart)
                continue;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<Existing> AppendExisting(IReadOnlyList<Existing> existing, Placement place)
    {
        var next = new List<Existing>(existing.Count + 1);
        next.AddRange(existing);
        next.Add(new Existing(
            place.TemplateId, place.X, place.Y, place.Z, place.GroupId, place.InvisibleStart));
        return next;
    }
}
