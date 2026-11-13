namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// A <c>next_phase = -1</c> use normally consumes the doodad. When the same group also offers a use that moves
/// the doodad on, the -1 use is one player choice of several and the doodad stays, unless that choice hands out items.
/// </summary>
public static class DoodadStayOnPhaseRules
{
    public static bool KeepsDoodad(string funcType, int nextPhase,
        IEnumerable<(string FuncType, int NextPhase)> group, bool grantsItems)
        => nextPhase == -1
           && funcType == "DoodadFuncUse"
           && !grantsItems
           && group.Any(f => f.NextPhase > 0 && f.FuncType is "DoodadFuncUse" or "DoodadFuncConditionalUse");
}
