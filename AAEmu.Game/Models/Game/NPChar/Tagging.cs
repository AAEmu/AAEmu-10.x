using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Items.Containers;

namespace AAEmu.Game.Models.Game.NPChar;

public class Tagging(Unit owner)
{
    private readonly object _lock = new();
    private Dictionary<Character, int> _taggers = [];
    private Character _tagger;
    private uint _tagTeam;
    // private int _totalDamage;

    public Unit Owner { get; } = owner;

    public Character Tagger
    {
        get
        {
            lock (_lock)
            {
                return _tagger;
            }
        }
    }

    public uint TagTeam
    {
        get
        {
            lock (_lock)
            {
                return _tagTeam;
            }
        }
    }

    public void ClearAllTaggers()
    {
        _taggers = [];
        _tagger = null;
        _tagTeam = 0;
        // _totalDamage = 0;
    }

    /// <summary>
    /// Whether a hit that just changed an NPC's health is a killing blow to record as a tag: damage
    /// (not a GM kill, a pet capture or any other removal) by another unit took it from alive to dead,
    /// and nobody holds tag rights yet. <see cref="AddTagger"/> then resolves the attacker to the
    /// character behind it, or ignores it when there is none (an NPC guard).
    /// </summary>
    public static bool KillingBlowTags(bool hasTagger, uint tagTeam, BaseUnit attacker, BaseUnit victim,
        int oldHp, int newHp, KillReason killReason) =>
        killReason == KillReason.Damage && oldHp > 0 && newHp <= 0
        && !hasTagger && tagTeam == 0 && attacker is Unit && !ReferenceEquals(attacker, victim);

    public void AddTagger(Unit checkUnit, int damage)
    {
        lock (_lock)
        {
            // Check if the character is a pet, if so, propagate its user
            if (checkUnit is Units.Mate mate)
            {
                checkUnit = WorldManager.Instance.GetCharacterByObjId(mate.OwnerObjId) ?? checkUnit;
            }
            // A vehicle's hits count for the character firing it: the driver of an ownerless world cannon
            // (quest 1401's Andelph cannons), otherwise the vehicle's owner.
            else if (checkUnit is Units.Slave slave)
            {
                checkUnit = (Unit)(slave.CombatSide as Character) ?? slave.GetOwnerCharacter() ?? checkUnit;
            }

            if (checkUnit is Character player)
            {
                if (_taggers.TryAdd(player, damage))
                {
                    _tagger ??= player;
                }
                else
                {
                    _taggers[player] += damage;
                }

                // _totalDamage += damage;

                // Check if the character is in a party
                if (player.InParty)
                {
                    var checkTeam = TeamManager.Instance.GetTeamByObjId(player.ObjId);
                    var partyDamage = 0;
                    foreach (var member in checkTeam.Members)
                    {
                        if (member == null || member.Character == null)
                            continue;

                        if (member.Character.GetDistanceTo(Owner, true) <= LootingContainer.MaxLootingRange)
                        {
                            // tm is an eligible party member
                            if (_taggers.TryGetValue(member.Character, out var value))
                            {
                                // Tagger is already in the list
                                partyDamage += value;
                            }
                        }
                    }
                    // Did the party do more than 50% of the total HP in damage?
                    if (partyDamage > Owner.MaxHp * 0.5)
                    {
                        _tagTeam = checkTeam.Id;
                    }
                }
                else
                {
                    if (_taggers[player] > Owner.MaxHp * 0.5)
                    {
                        _tagger = player;
                    }
                }
            }
            // TODO: packet to set red-but-not-aggro HP bar for taggers, "dull red" HP bar for not-taggers
        }
    }
}
