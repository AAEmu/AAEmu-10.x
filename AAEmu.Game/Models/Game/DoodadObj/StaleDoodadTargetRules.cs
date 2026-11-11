using AAEmu.Game.Models.Game.Skills.Static;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// A client that uses a doodad the server has already removed still shows it; the server drops it for that client.
/// </summary>
public static class StaleDoodadTargetRules
{
    public static bool IsStale(SkillResult result, uint doodadObjId, bool doodadExists) =>
        result == SkillResult.NoTarget && doodadObjId != 0 && !doodadExists;
}
