using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

// world_message_effects: a world notice; `message` is a text key the client resolves. zone_group_only
// rows reach everyone in the caster's zone group.
public class WorldMessageEffect : EffectTemplate
{
    public bool ZoneGroupOnly { get; set; }
    public string Message { get; set; }
    public bool ZoneGroupWarState { get; set; }
    public int FactionScopeId { get; set; }
    public int KillStreakCount { get; set; }
    public bool KillHero { get; set; }
    public string IconKey { get; set; }
    public bool ChatMsg { get; set; }
    public bool NameWithForeignWorld { get; set; }

    public override bool OnActionTime => false;

    private static uint ZoneGroupOf(BaseUnit unit) =>
        ZoneManager.Instance.GetZoneByKey(unit.Transform.ZoneId)?.GroupId ?? 0;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        if (string.IsNullOrEmpty(Message))
            return;

        // messageType bit: 1 = also surface in chat. ChatMsg drives it; source 0 = generic world message.
        var packet = new SCWorldMessagePacket(0, ChatMsg ? (byte)1 : (byte)0, Message);
        if (packetBuilder != null)
        {
            packetBuilder.AddPacket(packet);
            return;
        }

        if (ZoneGroupOnly && caster.ParentWorld != null)
        {
            var casterGroup = ZoneGroupOf(caster);
            foreach (var player in WorldMessageAudience.ZoneGroupMembers(
                         caster.ParentWorld.GetAllCharacters(), casterGroup, ZoneGroupOf))
                player.SendPacket(packet);
            return;
        }

        // faction_scope_id / kill_streak_count / kill_hero have no scoped channel yet: visible range only.
        caster.BroadcastPacket(packet, true);
    }
}
