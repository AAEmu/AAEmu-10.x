using System.Collections.Generic;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Units;

/// <summary>
/// Whose side a slave fights on as an attacker. World-spawned vehicles (the Andelph cannons of quest
/// 1401, slave 609) have no summoner and keep their template faction, Friendly (1), which is friendly
/// to every faction: the cannon's hostile-only blast filter and DamageEffect's CanAttack both dropped
/// the golems, so every shot landed on nobody. While someone sits in the driver seat such a vehicle
/// fights as that character, against that character's hostile targets only (see
/// <see cref="BaseUnit.CanAttack"/>). A summoned vehicle already took its owner's faction at spawn, and
/// a part of a hull (OwnerObjId = the hull) took the hull's, so both keep their own. So does an
/// ownerless slave whose template names a real faction: only Friendly needs the stand-in.
/// </summary>
public static class SlaveCombatSideRules
{
    /// <returns>The driver to fight as, or null to keep the slave's own faction.</returns>
    public static Character DriverSide(bool hasSummoner, uint ownerObjId, FactionsEnum ownFactionId,
        IReadOnlyDictionary<AttachPointKind, Character> attachedCharacters)
    {
        if (hasSummoner || ownerObjId != 0 || ownFactionId != FactionsEnum.Friendly || attachedCharacters == null)
            return null;

        return attachedCharacters.TryGetValue(AttachPointKind.Driver, out var driver) ? driver : null;
    }
}
