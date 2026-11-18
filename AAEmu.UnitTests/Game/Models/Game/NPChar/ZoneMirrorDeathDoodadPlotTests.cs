using System.Reflection;
using AAEmu.Game;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.Skills.Plots.Tree;
using AAEmu.Game.Models.Game.Skills.Plots.Type;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.World;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

[NotInParallel]
public class ZoneMirrorDeathDoodadPlotTests
{
    private SingletonScope<NpcGameData> _npcScope;
    private SingletonScope<SkillManager> _skillScope;
    private bool _oldAuthority;
    private Npc _npc;
    private List<NpcSkill> _rows;
    private SkillTemplate _skill;
    private PlotEventTemplate _event;
    private InteractionEffect _effect;
    private int _executions;

    [Before(Test)]
    public void Setup()
    {
        _oldAuthority = WorldIntegration.ZoneAuthority;
        WorldIntegration.ZoneAuthority = true;
        var data = new NpcGameData();
        _rows = [new NpcSkill { OwnerId = 101, SkillId = 201, SkillUseCondition = SkillUseConditionKind.OnDeath }];
        ((Dictionary<uint, List<NpcSkill>>)typeof(NpcGameData).GetProperty("SkillsForNpc", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(data)!)[101] = _rows;
        _npcScope = new SingletonScope<NpcGameData>(data);
        _event = new PlotEventTemplate { Id = 401, PlotId = 301, Tickets = 1,
            SourceUpdateMethodId = (uint)PlotSourceUpdateMethodType.PreviousSource,
            TargetUpdateMethodId = (uint)PlotTargetUpdateMethodType.Area, TargetUpdateMethodParam2 = 0 };
        _event.Effects.AddLast(new PlotEventEffect { ActualId = 501, ActualType = "InteractionEffect",
            SourceId = PlotEffectSource.OriginalSource, TargetId = PlotEffectTarget.Location });
        _skill = new SkillTemplate { Id = 201, Plot = new Plot { Id = 301, Tree = new PlotTree(301) { RootNode = new PlotNode { Event = _event } } } };
        _effect = new InteractionEffect { Id = 501, WorldInteraction = WorldInteractionType.SummonDoodad, DoodadId = 601 };
        var manager = TestManagers.CreateSkillManager();
        Set(manager, "_skills", new Dictionary<uint, SkillTemplate> { [201] = _skill });
        Set(manager, "_effects", new Dictionary<string, Dictionary<uint, EffectTemplate>> { ["InteractionEffect"] = new() { [501] = _effect } });
        _skillScope = new SingletonScope<SkillManager>(manager);
        _npc = new Npc { TemplateId = 101, Template = new NpcTemplate { Id = 101 }, ObjId = 77, IsZoneMirror = true, Hp = 0 };
        _executions = 0;
    }

    [After(Test)]
    public void Cleanup()
    {
        WorldIntegration.ZoneAuthority = _oldAuthority;
        _skillScope.Dispose();
        _npcScope.Dispose();
    }

    // Reflection also lets the exact unpatched release run the behavioral negative control.
    private int Run() => (int?)typeof(Npc).GetMethod("RunZoneDeathDoodadPlots", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_npc,
        new object[] { (Action<Npc, SkillTemplate>)((corpse, template) => { if (corpse == _npc && template.Id == 201) Interlocked.Increment(ref _executions); }) }) ?? 0;

    [Test]
    public async Task NativeDeathObjectRunsOnceForTheCorpse()
    {
        await Assert.That(Run()).IsEqualTo(1);
        await Assert.That(Run()).IsEqualTo(0);
        await Assert.That(_executions).IsEqualTo(1);
    }

    [Test]
    public async Task DuplicateNativeRowsDoNotDoubleTheBottle()
    {
        _rows.Add(_rows[0]);
        await Assert.That(Run()).IsEqualTo(1);
        await Assert.That(_executions).IsEqualTo(1);
    }

    [Test]
    public async Task RespawnAllowsOneNewDeathDrop()
    {
        Run();
        typeof(Npc).GetMethod("ResetZoneDeathDoodadPlots", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_npc, null);
        await Assert.That(Run()).IsEqualTo(1);
        await Assert.That(_executions).IsEqualTo(2);
    }

    [Test]
    public async Task ConcurrentDeathNotificationsProduceOneDrop()
    {
        Parallel.For(0, 16, _ => Run());
        await Assert.That(_executions).IsEqualTo(1);
    }

    [Test]
    public async Task LivingNpcDoesNotConsumeTheDeathHook()
    {
        _npc.Hp = 1;
        await Assert.That(Run()).IsEqualTo(0);
        _npc.Hp = 0;
        await Assert.That(Run()).IsEqualTo(1);
    }

    [Test]
    public async Task LegacySpawnerKeepsItsOwnDeathEvents()
    {
        _npc.Spawner = new NpcSpawner();
        await Assert.That(Run()).IsEqualTo(0);
        await Assert.That(_executions).IsEqualTo(0);
    }

    [Test]
    public async Task NonMirrorOrNonAuthoritativeWorldDoesNotExecute()
    {
        _npc.IsZoneMirror = false;
        await Assert.That(Run()).IsEqualTo(0);
        _npc.IsZoneMirror = true;
        WorldIntegration.ZoneAuthority = false;
        await Assert.That(Run()).IsEqualTo(0);
        WorldIntegration.ZoneAuthority = true;
        await Assert.That(Run()).IsEqualTo(1);
    }

    [Test]
    public async Task OrdinaryCombatSkillsRemainNative()
    {
        _rows[0].SkillUseCondition = SkillUseConditionKind.None;
        await Assert.That(Run()).IsEqualTo(0);
    }

    [Test]
    public async Task MixedEffectsAndConditionalGraphsAreExcluded()
    {
        _event.Conditions.AddLast(new PlotEventCondition());
        await Assert.That(Run()).IsEqualTo(0);
    }

    [Test]
    public async Task GraphWithAChildIsExcluded()
    {
        _skill.Plot.Tree.RootNode.Children.Add(new PlotNode { Event = _event });
        await Assert.That(Run()).IsEqualTo(0);
    }

    [Test]
    public async Task NonObjectInteractionIsExcluded()
    {
        _effect.WorldInteraction = WorldInteractionType.Looting;
        await Assert.That(Run()).IsEqualTo(0);
    }

    [Test]
    public async Task CombatAreaTargetSearchIsExcluded()
    {
        _event.TargetUpdateMethodParam2 = 1;
        await Assert.That(Run()).IsEqualTo(0);
    }

    private sealed class RecordingDrop : InteractionEffect
    {
        public int Calls;
        public float Offset;
        public ushort Timeline;
        public override void Apply(AAEmu.Game.Models.Game.Units.BaseUnit caster, AAEmu.Game.Models.Game.Skills.SkillCaster casterObj,
            AAEmu.Game.Models.Game.Units.BaseUnit target, AAEmu.Game.Models.Game.Skills.SkillCastTarget targetObj,
            AAEmu.Game.Models.Game.Skills.CastAction castObj, AAEmu.Game.Models.Game.Skills.Effects.EffectSource source,
            AAEmu.Game.Models.Game.Skills.SkillObject skillObject, DateTime time, AAEmu.Game.Core.Packets.CompressedGamePackets packetBuilder = null)
        {
            Calls++;
            Offset = System.Numerics.Vector3.Distance(caster.Transform.World.Position, target.Transform.World.Position);
            Timeline = source.Skill.TlId;
        }
    }

    [Test]
    public async Task ActualGraphRunsOnCorpseKeepsNativeOffsetAndReleasesTimeline()
    {
        using var worldScope = TestDungeonWorld.InstallWorldManager();
        var world = TestDungeonWorld.CreateSizedWorld(5991, 0, 1, 1, 1);
        TestDungeonWorld.Attach(_npc, world);
        _npc.Transform.Local.SetPosition(20, 20, 10);
        _event.TargetUpdateMethodParam3 = 1000;
        var drop = new RecordingDrop { Id = 501, WorldInteraction = WorldInteractionType.SummonDoodad, DoodadId = 601 };
        Set(SkillManager.Instance, "_effects", new Dictionary<string, Dictionary<uint, EffectTemplate>> { ["InteractionEffect"] = new() { [501] = drop } });
        var skill = new AAEmu.Game.Models.Game.Skills.Skill(_skill);
        var method = typeof(AAEmu.Game.Models.Game.Skills.Skill).GetMethod("RunNpcDeathDoodadPlotAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null) await (Task)method.Invoke(skill, new object[] { _npc })!;
        await Assert.That(drop.Calls).IsEqualTo(1);
        await Assert.That(drop.Offset > 0.98f && drop.Offset < 1.01f).IsTrue();
        await Assert.That(drop.Timeline > 0).IsTrue();
        await Assert.That(skill.TlId).IsEqualTo((ushort)0);
        await Assert.That(_npc.ActivePlotState).IsNull();
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
