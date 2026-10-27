namespace AAEmu.Game.Models.Game.Weather;

/// <summary>
/// Local maritime weather FX volumes (cloudy / rainstorm / maelstrom / thundercloud).
/// Owned by <see cref="AAEmu.Game.Core.Managers.WeatherManager"/> — placement and the doodad
/// Timer→Clout→RatioRespawn cycle are the local half of weather; the schedule Clear/Snow cycle
/// is the global half. Not derived from display names: model path tokens only.
/// </summary>
public static class MaritimeWeatherRules
{
    /// <summary>
    /// Prefab models that carry the active maritime weather FX. After their window they
    /// RatioRespawn into the hub, which RatioRespawns back into one of these.
    /// </summary>
    public static bool IsVolumeModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return false;
        return model.Contains("env.cloud", StringComparison.OrdinalIgnoreCase)
               || model.Contains("env.rainstorm", StringComparison.OrdinalIgnoreCase)
               || model.Contains("env.vortex", StringComparison.OrdinalIgnoreCase)
               || model.Contains("env.raincloud", StringComparison.OrdinalIgnoreCase);
    }
}
