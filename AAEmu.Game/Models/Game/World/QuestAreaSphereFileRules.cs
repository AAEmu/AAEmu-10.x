namespace AAEmu.Game.Models.Game.World;

/// <summary>
/// Which <c>quest_area_sphere.g</c> files the World loads. Each zone has one, at
/// <c>.../zone/&lt;zoneId&gt;/world_server/quest_area_sphere.g</c>, but it is listed twice: once by the
/// client file manager (relative path) and once by the loose ZoneGameDataRoot scan (absolute path).
/// The two spellings never compare equal, so every zone's volumes were loaded twice (206 files for 103
/// zones) and every quest-area sphere fired ENTER twice per evaluation.
/// </summary>
public static class QuestAreaSphereFileRules
{
    /// <summary>
    /// Keeps the first listed file for each zone, in listing order, so the client file manager's copy
    /// wins and the loose scan only fills zones it does not have. Paths without a zone id are kept for
    /// the loader to report.
    /// </summary>
    public static List<string> OnePerZone(IEnumerable<string> paths)
    {
        var result = new List<string>();
        if (paths == null)
            return result;

        var seenZones = new HashSet<uint>();
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;
            if (TryParseZoneId(path, out var zoneId) && !seenZones.Add(zoneId))
                continue;
            result.Add(path);
        }

        return result;
    }

    /// <summary>Zone id from <c>.../zone/&lt;zoneId&gt;/world_server/quest_area_sphere.g</c>, either slash.</summary>
    public static bool TryParseZoneId(string path, out uint zoneId)
    {
        zoneId = 0;
        if (string.IsNullOrEmpty(path))
            return false;

        var parts = path.Split('/', '\\');
        // [..., "<zoneId>", "world_server", "quest_area_sphere.g"]
        return parts.Length >= 3 && uint.TryParse(parts[^3], out zoneId);
    }
}
