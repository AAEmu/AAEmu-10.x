using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// The client learns a doodad's family from its create packet only; no later packet carries it. A doodad
/// that is already on screen therefore keeps the family its owner had when it was streamed. After a
/// committed join, leave, kick or disband, the owner's family-gated doodads are taken off their viewers'
/// clients and created again, as if those viewers had left and re-entered range.
/// </summary>
internal static class DoodadFamilyRefresh
{
    /// <summary>Resends the family-gated doodads of <paramref name="ownerIds"/> to everyone who sees them.</summary>
    public static void ResendOwnedDoodads(IReadOnlyCollection<uint> ownerIds, IWorldManager worlds,
        IDoodadManager doodads = null)
    {
        if (ownerIds == null || ownerIds.Count == 0)
            return;

        foreach (var world in worlds?.GetWorlds() ?? [])
        {
            var playerDoodads = world?.SpawnManager?.GetAllPlayerDoodads();
            if (playerDoodads == null)
                continue;

            foreach (var doodad in playerDoodads.ToArray())
            {
                if (!NeedsResend(doodad, ownerIds, doodads ??= DoodadManager.Instance))
                    continue;
                foreach (var viewer in WorldManager.GetAround<Character>(doodad))
                    Resend(doodad, viewer);
            }
        }
    }

    /// <summary>
    /// Only doodads with a FAMILY-permission func are sent again: that permission is what the client
    /// checks the doodad's family for, and re-creating every crop of the owner would interrupt other
    /// players' interactions with them for no visible change.
    /// </summary>
    internal static bool NeedsResend(Doodad doodad, IReadOnlyCollection<uint> ownerIds, IDoodadManager doodads)
    {
        if (doodad is not { IsVisible: true } || doodad.OwnerType == DoodadOwnerType.System ||
            doodad.OwnerId == 0 || !ownerIds.Contains(doodad.OwnerId))
            return false;

        foreach (var group in doodad.Template?.FuncGroups ?? [])
            foreach (var func in doodads.GetFuncsForGroup(group.Id) ?? [])
                if (func.PermId == (uint)DoodadFuncPermission.Family)
                    return true;

        return false;
    }

    /// <summary>
    /// Removes the doodad from the viewer's client and creates it again in the phase that viewer sees.
    /// Per-viewer server state is left alone, as it is when a viewer crosses a region border.
    /// </summary>
    internal static void Resend(Doodad doodad, Character viewer)
    {
        viewer.SendPacket(new SCDoodadRemovedPacket(doodad.ObjId));
        viewer.SendPacket(new SCDoodadCreatedPacket(doodad, doodad.GetPhaseFor(viewer)));
    }
}
