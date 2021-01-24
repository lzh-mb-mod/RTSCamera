﻿using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic.CombatAI;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(MovementOrder), "Tick")]
    public class Patch_MovementOrder
    {
        public static bool GetSubstituteOrder_Prefix(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget && CommandSystemConfig.Get().AttackSpecificFormation)
            {
                var position = formation.QuerySystem.MedianPosition;
                position.SetVec2(formation.CurrentPosition);
                if (formation.Team == Mission.Current.PlayerTeam)
                {
                    Utility.DisplayFormationReadyMessage(formation);
                }
                __result = MovementOrder.MovementOrderMove(position);
                return false;
            }

            return true;
        }

        public static bool SetChargeBehaviorValues_Prefix(Agent unit)
        {
            if (unit.Formation != null && unit.Formation.MovementOrder.OrderType == OrderType.ChargeWithTarget && CommandSystemConfig.Get().AttackSpecificFormation)
            {
                UnitAIBehaviorValues.SetUnitAIBehaviorWhenChargeToFormation(unit);
                return false;
            }

            return true;
        }
    }
}
