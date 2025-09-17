using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Utilities;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(MovementOrder), "Tick")]
    public class Patch_MovementOrder
    {
        public static bool GetSubstituteOrder_Prefix(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget && formation.TargetFormation != null && CommandSystemConfig.Get().AttackSpecificFormation &&
                !CommandSystemSubModule.IsRealisticBattleModuleInstalled && !formation.IsAIControlled)
            {
                if (CommandSystemConfig.Get().BehaviorAfterCharge == BehaviorAfterCharge.Hold)
                {
                    var position = formation.CachedMedianPosition;
                    position.SetVec2(formation.CurrentPosition);
                    if (formation.Team == Mission.Current.PlayerTeam && formation.PlayerOwner == Agent.Main)
                    {
                        Utility.DisplayFormationReadyMessage(formation);
                    }
                    __result = MovementOrder.MovementOrderMove(position);
                    return false;
                }
            }

            return true;
        }
    }
}
