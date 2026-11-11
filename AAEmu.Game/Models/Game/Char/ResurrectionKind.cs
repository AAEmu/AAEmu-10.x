namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// The choice a dead character's client sends with <c>CSResurrectCharacterPacket</c>, one byte.
/// </summary>
public enum ResurrectionKind : byte
{
    /// <summary>Come back at the return point (temple).</summary>
    ReturnPoint = 0,

    /// <summary>
    /// Come back in place. The death window sends it for three choices: accepting another player's resurrection,
    /// the free in-place revive (<c>freeResurrectionInPlace</c>) and the self grace revive.
    /// </summary>
    InPlace = 1,

    /// <summary>The death window's special revive, which has its own wait timer. Not in place.</summary>
    SpecialRevive = 2,
}
