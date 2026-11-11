using System.Reflection;

using AAEmu.Commons.Network.Core;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.TodayAssignment;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;
using Microsoft.Extensions.DependencyInjection;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

/// <summary>
/// "The Missing Ally" (3766): Start component 15763 holds QuestActConAcceptNpc 3007 (npc 983) and a
/// complete_quest_context 3763 unit_req; Progress component 15765 holds the sphere objective. A GM
/// <c>/quest add</c> skips the Start gate on purpose, and the evaluation re-checked it, so the quest
/// sat at Start for good and the Progress acts (the sphere listener) never armed — before or after a
/// relog.
/// </summary>
[NotInParallel] // seeds the process-wide QuestManager, UnitRequirementsGameData and SingletonContainer
public sealed class QuestStartStepRequirementTests
{
    private const uint QuestId = 3766;
    private const uint StartComponentId = 15763;
    private const uint ProgressComponentId = 15765;
    private const uint AcceptNpcId = 983;
    private const uint PrerequisiteQuestId = 3763;

    private readonly Character _character;
    private ProgressRecordingAct _progressAct = null!;

    public QuestStartStepRequirementTests()
    {
        var session = Mock.Of<ISession>();
        var connection = new GameConnection(session.Object);
        _character = new Character(new UnitCustomModelParams()) { Id = 1006, Name = "Ook", Level = 44 };
        _character.Connection = connection;
        connection.ActiveChar = _character;
        _character.Quests = new CharacterQuests(_character);
    }

    [Before(Test)]
    public void Before() =>
        SingletonContainer.ServiceProvider = new ServiceCollection()
            .AddSingleton(new QuestManager(
                Mock.Of<ITaskManager>().Object,
                Mock.Of<IZoneManager>().Object))
            .BuildServiceProvider();

    [After(Test)]
    public void TearDown()
    {
        UnitRequirementsGameData.Instance.SetForTest();
        TodayQuestGameData.Instance.SetStepsForTest();
        SingletonContainer.ServiceProvider = null;
    }

    [Test]
    public async Task ForcedAdd_WithUnmetStartRequirement_AdvancesToProgressAndArmsItsActs()
    {
        var template = Seed(startRequirement: UnitReqsKindType.CompleteQuestContext);
        var quest = NewQuest(template);

        // What CharacterQuests.AddQuest does after its gates: StartQuest sets Start, then one evaluation.
        quest.Step = QuestComponentKind.Start;
        var advanced = quest.RunCurrentStep();

        await Assert.That(advanced).IsTrue();
        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Progress);
        await Assert.That(_progressAct.InitializeActionCalls).IsEqualTo(1);
    }

    [Test]
    public async Task QuestSavedAtStart_AdvancesOnTheFirstEvaluationAfterLoad()
    {
        // A quest force-added before the fix was saved at Start; the load restores that step.
        var template = Seed(startRequirement: UnitReqsKindType.CompleteQuestContext);
        var quest = NewQuest(template);
        quest.Step = QuestComponentKind.Start;
        await Assert.That(_progressAct.InitializeActionCalls).IsEqualTo(0);

        quest.RunCurrentStep();

        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Progress);
        await Assert.That(_progressAct.InitializeActionCalls).IsEqualTo(1);
    }

    [Test]
    public async Task StartStillNeedsItsAcceptAct()
    {
        // The unit_reqs no longer hold Start, but the accept acts still do: a different acceptor stays put.
        var template = Seed(startRequirement: null);
        var quest = NewQuest(template, acceptorId: AcceptNpcId + 1);
        quest.Step = QuestComponentKind.Start;

        var advanced = quest.RunCurrentStep();

        await Assert.That(advanced).IsFalse();
        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Start);
        await Assert.That(_progressAct.InitializeActionCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ProgressComponentRequirements_StillGateTheObjective()
    {
        var template = Seed(startRequirement: null, progressRequirement: UnitReqsKindType.CompleteQuestContext);
        var quest = NewQuest(template);
        quest.Step = QuestComponentKind.Start;
        quest.RunCurrentStep();
        _progressAct.Met = true;

        var advanced = quest.RunCurrentStep();

        await Assert.That(advanced).IsFalse();
        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Progress);
    }

    [Test]
    [Arguments(QuestComponentKind.Start, false)]
    [Arguments(QuestComponentKind.Progress, true)]
    [Arguments(QuestComponentKind.Ready, true)]
    [Arguments(QuestComponentKind.Supply, true)]
    public async Task OnlyStartRequirementsAreLeftToTheAcceptGate(QuestComponentKind step, bool gates)
    {
        await Assert.That(QuestStartRequirementRules.GatesAcceptedQuest(step)).IsEqualTo(gates);
    }

    private Quest NewQuest(QuestTemplate template, uint acceptorId = AcceptNpcId)
    {
        var tickManager = Mock.Of<ITickManager>();
        tickManager.OnTick.Returns(new TickManager.TickEventHandler());
        var quest = new Quest(
            template,
            _character,
            Mock.Of<IQuestManager>().Object,
            new TaskManager(tickManager.Object),
            Mock.Of<ISkillManager>().Object,
            Mock.Of<IExpressTextManager>().Object,
            Mock.Of<IWorldManager>().Object)
        {
            Status = QuestStatus.Progress,
            QuestAcceptorType = QuestAcceptorType.Npc,
            AcceptorId = acceptorId
        };
        _character.Quests.ActiveQuests[QuestId] = quest;
        return quest;
    }

    private QuestTemplate Seed(UnitReqsKindType? startRequirement, UnitReqsKindType? progressRequirement = null)
    {
        var template = new QuestTemplate { Id = QuestId };

        var start = new QuestComponentTemplate(template) { Id = StartComponentId, KindId = QuestComponentKind.Start };
        start.ActTemplates.Add(new QuestActConAcceptNpc(start) { ActId = 21405, DetailId = 3007, NpcId = AcceptNpcId });

        var progress = new QuestComponentTemplate(template) { Id = ProgressComponentId, KindId = QuestComponentKind.Progress };
        _progressAct = new ProgressRecordingAct(progress) { ActId = 21410, Count = 1, ThisComponentObjectiveIndex = 0 };
        progress.ActTemplates.Add(_progressAct);

        template.Components[start.Id] = start;
        template.Components[progress.Id] = progress;
        ComponentTemplates()[start.Id] = start;
        ComponentTemplates()[progress.Id] = progress;

        var rows = new List<UnitReqs>();
        if (startRequirement is { } startKind)
            rows.Add(Row(1, StartComponentId, startKind, PrerequisiteQuestId));
        if (progressRequirement is { } progressKind)
            rows.Add(Row(2, ProgressComponentId, progressKind, PrerequisiteQuestId));
        UnitRequirementsGameData.Instance.SetForTest(rows.ToArray());

        return template;
    }

    private static UnitReqs Row(uint id, uint componentId, UnitReqsKindType kind, uint value1) =>
        new()
        {
            Id = id,
            OwnerId = componentId,
            OwnerType = "QuestComponent",
            KindType = kind,
            Value1 = value1,
            DisplayMessage = true
        };

    private static Dictionary<uint, QuestComponentTemplate> ComponentTemplates() =>
        (Dictionary<uint, QuestComponentTemplate>)typeof(QuestManager)
            .GetField("_componentTemplates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(QuestManager.Instance)!;

    /// <summary>Stands in for QuestActObjSphere, whose InitializeAction needs a live world.</summary>
    private sealed class ProgressRecordingAct(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
    {
        public override bool CountsAsAnObjective => true;
        public int InitializeActionCalls { get; private set; }
        public bool Met { get; set; }

        public override void InitializeAction(Quest quest, QuestAct questAct) => InitializeActionCalls++;

        public override void FinalizeAction(Quest quest, QuestAct questAct)
        {
        }

        public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount) => Met;
    }
}
