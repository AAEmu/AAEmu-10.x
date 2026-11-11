using System.Numerics;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Models.Game.World.Zones;
using AAEmu.Game.Models.StaticValues;

using WorldIntegration = AAEmu.Game.WorldIntegration;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSResurrectCharacterPacket() : GamePacket(CSOffsets.CSResurrectCharacterPacket, 1)
{
    /// <summary>Duration of the post-revive Respawn-Cooldown debuff in milliseconds (5 min).</summary>
    private const int RespawnCooldownDurationMs = 300_000;

    public override void Read(PacketStream stream)
    {
        // One byte, not a flag: 0 return point, 1 in place, 2 special revive. Only 1 is in place.
        var kind = (ResurrectionKind)stream.ReadByte();
        var inPlace = kind == ResurrectionKind.InPlace;

        Logger.Debug("ResurrectCharacter, Kind: {0}", kind);

        var portal = new Portal();

        // поищем сначала "UnitId": 502, "Title": "Temple Priestess",
        // Inside dungeons or other instances, just respawn at the nearest Priestess
        if (Connection.ActiveChar.Transform.InstanceId != WorldManager.DefaultInstanceId)
        {
            var npcs = Connection.ActiveChar.ParentWorld.GetAllNpcs();
            foreach (var npc in npcs.Where(npc => npc.TemplateId == 502))
            {
                portal.WorldId = Connection.ActiveChar.Transform.WorldId;
                portal.ZoneId = npc.Transform.ZoneId;
                portal.X = npc.Transform.World.Position.X + Random.Shared.Next(1, 3);
                portal.Y = npc.Transform.World.Position.Y + Random.Shared.Next(1, 3);
                portal.Z = npc.Transform.World.Position.Z;
                portal.ZRot = npc.Transform.World.Rotation.Z;
                portal.Yaw = npc.Transform.World.Rotation.Z;
                break;
            }
        }
        else
        {
            // Check if the current zone is at War and if it has special respawn areas for factions
            var usePortalId = 0u;
            var currentZone = ZoneManager.Instance.GetZoneByKey(Connection.ActiveChar.Transform.ZoneId);
            if (currentZone != null)
            {
                var conflictData = ZoneManager.Instance.GetConflicts().FirstOrDefault(c => c.ZoneGroupId == currentZone.GroupId);
                if (conflictData?.CurrentZoneState == ZoneConflictType.War)
                {
                    switch (Connection.ActiveChar.Faction.MotherId)
                    {
                        case FactionsEnum.NuiaAlliance:
                            usePortalId = conflictData.NuiaReturnPointId;
                            break;
                        case FactionsEnum.HaranyaAlliance:
                            usePortalId = conflictData.HariharaReturnPointId;
                            break;
                    }
                }
            }

            // Try to get a faction specific respawn
            if (usePortalId > 0)
            {
                portal = PortalManager.Instance.GetRespawnById(usePortalId);
            }

            // Find the closest return portal (in the world) for the player if none has been found yet
            if (usePortalId == 0 || portal == null)
            {
                portal = PortalManager.Instance.GetClosestReturnPortal(Connection.ActiveChar);
            }
        }

        var character = Connection.ActiveChar;

        // A wanted character who died comes back as a defendant at their courthouse instead of the
        // temple - the resurrection below moves the server unit to whatever this portal holds.
        var defendantCourt = JusticeManager.Instance.TakeDefendantCourt(character);
        if (defendantCourt != null)
            portal = defendantCourt;

        // Accepting another player's resurrection comes back where the character fell, not at the point found
        // above. The death window sends the same in-place kind for the free in-place revive and the self grace
        // revive, with no offer waiting; those are not handled yet and come back at the return point, as before.
        var whereFallen = character.TakeResurrectOffer(kind, defendantCourt != null);
        if (whereFallen)
            portal = null;

        var oldHp = character.Hp;

        // Resolve spawn point before restoring vitals. Historically we only told the *client*
        // SCCharacterResurrected coordinates and left World Transform at the corpse. With Zone
        // authority, NPCs still had an active unit at the death scene — the moment we set Hp > 0 they
        // hit again, pinned Hp to 0, and GamePing re-emitted zeros while the player stood at temple
        // (IsDead blocks RegenTick). Same-level res must move the server unit first.
        var spot = ResurrectionSpot(portal, character.Transform);
        float rx = spot.World.X, ry = spot.World.Y, rz = spot.World.Z, rrot = spot.WorldYaw;

        // Coming back at a return point (or the courthouse) is a move the server makes, not movement: the speed
        // check forgets the spot it last saw, or the check that falls on this landing measures the trip from where
        // the character died. Only for the dead - this packet is the client's, and a living character must not be
        // able to clear its own check with it.
        if (!whereFallen && character.IsDead)
            SusManager.Instance.ResetAnalyzePlayerDeltaMovement(character.Id);

        // Same-instance temple resurrection must update the authoritative transform here.
        character.DisabledSetPosition = false;
        character.SetPosition(spot.Local.X, spot.Local.Y, spot.Local.Z, 0f, 0f, spot.LocalYaw);
        character.Transform.FinalizeTransform();

        character.IsInBattle = false;
        character.CurrentTarget = null;
        character.IsUnderWater = false;
        character.Breath = character.LungCapacity;

        // Apply the final vital floor after revival debuffs so conversion effects cannot leave the
        // character dead immediately after resurrection.
        int newHp;
        int newMp;
        if (inPlace)
        {
            var hpPct = character.ResurrectHpPercent;
            var mpPct = character.ResurrectMpPercent;
            if (hpPct == 0)
                hpPct = 1;
            if (mpPct == 0)
                mpPct = 1;
            newHp = Math.Max(1, (int)Math.Ceiling(character.MaxHp * (hpPct / 100.0)));
            newMp = Math.Max(0, (int)Math.Ceiling(character.MaxMp * (mpPct / 100.0)));
            character.ResurrectHpPercent = 1;
            character.ResurrectMpPercent = 1;
        }
        else
        {
            // Temple / return-point res — 10% with a hard floor of 1.
            newHp = Math.Max(1, (int)Math.Ceiling(character.MaxHp * 0.1));
            newMp = Math.Max(0, (int)Math.Ceiling(character.MaxMp * 0.1));
        }

        // Zone resurrection while World Hp is still 0 so leftover skill hits early-out.
        if (WorldIntegration.ZoneAuthority)
        {
            WorldIntegration.RelayUnitResurrectionToZone?.Invoke(
                character.ObjId, rx, ry, rz, rrot);
            WorldIntegration.RelayCombatClearedToZone?.Invoke(character.ObjId);
        }

        // Debuffs first: OnStarted conversions clamp drain to current HP (0 → no-op).
        ApplyRevivalDebuffs(character, inPlace);

        character.Hp = newHp;
        character.Mp = newMp;
        // Safety: never exit revive dead.
        if (character.Hp <= 0)
            character.Hp = Math.Max(1, newHp);

        if (WorldIntegration.ZoneAuthority)
            WorldIntegration.RelayUnitPointsToZone?.Invoke(character.ObjId, character.Hp, character.Mp);

        character.BroadcastPacket(
            new SCCharacterResurrectedPacket(character.ObjId, rx, ry, rz, rrot),
            true
        );

        character.BroadcastPacket(
            new SCUnitPointsPacket(character.ObjId, character.Hp, character.Mp),
            true
        );

        if (character.Hp > 0)
            character.PostUpdateCurrentHp(character, oldHp, character.Hp, KillReason.Unknown);

        Logger.Info(
            "ResurrectCharacter {0} kind={1} whereFallen={2} hp={3}/{4} mp={5}/{6} pos=({7:F1},{8:F1},{9:F1})",
            character.Name, kind, whereFallen, character.Hp, character.MaxHp, character.Mp, character.MaxMp,
            rx, ry, rz);

        // Defendant resurrections get the courthouse state and the imprison-or-trial offer here,
        // after the vitals are restored.
        JusticeManager.Instance.OnResurrectionFinished(character);
    }

    /// <summary>
    /// Where a resurrection puts the character: the world spot the client and the zone are told, and the spot
    /// <c>SetPosition</c> writes, which is the LOCAL transform. A return point is a world spot (unchanged behaviour).
    /// Without one - coming back where it fell - the character keeps its own transform: a passenger who died on a
    /// ship's deck stays parented to the ship, and writing its world coordinates into the local transform would apply
    /// the ship's offset twice.
    /// </summary>
    internal static (Vector3 World, float WorldYaw, Vector3 Local, float LocalYaw) ResurrectionSpot(
        Portal portal, Transform transform)
    {
        if (portal != null && (portal.X != 0f || portal.Y != 0f))
        {
            var returnPoint = new Vector3(portal.X, portal.Y, portal.Z);
            return (returnPoint, portal.ZRot, returnPoint, portal.ZRot);
        }

        var world = transform.World;
        var local = transform.Local;
        return (world.Position, world.Rotation.Z, local.Position, local.Rotation.Z);
    }

    /// <summary>
    /// Apply post-revive debuffs based on the death context:
    ///   inPlace (player-res) → no debuffs at all
    ///   DiedInPvpWarZone     → Leech + 5 min Respawn-CD
    ///   DiedInPvp            → 5 min Respawn-CD only (no Weakened Body)
    ///   PvE death            → Weakened Body + 5 min Respawn-CD
    /// </summary>
    private static void ApplyRevivalDebuffs(Character character, bool inPlace)
    {
        if (inPlace)
        {
            // Player-resurrected (e.g. by another player's resurrect skill): no debuffs.
            character.DiedInPvpWarZone = false;
            character.DiedInPvp = false;
            return;
        }

        var casterObj = new SkillCasterUnit(character.ObjId);

        if (character.DiedInPvpWarZone)
        {
            // PvP death in War zone → Leech + Respawn-CD
            character.DiedInPvpWarZone = false;
            character.DiedInPvp = false;
            ApplyBuff(character, casterObj, (uint)BuffConstants.WarZoneLeech);
            ApplyBuff(character, casterObj, (uint)BuffConstants.RespawnCooldown, RespawnCooldownDurationMs);
        }
        else if (character.DiedInPvp)
        {
            // PvP death outside War zone → Respawn-CD only (no Weakened Body)
            character.DiedInPvp = false;
            ApplyBuff(character, casterObj, (uint)BuffConstants.RespawnCooldown, RespawnCooldownDurationMs);
        }
        else
        {
            // PvE death → Weakened Body + Respawn-CD
            ApplyBuff(character, casterObj, (uint)BuffConstants.WeakenedBody);
            ApplyBuff(character, casterObj, (uint)BuffConstants.RespawnCooldown, RespawnCooldownDurationMs);
        }
    }

    private static void ApplyBuff(Character character, SkillCasterUnit casterObj, uint buffId, int forcedDurationMs = 0)
    {
        var template = SkillManager.Instance.GetBuffTemplate(buffId);
        if (template == null)
            return;

        var buff = new Buff(character, character, casterObj, template, null, DateTime.UtcNow);
        if (forcedDurationMs > 0)
            character.Buffs.AddBuff(buff, forcedDuration: forcedDurationMs);
        else
            character.Buffs.AddBuff(buff);
    }
}
