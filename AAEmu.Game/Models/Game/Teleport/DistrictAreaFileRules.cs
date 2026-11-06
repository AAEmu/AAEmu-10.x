using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace AAEmu.Game.Models.Game.Teleport;

/// <summary>
/// Server district shapes in <c>level_design/zone/{zoneKey}/world_server/district.xml</c>.
/// Each <c>AreaShape</c> entity holds one area whose <c>value1</c> is compact <c>districts.id</c>
/// (the <c>Area Id</c> attribute is always 0 here). Entity <c>Pos</c> is zone-local; the point
/// offsets are scaled and rotated by the entity's optional <c>Scale</c> and <c>Rotate</c>
/// (CryEngine quaternion w,x,y,z) before they are added to it.
/// <para>
/// These shapes are what records a return district in the Teleport Book. The client's
/// <c>subzone_area.xml</c> (the <c>CSNotifySubZone</c> path) covers only part of them: the core
/// Auroria zones (Salpimari, Nuimari, Marcala, Calmlands, Golden Ruins) ship an empty one.
/// </para>
/// </summary>
public static class DistrictAreaFileRules
{
    public readonly record struct LocalDistrict(uint ZoneKey, uint DistrictId, IReadOnlyList<Vector2> Points);

    public static List<LocalDistrict> Parse(string xml, uint zoneKey)
    {
        var list = new List<LocalDistrict>();
        if (string.IsNullOrWhiteSpace(xml) || zoneKey == 0)
            return list;

        // Malformed XML throws so the catalog can log the broken file path.
        var doc = XDocument.Parse(xml);

        foreach (var entity in doc.Root?.Elements("Entity") ?? [])
        {
            if (!TryReadVector(entity.Attribute("Pos")?.Value, out var origin))
                continue;

            var cellOffset = new Vector2(ReadIntAttr(entity, "cellX") * 1024f, ReadIntAttr(entity, "cellY") * 1024f);
            var scale = TryReadVector(entity.Attribute("Scale")?.Value, out var s) ? s : Vector3.One;
            var rotation = TryReadQuaternion(entity.Attribute("Rotate")?.Value, out var q) ? q : Quaternion.Identity;

            foreach (var area in entity.Elements("Area"))
            {
                if (!uint.TryParse(area.Attribute("value1")?.Value, NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var districtId) ||
                    districtId == 0)
                    continue;

                var points = new List<Vector2>();
                foreach (var point in area.Element("Points")?.Elements("Point") ?? [])
                {
                    if (!TryReadVector(point.Attribute("Pos")?.Value, out var offset))
                        continue;
                    points.Add(ToLocal(origin, cellOffset, scale, rotation, offset));
                }

                if (points.Count < 3)
                    continue;

                list.Add(new LocalDistrict(zoneKey, districtId, points));
            }
        }

        return list;
    }

    /// <summary>Zone-local XY of one shape vertex.</summary>
    public static Vector2 ToLocal(Vector3 origin, Vector2 cellOffset, Vector3 scale, Quaternion rotation, Vector3 offset)
    {
        var turned = Vector3.Transform(offset * scale, rotation);
        return new Vector2(origin.X + cellOffset.X + turned.X, origin.Y + cellOffset.Y + turned.Y);
    }

    private static bool TryReadVector(string raw, out Vector3 value)
    {
        value = Vector3.Zero;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var parts = raw.Split(',');
        if (parts.Length < 2 ||
            !TryF(parts[0], out var x) ||
            !TryF(parts[1], out var y))
            return false;
        var z = 0f;
        if (parts.Length >= 3)
            TryF(parts[2], out z);
        value = new Vector3(x, y, z);
        return true;
    }

    private static bool TryReadQuaternion(string raw, out Quaternion value)
    {
        value = Quaternion.Identity;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var parts = raw.Split(',');
        if (parts.Length != 4 ||
            !TryF(parts[0], out var w) || !TryF(parts[1], out var x) ||
            !TryF(parts[2], out var y) || !TryF(parts[3], out var z))
            return false;
        var q = new Quaternion(x, y, z, w);
        if (q.LengthSquared() < 1e-6f)
            return false;
        value = Quaternion.Normalize(q);
        return true;
    }

    private static bool TryF(string s, out float v) =>
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private static int ReadIntAttr(XElement el, string name) =>
        int.TryParse(el.Attribute(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
}

/// <summary>
/// Return-district shapes in world XY, bucketed by zone key, for the position check that records
/// Teleport Book districts.
/// </summary>
public sealed class DistrictAreaIndex
{
    private readonly record struct Shape(uint DistrictId, IReadOnlyList<Vector2> Points, Vector2 Min, Vector2 Max);

    private readonly Dictionary<uint, List<Shape>> _byZone = [];

    public int ShapeCount { get; private set; }

    public IReadOnlyCollection<uint> DistrictIds => _districtIds;
    private readonly HashSet<uint> _districtIds = [];

    public void Add(uint zoneKey, uint districtId, IReadOnlyList<Vector2> worldPoints)
    {
        if (zoneKey == 0 || districtId == 0 || worldPoints == null || worldPoints.Count < 3)
            return;

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var p in worldPoints)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        if (!_byZone.TryGetValue(zoneKey, out var shapes))
        {
            shapes = [];
            _byZone[zoneKey] = shapes;
        }

        shapes.Add(new Shape(districtId, worldPoints, min, max));
        _districtIds.Add(districtId);
        ShapeCount++;
    }

    /// <summary>Districts whose shape in <paramref name="zoneKey"/> contains the world XY point.</summary>
    public IReadOnlyList<uint> DistrictsAt(uint zoneKey, float x, float y)
    {
        if (!_byZone.TryGetValue(zoneKey, out var shapes))
            return [];

        List<uint> found = null;
        foreach (var shape in shapes)
        {
            if (x < shape.Min.X || x > shape.Max.X || y < shape.Min.Y || y > shape.Max.Y)
                continue;
            if (!SubZoneAreaFileRules.Contains(shape.Points, x, y))
                continue;
            found ??= [];
            if (!found.Contains(shape.DistrictId))
                found.Add(shape.DistrictId);
        }

        return (IReadOnlyList<uint>)found ?? [];
    }
}

/// <summary>
/// How a district recorded from its <c>district.xml</c> shape is stored and resolved.
/// </summary>
public static class DistrictVisitRules
{
    /// <summary>
    /// District visits share <c>portal_visited_district.subzone</c> (signed INT) with subzone visits.
    /// Compact <c>sub_zones.id</c> stays far below this base, so a district is stored as base + id.
    /// </summary>
    public const uint DistrictVisitKeyBase = 10_000_000;

    public static uint ToVisitKey(uint districtId) => DistrictVisitKeyBase + districtId;

    public static bool TryGetDistrictId(uint visitKey, out uint districtId)
    {
        districtId = visitKey > DistrictVisitKeyBase ? visitKey - DistrictVisitKeyBase : 0;
        return districtId != 0;
    }

    /// <summary>
    /// Which return point a district gives a character: the row for their own faction, else a row for
    /// a faction with the same mother faction (player nations), else none: the district is not theirs.
    /// </summary>
    public static uint PickReturnPoint(
        IReadOnlyList<(uint FactionId, uint ReturnPointId)> rows,
        uint factionId,
        Func<uint, uint> motherOf)
    {
        if (rows == null || rows.Count == 0)
            return 0;

        foreach (var row in rows)
        {
            if (row.FactionId == factionId && row.ReturnPointId != 0)
                return row.ReturnPointId;
        }

        var mother = motherOf?.Invoke(factionId) ?? 0;
        if (mother == 0)
            return 0;

        uint best = 0;
        var bestFaction = uint.MaxValue;
        foreach (var row in rows)
        {
            if (row.ReturnPointId == 0 || (motherOf?.Invoke(row.FactionId) ?? 0) != mother)
                continue;
            if (row.FactionId < bestFaction)
            {
                bestFaction = row.FactionId;
                best = row.ReturnPointId;
            }
        }

        return best;
    }

    /// <summary>Districts in <paramref name="current"/> that were not in <paramref name="previous"/>.</summary>
    public static IReadOnlyList<uint> Entered(IReadOnlyCollection<uint> previous, IReadOnlyList<uint> current)
    {
        if (current == null || current.Count == 0)
            return [];
        if (previous == null || previous.Count == 0)
            return current;
        var entered = new List<uint>();
        foreach (var id in current)
        {
            if (!previous.Contains(id))
                entered.Add(id);
        }

        return entered;
    }
}
