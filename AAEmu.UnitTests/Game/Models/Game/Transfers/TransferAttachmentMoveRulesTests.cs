using System.Numerics;

using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Transfers;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Transfers;

public class TransferAttachmentMoveRulesTests
{
    [Test]
    public async Task DoodadsNeedingVisibilitySync_IncludesMotorAndBoundedSeats()
    {
        var motor = new Transfer { ObjId = 10 };
        var boarding = new Transfer { ObjId = 20 };
        motor.Bounded = boarding;
        motor.AttachedDoodads.Add(new Doodad { ObjId = 100 });
        boarding.AttachedDoodads.Add(new Doodad { ObjId = 101 });
        boarding.AttachedDoodads.Add(new Doodad { ObjId = 102 });

        var ids = TransferAttachmentMoveRules.DoodadsNeedingVisibilitySync(motor)
            .Select(d => d.ObjId)
            .ToArray();

        await Assert.That(ids).IsEquivalentTo([100u, 101u, 102u]);
    }

    [Test]
    public async Task BodiesNeedingVisibilitySync_MotorThenBounded()
    {
        var motor = new Transfer { ObjId = 10 };
        var boarding = new Transfer { ObjId = 20 };
        motor.Bounded = boarding;

        var ids = TransferAttachmentMoveRules.BodiesNeedingVisibilitySync(motor)
            .Select(t => t.ObjId)
            .ToArray();

        await Assert.That(ids).IsEquivalentTo([10u, 20u]);
    }

    [Test]
    public async Task TryGetSeatLocal_PassengerSeats_MatchSpawnOffsets()
    {
        await Assert.That(TransferAttachmentMoveRules.TryGetSeatLocal(AttachPointKind.Passenger0, out var p0, out var y0))
            .IsTrue();
        await Assert.That(p0).IsEqualTo(new Vector3(0.00537476f, 5.7852f, 1.36648f));
        await Assert.That(y0).IsEqualTo(MathF.PI * 2f);

        await Assert.That(TransferAttachmentMoveRules.TryGetSeatLocal(AttachPointKind.Passenger1, out var p1, out var y1))
            .IsTrue();
        await Assert.That(p1).IsEqualTo(new Vector3(0.00537476f, 1.63614f, 1.36648f));
        await Assert.That(y1).IsEqualTo(0f);

        await Assert.That(TransferAttachmentMoveRules.TryGetSeatLocal(AttachPointKind.None, out _, out _))
            .IsFalse();
    }

    [Test]
    public async Task IsParentedTo_FalseWhenWireOrTransformParentMissing()
    {
        var boarding = new Transfer { ObjId = 20 };
        var seat = new Doodad { ObjId = 101, AttachPoint = AttachPointKind.Passenger0 };
        await Assert.That(TransferAttachmentMoveRules.IsParentedTo(seat, boarding)).IsFalse();

        seat.ParentObjId = boarding.ObjId;
        // Transform.Parent still null — wire alone is not enough.
        await Assert.That(TransferAttachmentMoveRules.IsParentedTo(seat, boarding)).IsFalse();
        await Assert.That(TransferAttachmentMoveRules.IsParentedTo(null, boarding)).IsFalse();
        await Assert.That(TransferAttachmentMoveRules.IsParentedTo(seat, null)).IsFalse();
    }

    [Test]
    public async Task Occupancy_IsOnTransfer_ParentOrGrandparent()
    {
        await Assert.That(TransferAttachmentMoveRules.IsOnTransfer(20, 0, 20)).IsTrue();
        await Assert.That(TransferAttachmentMoveRules.IsOnTransfer(99, 20, 20)).IsTrue();
        await Assert.That(TransferAttachmentMoveRules.IsOnTransfer(99, 10, 20)).IsFalse();
        await Assert.That(
            TransferAttachmentMoveRules.RecordedTransferIsStillReached(20, 20, 0)).IsTrue();
        await Assert.That(
            TransferAttachmentMoveRules.RecordedTransferIsStillReached(20, 99, 0)).IsFalse();
    }
}
