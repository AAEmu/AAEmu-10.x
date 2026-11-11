using System.Numerics;

using AAEmu.Commons.Network;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.Teleport;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSTeleportEndedPacket() : GamePacket(CSOffsets.CSTeleportEndedPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var x = Helpers.ConvertLongX(stream.ReadInt64());
        var y = Helpers.ConvertLongY(stream.ReadInt64());
        var z = stream.ReadSingle();
        var ori = stream.ReadBytes(16); // TODO example: 00000000 00000000 00000000 0000803F

        var me = Connection.ActiveChar;
        if (me == null)
            return;

        // Same-instance GM /move and SCTeleportUnit never send CSInstanceLoaded.
        // Clearing the movement lock without applying the arrival left World
        // Transform on the old cell, so later GM spawns used dry-land coords
        // while the client was already at the destination.
        // The lock is released here without giving up the landings the client was sent to (SkillTeleportLanding):
        // the ones this confirmation does not answer are still checked against the next one.
        if (!ReturnTeleportRules.ShouldApplyEndedPosition(x, y, z))
        {
            // The (0, 0, 0) a same-world load can answer with names no spot: ignored, and every landing still waits,
            // so a confirmation that follows it is checked as if this one had not come.
            me.ReleasePositionLockKeepingLandings();
            Logger.Warn("TeleportEnded ignored origin for {0} zone={1}", me.Name, me.Transform.ZoneId);
            return;
        }

        // Only a teleport the server made is confirmed: a landing in this world and instance waiting for it, or the
        // position lock a move of the server's own holds (a GM move, an instance load). Without either it is a
        // modified client naming a spot to be put at, and nothing is changed.
        if (!me.DisabledSetPosition && !me.TeleportLandings.Awaits(me.Transform.WorldId, me.Transform.InstanceId))
        {
            Logger.Warn("TeleportEnded refused for {0}: no teleport waits for it, reported ({1:0.0},{2:0.0},{3:0.0}) zone={4}",
                me.Name, x, y, z, me.Transform.ZoneId);
            return;
        }

        // The spot the client reports is checked against the landings it was sent to. Far from all of them, the
        // character goes where the server sent it.
        var tolerance = AppConfiguration.Instance.World.TeleportLandingToleranceMeters;
        var reported = new Vector3(x, y, z);
        var check = me.TeleportLandings.Take(me.Transform.WorldId, me.Transform.InstanceId, reported, tolerance,
            out var sent);
        me.ReleasePositionLockKeepingLandings();

        if (check == TeleportEndedCheck.FarOff)
        {
            SusManager.Instance.LogActivity(SusManager.CategoryCheating, me,
                $"Player {me.Name} confirmed a teleport landing at ({x:F1}, {y:F1}, {z:F1}), {Vector3.Distance(reported, sent):F1} m from the spot it was sent to ({sent.X:F1}, {sent.Y:F1}, {sent.Z:F1}) (max {tolerance:F1}), placed at the sent spot");
            (x, y, z) = (sent.X, sent.Y, sent.Z);
        }

        var rot = me.Transform.World.Rotation;
        me.SetPosition(x, y, z, rot.X, rot.Y, rot.Z);
        // Put back at the sent spot, the speed check starts from it: the landing reset the check, and the client's
        // next move from the spot it claimed has to be judged from where the server put it, not only remembered.
        if (check == TeleportEndedCheck.FarOff)
            SusManager.Instance.RestartAnalyzePlayerDeltaMovement(me);
        me.Transform.FinalizeTransform();
        Logger.Info("TeleportEnded applied {0} -> ({1:0.0},{2:0.0},{3:0.0}) zone={4}",
            me.Name, x, y, z, me.Transform.ZoneId);

        WorldManager.ResendVisibleObjectsToCharacter(me, clientDroppedVisibility: false);
    }
}
