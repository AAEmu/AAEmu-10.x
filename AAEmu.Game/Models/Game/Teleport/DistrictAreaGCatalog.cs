using System.Globalization;

using NLog;

namespace AAEmu.Game.Models.Game.Teleport;

/// <summary>
/// Reads <c>district.xml</c> under configured ZoneGameDataRoot worlds
/// (<c>level_design/zone/{zoneKey}/world_server/</c>). No fallback disk path.
/// </summary>
public static class DistrictAreaGCatalog
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Every district shape with the name of the world folder it came from: the same zone key can
    /// also sit under a <c>backup_*</c> world, so callers keep the copy whose world owns the zone.
    /// </summary>
    public static List<(string WorldName, DistrictAreaFileRules.LocalDistrict District)> LoadFromRoots(IEnumerable<string> roots)
    {
        var list = new List<(string WorldName, DistrictAreaFileRules.LocalDistrict District)>();
        if (roots == null)
            return list;

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var worlds = Path.Combine(root, "worlds");
            if (!Directory.Exists(worlds))
                continue;

            foreach (var worldDir in Directory.EnumerateDirectories(worlds))
            {
                var zoneRoot = Path.Combine(worldDir, "level_design", "zone");
                if (!Directory.Exists(zoneRoot))
                    continue;
                var worldName = Path.GetFileName(worldDir);

                foreach (var zoneDir in Directory.EnumerateDirectories(zoneRoot))
                {
                    var folder = Path.GetFileName(zoneDir);
                    if (!uint.TryParse(folder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoneKey) ||
                        zoneKey == 0)
                        continue;

                    var path = Path.Combine(zoneDir, "world_server", "district.xml");
                    if (!File.Exists(path))
                        continue;

                    try
                    {
                        foreach (var district in DistrictAreaFileRules.Parse(File.ReadAllText(path), zoneKey))
                            list.Add((worldName, district));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to read district.xml {0}", path);
                    }
                }
            }
        }

        return list;
    }
}
