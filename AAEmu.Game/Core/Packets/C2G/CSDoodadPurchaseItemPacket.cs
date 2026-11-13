using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;

namespace AAEmu.Game.Core.Packets.C2G;

/// <summary>
/// Confirms an item purchase offered by the target doodad's current function.
/// </summary>
/// <remarks>
/// bc doodad, u32 <c>type</c> (<c>doodad_func_purchases.id</c>), bool <c>useAAPoint</c>. Sent when the
/// player confirms the purchase dialog; for a purchase paid in an item, such as the Gilda Star displays on
/// Mirage Isle, that is the dialog the UI calls <c>DLG_TASK_PURCHASE_COIN</c>.
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

        var doodad = character.ParentWorld?.GetDoodad(doodadObjId);
        if (doodad == null || !doodad.IsVisible)
        {
            Logger.Debug("Doodad purchase {0} refused for {1}: doodad {2} is {3}",
                purchaseId, character.Name, doodadObjId, doodad == null ? "unknown" : "hidden");
            return;
        }

        lock (doodad)
        {
            var func = doodad.CurrentFuncs?.FirstOrDefault(candidate =>
                candidate.FuncId == purchaseId &&
                candidate.FuncType == nameof(DoodadFuncPurchase));
            if (func == null ||
                DoodadManager.Instance.GetFuncTemplate(func.FuncId, func.FuncType) is not DoodadFuncPurchase purchase)
            {
                Logger.Debug("Doodad purchase {0} refused for {1}: doodad {2} (template {3}) phase {4} offers no such purchase",
                    purchaseId, character.Name, doodad.ObjId, doodad.TemplateId, doodad.FuncGroupId);
                character.SendErrorMessage(ErrorMessageType.NoInteractionAvailable);
                return;
            }

            if (!DoodadFuncPurchase.HasPermission(character, doodad, func))
            {
                Logger.Debug("Doodad purchase {0} refused for {1}: permission {2} on doodad template {3}",
                    purchaseId, character.Name, func.PermId, doodad.TemplateId);
                return;
            }

            if (func.SkillId > 0)
            {
                var skill = SkillManager.Instance.GetSkillTemplate(func.SkillId);
                if (skill == null)
                {
                    Logger.Debug("Doodad purchase {0} refused for {1}: func skill {2} is not loaded",
                        purchaseId, character.Name, func.SkillId);
                    return;
                }

                // The func skill's range, as before. Only a purchase paid in an item the configuration
                // names gets a second look, with extra horizontal reach and the same bound on height.
                var buyerPosition = character.Transform.World.Position;
                var doodadPosition = doodad.Transform.World.Position;
                var distance = character.GetDistanceTo(doodad, true);
                var withinReach = distance <= skill.MaxRange ||
                    (DoodadPurchaseRules.TryGetExtraReach(
                         AppConfiguration.Instance.World.DoodadCoinPurchaseExtraReachMeters, purchase.CoinItemId,
                         out var extraReach) &&
                     DoodadPurchaseRules.IsWithinExtraReach(buyerPosition, doodadPosition, skill.MaxRange, extraReach));
                if (!withinReach)
                {
                    Logger.Debug("Doodad purchase {0} refused for {1}: {2:F1} m from doodad template {3}, {4:F1} m in height (skill {5} range {6}, coin item {7})",
                        purchaseId, character.Name, distance, doodad.TemplateId,
                        MathF.Abs(buyerPosition.Z - doodadPosition.Z), skill.Id, skill.MaxRange, purchase.CoinItemId);
                    character.SendErrorMessage(ErrorMessageType.TooFarAway);
                    return;
                }
            }

            if (!purchase.TryPurchase(character, useAaPoint))
            {
                Logger.Debug("Doodad purchase {0} refused for {1}: payment or bag check failed (item {2} x{3}, coin {4} x{5}, currency {6})",
                    purchaseId, character.Name, purchase.ItemId, purchase.Count, purchase.CoinItemId, purchase.CoinCount,
                    purchase.CurrencyId);
                return;
            }

            Logger.Debug("Doodad purchase {0}: {1} bought item {2} x{3} from doodad template {4}",
                purchaseId, character.Name, purchase.ItemId, purchase.Count, doodad.TemplateId);
            doodad.CompleteDeferredFunc(character, func);
        }
    }
}
