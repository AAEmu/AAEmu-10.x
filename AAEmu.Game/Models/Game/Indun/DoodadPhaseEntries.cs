namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Tells a doodad moving into a phase from a repeat walk of the phase it already sits in.</summary>
public sealed class DoodadPhaseEntries
{
    private uint _lastPhase;

    public bool IsNewEntry(uint phase)
    {
        var isNew = phase != _lastPhase;
        _lastPhase = phase;
        return isNew;
    }
}
