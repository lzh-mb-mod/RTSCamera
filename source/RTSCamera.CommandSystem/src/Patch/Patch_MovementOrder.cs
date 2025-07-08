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
            if (__instance.OrderType == OrderType.Charge && formation.TargetFormation != null && CommandSystemConfig.Get().AttackSpecificFormation &&
                CommandSystemSubModule.IsRealisticBattleModuleNotInstalled && !formation.IsAIControlled)
            {
                if (CommandSystemConfig.Get().BehaviorAfterCharge == BehaviorAfterCharge.Hold)
                {
                    var position = formation.QuerySystem.MedianPosition;
                    position.SetVec2(formation.CurrentPosition);
                    if (formation.Team == Mission.Current.PlayerTeam && formation.PlayerOwner == Agent.Main)
                    {
                        Utility.DisplayFormationReadyMessage(formation);
                    }
                    __result = MovementOrder.MovementOrderMove(position);
                    return false;
                }
                else
                {

                    //if (formation.Team == Mission.Current.PlayerTeam && formation.PlayerOwner == Agent.Main)
                    //{
                    //    Utility.DisplayFormationReadyMessage(formation);
                    //    Utility.DisplayFormationChargeMessage(formation);
                    //}
                    return true;
                }
            }

            return true;
        }

        public static bool SetChargeBehaviorValues_Prefix(Agent unit)
        {
            // TODO: Need update
            //if (Utility.ShouldChargeToFormation(unit))
            //{
            //    UnitAIBehaviorValues.SetUnitAIBehaviorWhenChargeToFormation(unit);
            //    return false;
            //}

            return true;
        }
    }
}
