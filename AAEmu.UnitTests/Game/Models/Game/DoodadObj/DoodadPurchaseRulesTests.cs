using System.Numerics;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadPurchaseRulesTests
{
    // Mirage Isle "Improved Glider Design" display: doodad_funcs 11585, purchase 260, skill 14569 (4 m).
    private static DoodadFunc GliderDesign() => new()
    {
        FuncKey = 11585,
        FuncId = 260,
        FuncType = nameof(DoodadFuncPurchase),
        SkillId = 14569,
        NextPhase = -1,
    };

    [Test]
    public async Task FindPurchaseFunc_MatchesPurchaseId()
    {
        var func = GliderDesign();
        await Assert.That(DoodadPurchaseRules.FindPurchaseFunc([func], 260)).IsSameReferenceAs(func);
    }

    [Test]
    public async Task FindPurchaseFunc_AcceptsDoodadFuncRowKey()
    {
        var func = GliderDesign();
        await Assert.That(DoodadPurchaseRules.FindPurchaseFunc([func], 11585)).IsSameReferenceAs(func);
    }

    [Test]
    public async Task FindPurchaseFunc_PrefersPurchaseIdOverAnotherFuncsRowKey()
    {
        var byKey = new DoodadFunc { FuncKey = 300, FuncId = 1, FuncType = nameof(DoodadFuncPurchase) };
        var byId = new DoodadFunc { FuncKey = 999, FuncId = 300, FuncType = nameof(DoodadFuncPurchase) };
        await Assert.That(DoodadPurchaseRules.FindPurchaseFunc([byKey, byId], 300)).IsSameReferenceAs(byId);
    }

    [Test]
    public async Task FindPurchaseFunc_IgnoresOtherFuncTypes()
    {
        var timer = new DoodadFunc { FuncKey = 1, FuncId = 260, FuncType = "DoodadFuncTimer" };
        await Assert.That(DoodadPurchaseRules.FindPurchaseFunc([timer], 260)).IsNull();
        await Assert.That(DoodadPurchaseRules.FindPurchaseFunc(null, 260)).IsNull();
    }

    [Test]
    public async Task IsWithinReach_AcceptsBuyerAtTheDisplayEdge()
    {
        // Standing at the carpet edge, 5.5 m from the display origin and 1.6 m above it:
        // the old 3D check against 4 m refused this.
        var doodad = new Vector3(3747.986f, 4689.182f, 144.094f);
        var buyer = new Vector3(3747.986f + 5.5f, 4689.182f, 145.7f);
        await Assert.That(DoodadPurchaseRules.IsWithinReach(buyer, doodad, 4f, out var horizontal, out _)).IsTrue();
        await Assert.That(horizontal).IsEqualTo(5.5f).Within(0.01f);
    }

    [Test]
    public async Task IsWithinReach_RefusesBuyerFarAway()
    {
        var doodad = new Vector3(3747.986f, 4689.182f, 144.094f);
        var buyer = new Vector3(3689.6f, 4560.3f, 154.3f);
        await Assert.That(DoodadPurchaseRules.IsWithinReach(buyer, doodad, 4f, out _, out _)).IsFalse();
    }

    [Test]
    public async Task IsWithinReach_RefusesBuyerOnAnotherFloor()
    {
        var doodad = new Vector3(100f, 100f, 100f);
        var buyer = new Vector3(101f, 100f, 100f + DoodadPurchaseRules.VerticalReach + 1f);
        await Assert.That(DoodadPurchaseRules.IsWithinReach(buyer, doodad, 4f, out _, out _)).IsFalse();
    }

    [Test]
    public async Task IsWithinReach_SlackIsTheLimit()
    {
        var doodad = Vector3.Zero;
        var limit = 4f + DoodadPurchaseRules.ReachSlack;
        await Assert.That(DoodadPurchaseRules.IsWithinReach(new Vector3(limit - 0.01f, 0, 0), doodad, 4f, out _, out _)).IsTrue();
        await Assert.That(DoodadPurchaseRules.IsWithinReach(new Vector3(limit + 0.01f, 0, 0), doodad, 4f, out _, out _)).IsFalse();
    }
}
