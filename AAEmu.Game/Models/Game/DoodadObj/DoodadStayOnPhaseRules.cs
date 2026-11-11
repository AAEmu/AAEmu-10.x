namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// A <c>next_phase = -1</c> use normally consumes the doodad. When the same group also offers a use that moves
/// the doodad on, the -1 use is one choice of several and the doodad stays.
/// </summary>
public static class DoodadStayOnPhaseRules
{
    public static bool KeepsDoodad(string funcType, int nextPhase, IEnumerable<int> groupNextPhases)
        => nextPhase == -1
           && funcType == "DoodadFuncUse"
           && groupNextPhases.Any(phase => phase > 0);
}
