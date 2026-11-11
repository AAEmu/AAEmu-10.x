using AAEmu.Game.Models.Game.Skills.Plots.Tree;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Plots;

public class PlotCasterDeathRulesTests
{
    [Test]
    public async Task APlotStops_WhenItsCasterDiesWhileItRuns()
    {
        // Songsong's OnSpawn plot 7127 loops 66104 -> 66104 every 12 s and kept summoning waves after it died.
        await Assert.That(PlotCasterDeathRules.StopsPlot(casterAliveAtStart: true, casterDeadNow: true)).IsTrue();
    }

    [Test]
    public async Task APlotKeepsRunning_WhileItsCasterLives()
    {
        await Assert.That(PlotCasterDeathRules.StopsPlot(casterAliveAtStart: true, casterDeadNow: false)).IsFalse();
    }

    [Test]
    public async Task AnOnDeathPlot_RunsForItsDeadCaster()
    {
        // Songsong's on_death 51183 starts after it died and must still wilt the flowers.
        await Assert.That(PlotCasterDeathRules.StopsPlot(casterAliveAtStart: false, casterDeadNow: true)).IsFalse();
    }
}
