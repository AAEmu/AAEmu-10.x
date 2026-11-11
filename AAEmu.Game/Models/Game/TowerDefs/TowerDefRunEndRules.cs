namespace AAEmu.Game.Models.Game.TowerDefs;

/// <summary>When a tower_defs run has nothing left to open: its last step is open and that step's own time is up.</summary>
public static class TowerDefRunEndRules
{
    public static bool IsComplete(int nextStep, int progCount, int killWaitStep, DateTime lastStepDueAt, DateTime now)
        => progCount > 0 && nextStep >= progCount && killWaitStep < 0 && now >= lastStepDueAt;
}
