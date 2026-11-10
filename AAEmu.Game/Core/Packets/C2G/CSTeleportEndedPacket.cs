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

        // The spot the client reports is checked against the landings it was sent to (SkillTeleportLanding), taken
        // before the unlock below gives them up. Far from all of them, the character goes where the server sent it.
        var applies = ReturnTeleportRules.ShouldApplyEndedPosition(x, y, z);
        var tolerance = AppConfiguration.Instance.World.TeleportLandingToleranceMeters;
        var reported = new Vector3(x, y, z);
        var sent = reported;
        var check = applies
            ? me.TeleportLandings.Take(me.Transform.WorldId, me.Transform.InstanceId, reported, tolerance, out sent)
            : TeleportEndedCheck.NoneExpected;

        // Same-instance GM /move and SCTeleportUnit never send CSInstanceLoaded.
        // Clearing the movement lock without applying the arrival left World
        // Transform on the old cell, so later GM spawns used dry-land coords
        // while the client was already at the destination.
        me.DisabledSetPosition = false;
        if (!applies)
        {
            Logger.Warn("TeleportEnded ignored origin for {0} zone={1}", me.Name, me.Transform.ZoneId);
            return;
        }

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
