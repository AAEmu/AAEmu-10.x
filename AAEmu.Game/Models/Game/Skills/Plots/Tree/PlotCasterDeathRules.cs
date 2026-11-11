namespace AAEmu.Game.Models.Game.Skills.Plots.Tree;

/// <summary>
/// Death interrupts every plot its caster was running, not only the one in <c>ActivePlotState</c>: a looping
/// OnSpawn plot (Songsong's 7127) loses that slot to later casts and would otherwise outlive its caster.
/// Plots started after death (on_death skills) still run.
/// </summary>
public static class PlotCasterDeathRules
{
    public static bool StopsPlot(bool casterAliveAtStart, bool casterDeadNow) => casterAliveAtStart && casterDeadNow;
}
