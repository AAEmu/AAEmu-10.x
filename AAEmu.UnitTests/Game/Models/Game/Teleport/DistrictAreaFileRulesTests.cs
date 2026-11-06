using System.Numerics;

using AAEmu.Game.Models.Game.Teleport;

namespace AAEmu.UnitTests.Game.Models.Game.Teleport;

public class DistrictAreaFileRulesTests
{
    // Shape of zone 204 (Salpimari) district.xml: Area Id is always 0, value1 names the district.
    private const string SalpimariExcerpt =
        """
        <Objects>
         <Entity Name="LevelDesignShape_204_anne_11" Pos="1700,2000,154" EntityClass="AreaShape" EntityId="9520" Layer="Main">
          <Area Id="0" Group="22" Proximity="0" Priority="0" value1="357" value2="0" flags="0" Height="0">
           <Points>
            <Point Pos="0,0,0"/>
            <Point Pos="100,0,0"/>
            <Point Pos="100,100,0"/>
            <Point Pos="0,100,0"/>
           </Points>
          </Area>
         </Entity>
         <Entity Name="LevelDesignShape_204_anne_5" Pos="966,2207,188" EntityClass="AreaShape" EntityId="9518" Layer="Main">
          <Area Id="0" Group="22" Proximity="0" Priority="0" value1="0" value2="0" flags="0" Height="0">
           <Points>
            <Point Pos="0,0,0"/>
            <Point Pos="10,0,0"/>
            <Point Pos="10,10,0"/>
           </Points>
          </Area>
         </Entity>
        </Objects>
        """;

    [Test]
    public async Task Parse_ReadsDistrictIdFromValue1_AndSkipsUnnamedShapes()
    {
        var districts = DistrictAreaFileRules.Parse(SalpimariExcerpt, 204);

        await Assert.That(districts.Count).IsEqualTo(1);
        await Assert.That(districts[0].DistrictId).IsEqualTo(357u);
        await Assert.That(districts[0].ZoneKey).IsEqualTo(204u);
        await Assert.That(districts[0].Points[2]).IsEqualTo(new Vector2(1800, 2100));
    }

    [Test]
    public async Task Parse_EmptyObjectsFile_GivesNoDistricts()
    {
        await Assert.That(DistrictAreaFileRules.Parse("<Objects />", 204).Count).IsEqualTo(0);
        await Assert.That(DistrictAreaFileRules.Parse("", 204).Count).IsEqualTo(0);
        await Assert.That(DistrictAreaFileRules.Parse(SalpimariExcerpt, 0).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_AppliesScaleAndRotation()
    {
        // Rotate is CryEngine w,x,y,z: a quarter turn about Z maps +X onto +Y.
        var half = MathF.Sqrt(0.5f);
        var xml =
            $"""
             <Objects>
              <Entity Name="s" Pos="100,200,0" Scale="2,2,2" Rotate="{half},0,0,{half}" EntityClass="AreaShape">
               <Area Id="0" Group="22" value1="7">
                <Points>
                 <Point Pos="0,0,0"/>
                 <Point Pos="10,0,0"/>
                 <Point Pos="10,10,0"/>
                </Points>
               </Area>
              </Entity>
             </Objects>
             """;

        var point = DistrictAreaFileRules.Parse(xml, 1)[0].Points[1];

        await Assert.That(MathF.Abs(point.X - 100)).IsLessThan(0.01f);
        await Assert.That(MathF.Abs(point.Y - 220)).IsLessThan(0.01f);
    }

    [Test]
    public async Task Index_FindsDistrictsContainingPoint_InThatZoneOnly()
    {
        var index = new DistrictAreaIndex();
        var square = new List<Vector2> { new(0, 0), new(100, 0), new(100, 100), new(0, 100) };
        var inner = new List<Vector2> { new(10, 10), new(20, 10), new(20, 20), new(10, 20) };
        index.Add(204, 357, square);
        index.Add(204, 511, inner);

        await Assert.That(index.DistrictsAt(204, 50, 50)).IsEquivalentTo(new[] { 357u });
        await Assert.That(index.DistrictsAt(204, 15, 15)).IsEquivalentTo(new[] { 357u, 511u });
        await Assert.That(index.DistrictsAt(204, 150, 50).Count).IsEqualTo(0);
        await Assert.That(index.DistrictsAt(205, 50, 50).Count).IsEqualTo(0);
        await Assert.That(index.ShapeCount).IsEqualTo(2);
    }

    [Test]
    public async Task Index_IgnoresDegenerateShapes()
    {
        var index = new DistrictAreaIndex();
        index.Add(204, 357, [new Vector2(0, 0), new Vector2(1, 1)]);
        index.Add(0, 357, [new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)]);
        index.Add(204, 0, [new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)]);

        await Assert.That(index.ShapeCount).IsEqualTo(0);
    }
}

public class DistrictVisitRulesTests
{
    [Test]
    public async Task VisitKey_RoundTrips_AndStaysClearOfSubZoneIds()
    {
        var key = DistrictVisitRules.ToVisitKey(357);

        await Assert.That(DistrictVisitRules.TryGetDistrictId(key, out var districtId)).IsTrue();
        await Assert.That(districtId).IsEqualTo(357u);
        // Subzone ids (compact sub_zones.id, ~1400 today) are not district keys.
        await Assert.That(DistrictVisitRules.TryGetDistrictId(1404, out _)).IsFalse();
        await Assert.That(DistrictVisitRules.TryGetDistrictId(DistrictVisitRules.DistrictVisitKeyBase, out _)).IsFalse();
        // portal_visited_district.subzone is a signed INT.
        await Assert.That(DistrictVisitRules.ToVisitKey(100_000) <= int.MaxValue).IsTrue();
    }

    private static readonly IReadOnlyList<(uint FactionId, uint ReturnPointId)> DiamondShores =
    [
        (101, 652), (103, 652), (104, 652), (109, 672), (113, 672), (161, 673)
    ];

    private static uint Mother(uint faction) => faction switch
    {
        101 or 103 or 104 or 200 => 148,
        109 or 112 or 113 or 187 or 199 => 149,
        161 => 114,
        _ => 0
    };

    [Test]
    public async Task PickReturnPoint_UsesOwnFactionRow()
    {
        await Assert.That(DistrictVisitRules.PickReturnPoint(DiamondShores, 101, Mother)).IsEqualTo(652u);
        await Assert.That(DistrictVisitRules.PickReturnPoint(DiamondShores, 109, Mother)).IsEqualTo(672u);
        await Assert.That(DistrictVisitRules.PickReturnPoint(DiamondShores, 161, Mother)).IsEqualTo(673u);
    }

    [Test]
    public async Task PickReturnPoint_FallsBackToSameMotherFaction()
    {
        // Warborn (112) has no Diamond Shores row; Harani-side rows share mother 149.
        await Assert.That(DistrictVisitRules.PickReturnPoint(DiamondShores, 112, Mother)).IsEqualTo(672u);
    }

    [Test]
    public async Task PickReturnPoint_NoneWhenDistrictIsNotTheFactions()
    {
        IReadOnlyList<(uint, uint)> nuiaOnly = [(101, 731), (103, 731)];

        await Assert.That(DistrictVisitRules.PickReturnPoint(nuiaOnly, 109, Mother)).IsEqualTo(0u);
        await Assert.That(DistrictVisitRules.PickReturnPoint(nuiaOnly, 999, Mother)).IsEqualTo(0u);
        await Assert.That(DistrictVisitRules.PickReturnPoint([], 101, Mother)).IsEqualTo(0u);
        await Assert.That(DistrictVisitRules.PickReturnPoint(null, 101, Mother)).IsEqualTo(0u);
    }

    [Test]
    public async Task Entered_ListsOnlyNewDistricts()
    {
        await Assert.That(DistrictVisitRules.Entered([], [357u, 511u])).IsEquivalentTo(new[] { 357u, 511u });
        await Assert.That(DistrictVisitRules.Entered([357u], [357u, 511u])).IsEquivalentTo(new[] { 511u });
        await Assert.That(DistrictVisitRules.Entered([357u, 511u], [357u]).Count).IsEqualTo(0);
        await Assert.That(DistrictVisitRules.Entered([357u], []).Count).IsEqualTo(0);
    }
}
