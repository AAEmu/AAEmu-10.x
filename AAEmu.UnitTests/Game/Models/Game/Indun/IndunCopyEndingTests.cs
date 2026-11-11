using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunCopyEndingTests
{
    private static readonly DateTime T0 = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly IndunZoneOption Island = new(202, 3, 1800, 180);

    [Test]
    public async Task ScriptEnd_WarnsOnceThenClosesOnce()
    {
        var ending = new IndunCopyEnding([180, 60, 10]);
        var scriptEnded = T0.AddSeconds(426);
        ending.MarkScriptEnded(scriptEnded);

        var step = ending.Step(Island, T0, scriptEnded);
        await Assert.That(step.Phase).IsEqualTo(IndunInstancePhase.End);
        await Assert.That(step.WarnSeconds).IsEqualTo(180);
        await Assert.That(ending.Step(Island, T0, scriptEnded.AddSeconds(1)).WarnSeconds).IsNull();

        var closing = ending.Step(Island, T0, scriptEnded.AddSeconds(180));
        await Assert.That(closing.Close).IsTrue();
        await Assert.That(ending.Step(Island, T0, scriptEnded.AddSeconds(181)).Close).IsFalse();
    }

    [Test]
    public async Task ScriptEnd_IsKeptFromTheFirstMark()
    {
        var ending = new IndunCopyEnding([180]);
        ending.MarkScriptEnded(T0.AddSeconds(426));
        ending.MarkScriptEnded(T0.AddSeconds(500));

        await Assert.That(ending.ScriptEndedUtc).IsEqualTo(T0.AddSeconds(426));
    }

    [Test]
    public async Task WithoutScriptEnd_PlayRunsItsFullBudget()
    {
        var ending = new IndunCopyEnding([180]);

        await Assert.That(ending.Step(Island, T0, T0.AddSeconds(500)).Phase).IsEqualTo(IndunInstancePhase.Play);
    }
}
