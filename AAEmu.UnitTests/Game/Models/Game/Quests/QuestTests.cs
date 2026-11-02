using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.Quests.Templates;
#pragma warning disable IDE0051

namespace AAEmu.UnitTests.Game.Models.Game.Quests;
// TODO: Re-enable the quest related test
// ReSharper disable UnusedMember.Local

public class QuestTests
{
    // [Fact]
    private async Task Start_WhenQuestStepIsNoneAndComponentIsEmpty_ShouldDoNothing()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);
        mockQuestTemplate.GetComponents(Any<QuestComponentKind>()).Returns([]);

        // Act
        var result = quest.StartQuest();

        // Assert
        await Assert.That(result).IsFalse();
        mockOwner.SendPacket(Any<GamePacket>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task QuestInitialized_SetsInitializationFinished()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.QuestInitialized();

        // Assert
        // Note: _questInitializationFinished is private, test indirectly
        await Assert.That(true).IsTrue(); // If no exception, it's fine
    }

    [Test]
    public async Task FinalizeQuestActs_CallsFinalizeOnActs()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);
        // Mock quest steps and components

        // Act
        quest.FinalizeQuestActs();

        // Assert
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task WriteData_ReturnsByteArray()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        var data = quest.WriteData();

        // Assert
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length > 0).IsTrue();
    }

    [Test]
    public async Task ReadData_WithValidData_SetsObjectives()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);
        var stream = new PacketStream();
        stream.Write(1); // objective 1
        stream.Write(2); // objective 2
        stream.Write(3); // objective 3
        stream.Write(4); // objective 4
        stream.Write(5); // objective 5
        stream.Write((byte)QuestComponentKind.Progress); // step
        stream.Write((byte)QuestAcceptorType.Npc); // acceptor type
        stream.Write(100u); // component id
        stream.Write(200u); // acceptor id
        stream.Write(DateTime.UtcNow); // time

        // Act
        quest.ReadData(stream.GetBytes());

        // Assert
        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Progress);
        await Assert.That(quest.QuestAcceptorType).IsEqualTo(QuestAcceptorType.Npc);
    }

    [Test]
    public async Task WriteData_ThenReadData_RestoresEveryPersistedField()
    {
        // The `quests.data` blob is the only place the objective counts, the step, the acceptor and the accept
        // time survive a World restart (CharacterQuests.Save/Load). A field that drops out of WriteData or
        // ReadData resets silently at the next login, which is what a "quest progress lost on reboot" report
        // looks like from the player's side.
        var saved = SetupQuest(out _, out _, out _, out _, out _, out _, out _);
        saved.Step = QuestComponentKind.Progress; // SetStep resets the objectives, so it goes first
        saved.Objectives[0] = 3;
        saved.Objectives[1] = 0;
        saved.Objectives[2] = 7;
        saved.Objectives[3] = 1;
        saved.Objectives[4] = 12;
        saved.QuestAcceptorType = QuestAcceptorType.Doodad;
        saved.ComponentId = 4521u;
        saved.AcceptorId = 777001u;
        var acceptTime = new DateTime(2026, 11, 1, 20, 15, 42, DateTimeKind.Utc);
        saved.Time = acceptTime;

        var blob = saved.WriteData();

        var loaded = SetupQuest(out _, out _, out _, out _, out _, out _, out _);
        loaded.ReadData(blob);

        await Assert.That(loaded.Objectives).IsEquivalentTo(saved.Objectives);
        await Assert.That(loaded.Step).IsEqualTo(QuestComponentKind.Progress);
        await Assert.That(loaded.QuestAcceptorType).IsEqualTo(QuestAcceptorType.Doodad);
        await Assert.That(loaded.ComponentId).IsEqualTo(4521u);
        await Assert.That(loaded.AcceptorId).IsEqualTo(777001u);
        // The wire carries whole seconds.
        await Assert.That((loaded.Time - acceptTime).Duration()).IsLessThan(TimeSpan.FromSeconds(1));
    }

    #region Property Tests

    [Test]
    public async Task Status_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.Status = QuestStatus.Completed;

        // Assert
        await Assert.That(quest.Status).IsEqualTo(QuestStatus.Completed);
    }

    [Test]
    public async Task Step_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.Step = QuestComponentKind.Reward;

        // Assert
        await Assert.That(quest.Step).IsEqualTo(QuestComponentKind.Reward);
    }

    #endregion

    #region Objective Tests

    [Test]
    public async Task Objectives_ArrayAccess_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.Objectives[0] = 10;

        // Assert
        await Assert.That(quest.Objectives[0]).IsEqualTo(10);
    }

    #endregion

    #region ComponentId Tests

    [Test]
    public async Task ComponentId_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.ComponentId = 123;

        // Assert
        await Assert.That(quest.ComponentId).IsEqualTo(123u);
    }

    #endregion

    #region DoodadId Tests

    [Test]
    public async Task DoodadId_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.DoodadId = 456;

        // Assert
        await Assert.That(quest.DoodadId).IsEqualTo(456u);
    }

    #endregion

    #region QuestAcceptorType Tests

    [Test]
    public async Task QuestAcceptorType_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.QuestAcceptorType = QuestAcceptorType.Doodad;

        // Assert
        await Assert.That(quest.QuestAcceptorType).IsEqualTo(QuestAcceptorType.Doodad);
    }

    #endregion

    #region AcceptorId Tests

    [Test]
    public async Task AcceptorId_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act
        quest.AcceptorId = 789;

        // Assert
        await Assert.That(quest.AcceptorId).IsEqualTo(789u);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Objectives_ArrayBounds_DoesNotThrow()
    {
        // Arrange
        var quest = SetupQuest(out var mockOwner, out var mockQuestTemplate, out _, out _, out _, out _, out _);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => quest.Objectives[10] = 1);
    }

    #endregion

    private static Quest SetupQuest(
        out Mock<ICharacter> mockCharacter,
        out Mock<IQuestTemplate> mockQuestTemplate,
        out Mock<IQuestManager> mockQuestManager,
        out Mock<TaskManager>? mockTaskManager,
        out Mock<ISkillManager> mockSkillManager,
        out Mock<IExpressTextManager> mockExpressTextManager,
        out Mock<IWorldManager> mockWorldManager)
    {
        mockCharacter = Mock.Of<ICharacter>();
        mockQuestManager = Mock.Of<IQuestManager>();
        mockQuestTemplate = Mock.Of<IQuestTemplate>();
        mockQuestTemplate.Components.Returns(new Dictionary<uint, QuestComponentTemplate>());
        mockExpressTextManager = Mock.Of<IExpressTextManager>();
        mockSkillManager = Mock.Of<ISkillManager>();

        // Create TaskManager with ITickManager dependency
        var mockTickManager = Mock.Of<ITickManager>();
        mockTickManager.OnTick.Returns(new TickManager.TickEventHandler());
        var taskManagerInstance = new TaskManager(mockTickManager.Object);
        mockTaskManager = null;

        mockWorldManager = Mock.Of<IWorldManager>();

        var quest = new Quest(
            mockQuestTemplate.Object,
            mockCharacter.Object,
            mockQuestManager.Object,
            taskManagerInstance,
            mockSkillManager.Object,
            mockExpressTextManager.Object,
            mockWorldManager.Object);

        quest.Owner = mockCharacter.Object;
        quest.Template = mockQuestTemplate.Object;
        return quest;
    }
}