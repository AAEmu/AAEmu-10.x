using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// Confirms an item purchase offered by the target doodad's current function.
/// </summary>
/// <remarks>
/// bc doodad, u32 <c>type</c> (<c>doodad_func_purchases.id</c>), bool <c>useAAPoint</c>. Sent by the
/// client's PurchaseCoinDlgTask (Gilda Star displays on Mirage Isle and other token purchases) after it
/// has checked the token count itself, so every refusal here is logged: the client shows nothing.
/// </remarks>
public class CSDoodadPurchaseItemPacket() : GamePacket(CSOffsets.CSDoodadPurchaseItemPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var doodadObjId = stream.ReadBc();
        var purchaseId = stream.ReadUInt32();
        var useAaPoint = stream.ReadBoolean();

        var character = Connection.ActiveChar;
        if (character == null)
            return;

        Logger.Debug("DoodadPurchaseItem doodad={0} purchase={1} useAAPoint={2} from {3}",
            doodadObjId, purchaseId, useAaPoint, character.Name);

        var doodad = character.ParentWorld?.GetDoodad(doodadObjId);
        if (doodad == null || !doodad.IsVisible)
        {
            Logger.Info("Doodad purchase {0} refused for {1}: doodad {2} is {3} in world {4}",
                purchaseId, character.Name, doodadObjId, doodad == null ? "unknown" : "hidden",
                character.ParentWorld);
            return;
        }

        lock (doodad)
        {
            var func = DoodadPurchaseRules.FindPurchaseFunc(doodad.CurrentFuncs, purchaseId);
            if (func == null ||
                DoodadManager.Instance.GetFuncTemplate(func.FuncId, func.FuncType) is not DoodadFuncPurchase purchase)
            {
                Logger.Info("Doodad purchase {0} refused for {1}: doodad {2} (template {3}) phase {4} offers no such purchase",
                    purchaseId, character.Name, doodad.ObjId, doodad.TemplateId, doodad.FuncGroupId);
                character.SendErrorMessage(ErrorMessageType.NoInteractionAvailable);
                return;
            }

            if (!DoodadFuncPurchase.HasPermission(character, doodad, func))
            {
                Logger.Info("Doodad purchase {0} refused for {1}: permission {2} on doodad template {3}",
                    purchaseId, character.Name, func.PermId, doodad.TemplateId);
                return;
            }

            if (func.SkillId > 0)
            {
                var skill = SkillManager.Instance.GetSkillTemplate(func.SkillId);
                if (skill == null)
                {
                    Logger.Warn("Doodad purchase {0} refused for {1}: func skill {2} is not loaded",
                        purchaseId, character.Name, func.SkillId);
                    return;
                }

                if (!DoodadPurchaseRules.IsWithinReach(
                        character.Transform.World.Position, doodad.Transform.World.Position, skill.MaxRange,
                        out var horizontal, out var vertical))
                {
                    Logger.Info("Doodad purchase {0} refused for {1}: {2:F1} m across, {3:F1} m up from doodad template {4} (skill {5} range {6})",
                        purchaseId, character.Name, horizontal, vertical, doodad.TemplateId, skill.Id, skill.MaxRange);
                    character.SendErrorMessage(ErrorMessageType.TooFarAway);
                    return;
                }
            }

            if (!purchase.TryPurchase(character, useAaPoint))
            {
                Logger.Info("Doodad purchase {0} refused for {1}: payment or bag check failed (item {2} x{3}, coin {4} x{5}, currency {6})",
                    purchaseId, character.Name, purchase.ItemId, purchase.Count, purchase.CoinItemId, purchase.CoinCount,
                    purchase.CurrencyId);
                return;
            }

            Logger.Info("Doodad purchase {0}: {1} bought item {2} x{3} from doodad template {4}",
                purchaseId, character.Name, purchase.ItemId, purchase.Count, doodad.TemplateId);
            doodad.CompleteDeferredFunc(character, func);
        }
    }
}
