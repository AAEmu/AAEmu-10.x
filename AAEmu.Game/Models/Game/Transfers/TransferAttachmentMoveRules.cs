using System.Numerics;

using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Transfers;

/// <summary>
/// Carriage seats are doodads parented to the boarding transfer. Move ticks update the motor and
/// boarding body but must also refresh those doodads' region membership, otherwise they stay
/// registered at the stop after the carriage has driven away.
/// Standing on the deck (no seat) uses the same occupancy keep-alive idea as ship hulls.
/// </summary>
public static class TransferAttachmentMoveRules
{
    /// <summary>Deck / boarding reach for adopting a transfer when gcId is the parent sentinel.</summary>
    public const float StandingHintRadiusMetres = 12f;

    public static float StandingHintRadiusSquared =>
        StandingHintRadiusMetres * StandingHintRadiusMetres;

    public static bool IsWithinStandingReach(float distanceSquared) =>
        distanceSquared <= StandingHintRadiusSquared;

    /// <summary>
    /// True when the character's parent (or that parent's parent) is <paramref name="transferObjId"/>.
    /// </summary>
    public static bool IsOnTransfer(uint parentObjId, uint parentOfParentObjId, uint transferObjId) =>
        transferObjId != 0 && (parentObjId == transferObjId || parentOfParentObjId == transferObjId);

    public static bool RecordedTransferIsStillReached(
        uint recordedObjId, uint parentObjId, uint parentOfParentObjId) =>
        IsOnTransfer(parentObjId, parentOfParentObjId, recordedObjId);

    /// <summary>
    /// Doodads that ride with this motor: its own bindings plus the bounded boarding part's seats.
    /// </summary>
    public static IEnumerable<Doodad> DoodadsNeedingVisibilitySync(Transfer motor)
    {
        if (motor == null)
            yield break;

        foreach (var doodad in EnumerateAttached(motor))
            yield return doodad;

        if (motor.Bounded != null)
        {
            foreach (var doodad in EnumerateAttached(motor.Bounded))
                yield return doodad;
        }
    }

    /// <summary>
    /// Transfers whose region/stream must be refreshed after a motor move (boarding body).
    /// </summary>
    public static IEnumerable<Transfer> BodiesNeedingVisibilitySync(Transfer motor)
    {
        if (motor == null)
            yield break;

        yield return motor;
        if (motor.Bounded != null)
            yield return motor.Bounded;
    }

    /// <summary>
    /// True when the seat is already parented to <paramref name="owner"/> for both transform and wire.
    /// </summary>
    public static bool IsParentedTo(Doodad doodad, Transfer owner) =>
        doodad != null
        && owner != null
        && doodad.ParentObjId == owner.ObjId
        && ReferenceEquals(doodad.Transform?.Parent, owner.Transform);

    /// <summary>
    /// Seat attach locals used when binding carriage chairs (Passenger0 / Passenger1).
    /// Same values TransferManager applies at spawn; kept here so a lost parent can be restored
    /// without baking world coordinates into Local.
    /// </summary>
    public static bool TryGetSeatLocal(AttachPointKind attachPoint, out Vector3 position, out float yaw)
    {
        switch (attachPoint)
        {
            case AttachPointKind.Passenger0:
                position = new Vector3(0.00537476f, 5.7852f, 1.36648f);
                yaw = MathF.PI * 2f;
                return true;
            case AttachPointKind.Passenger1:
                position = new Vector3(0.00537476f, 1.63614f, 1.36648f);
                yaw = 0f;
                return true;
            default:
                position = Vector3.Zero;
                yaw = 0f;
                return false;
        }
    }

    /// <summary>
    /// Re-parent a seat onto its boarding transfer and restore the seat-local offset.
    /// Returns true when a change was applied.
    /// </summary>
    public static bool EnsureParented(Doodad doodad, Transfer owner)
    {
        if (doodad == null || owner?.Transform == null)
            return false;
        if (IsParentedTo(doodad, owner))
            return false;
        if (!TryGetSeatLocal(doodad.AttachPoint, out var position, out var yaw))
            return false;

        doodad.Transform.Parent = owner.Transform;
        doodad.Transform.Local.SetPosition(position.X, position.Y, position.Z, 0f, 0f, yaw);
        doodad.ParentObjId = owner.ObjId;
        doodad.ParentObj = owner;
        return true;
    }

    /// <summary>
    /// Ensure every attached seat on <paramref name="body"/> is parented, then return them for
    /// visibility refresh.
    /// </summary>
    public static IEnumerable<Doodad> EnsureAttachedParented(Transfer body)
    {
        if (body?.AttachedDoodads == null)
            yield break;

        foreach (var doodad in body.AttachedDoodads)
        {
            if (doodad == null)
                continue;
            EnsureParented(doodad, body);
            yield return doodad;
        }
    }

    private static IEnumerable<Doodad> EnumerateAttached(Transfer transfer)
    {
        if (transfer?.AttachedDoodads == null)
            yield break;

        foreach (var doodad in transfer.AttachedDoodads)
        {
            if (doodad != null)
                yield return doodad;
        }
    }
}
