using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

/// <summary>GM: reseed ZoneAuthority NPC→player aggro for hostiles in sight.</summary>
public class AggroSeed : ICommand
{
    public string[] CommandNames { get; set; } = ["aggroseed", "seedaggro"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp() => "";

    public string GetCommandHelpText() =>
        "Seeds aggressive NPCs in sight onto you (ZoneAuthority player-aggro handoff).";

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (!WorldIntegration.ZoneAuthority)
        {
            CommandManager.SendErrorText(this, messageOutput, "ZoneAuthority is off");
            return;
        }

        var around = WorldManager.GetAround<Npc>(
            character, ZonePlayerAggroRules.BaseSightMetres * 4f);
        var aggressive = around?.Count(n => n.Template?.Aggression == true) ?? 0;
        var attackable = around?.Count(n => n.Template?.Aggression == true && n.CanAttack(character)) ?? 0;
        var seeded = ZonePlayerAggro.TrySeedNearbyNpcsTowardPlayer(character);
        CommandManager.SendNormalText(
            this,
            messageOutput,
            $"around={around?.Count ?? 0} aggressive={aggressive} canAttack={attackable} seeded={seeded}");
    }
}
