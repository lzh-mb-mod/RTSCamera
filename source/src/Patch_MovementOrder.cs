using RTSCamera.QuerySystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(MovementOrder), "Tick")]
    public class Patch_MovementOrder
    {

        //public static void Tick_Postfix(MovementOrder __instance, Formation formation)
        //{
        //    if (__instance.OrderType == OrderType.ChargeWithTarget)
        //    {
        //        var targetFormation = __instance.TargetFormation;
        //        var position = targetFormation.QuerySystem.MedianPosition;
        //        //var direction = targetFormation.QuerySystem.MedianPosition.AsVec2 -
        //        //                formation.QuerySystem.AveragePosition;
        //        formation.SetPositioning(position);
        //    }
        //}

        //public static bool GetPosition_Prefix(MovementOrder __instance, ref WorldPosition __result, Formation f)
        //{
        //    if (__instance.OrderType == OrderType.ChargeWithTarget && __instance.TargetFormation.CountOfUnits > 0)
        //    {
        //        FormationQuerySystem myFormationQuerySystem = f.QuerySystem;
        //        Formation targetFormation = __instance.TargetFormation;
        //        FormationQuerySystem targetFormationQuerySystem = targetFormation.QuerySystem;
        //        if (targetFormationQuerySystem == null)
        //        {
        //            __result = f.OrderPosition;
        //            return false;
        //        }
        //        WorldPosition targetMedianPosition = targetFormationQuerySystem.MedianPosition;
        //        targetMedianPosition.SetVec2(QueryDataStore.Get(f).TargetPosition.Value);
        //        if (f.FiringOrder != FiringOrder.FiringOrderHoldYourFire && (myFormationQuerySystem.IsRangedFormation || myFormationQuerySystem.IsRangedCavalryFormation))
        //        {
        //            if (myFormationQuerySystem.IsRangedCavalryFormation)
        //            {
        //                if (targetMedianPosition.AsVec2.DistanceSquared(myFormationQuerySystem.AveragePosition) <=
        //                    myFormationQuerySystem.MissileRange * myFormationQuerySystem.MissileRange)
        //                {
        //                    Vec2 direction = (targetFormationQuerySystem.MedianPosition.AsVec2 - myFormationQuerySystem.AveragePosition)
        //                        .Normalized();
        //                    targetMedianPosition.SetVec2(targetMedianPosition.AsVec2 -
        //                                                 direction * myFormationQuerySystem.MissileRange + f.CurrentPosition -
        //                                                 myFormationQuerySystem.AveragePosition);
        //                }
        //            }
        //            else // querySystem.IsRangedFormation == true
        //            {
        //                if (targetMedianPosition.AsVec2.DistanceSquared(myFormationQuerySystem.AveragePosition) <=
        //                    myFormationQuerySystem.MissileRange * myFormationQuerySystem.MissileRange)
        //                {
        //                    targetMedianPosition = myFormationQuerySystem.MedianPosition;
        //                    targetMedianPosition.SetVec2(f.CurrentPosition);
        //                }
        //            }
        //        }
        //        //else
        //        //{
        //        //    Vec2 vec2 = (targetFormationQuerySystem.AveragePosition - f.QuerySystem.AveragePosition).Normalized();
        //        //    float num = 2;
        //        //    if ((double)targetFormationQuerySystem.FormationPower < (double)f.QuerySystem.FormationPower * 0.200000002980232)
        //        //        num = 0.1f;
        //        //    targetMedianPosition.SetVec2(targetMedianPosition.AsVec2 - vec2 * num);
        //        //}

        //        //targetMedianPosition.SetVec2(MBMath.Lerp(f.CurrentPosition, targetMedianPosition.AsVec2, 0.5f, 0.01f ));
        //        if (f.FormationIndex == FormationClass.HeavyCavalry)
        //        {
        //            Utility.DisplayMessage(targetMedianPosition.X.ToString() + ',' + targetMedianPosition.Y);
        //        }
        //        __result = targetMedianPosition;
        //        return false;
        //    }

        //    return true;
        //}

        public static bool GetSubstituteOrder_Prefix(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget)
            {
                var position = formation.QuerySystem.MedianPosition;
                position.SetVec2(formation.CurrentPosition);
                __result = MovementOrder.MovementOrderMove(position);
                return false;
            }

            return true;
        }

        public static bool SetChargeBehaviorValues_Prefix(Agent unit)
        {
            if (unit.Formation != null && unit.Formation.MovementOrder.OrderType == OrderType.ChargeWithTarget)
            {
                UnitAIBehaviorValues.SetUnitAIBehaviorWhenChargeToFormation(unit);
                return false;
            }

            return true;
        }

        //public static bool Get_MovementState_Prefix(MovementOrder __instance, ref object __result)
        //{
        //    var orderType = (OrderType) (typeof(MovementOrder).GetProperty("OrderType")?.GetValue(__instance) ??
        //                                 new OrderType());
        //    if (orderType == OrderType.ChargeWithTarget)
        //    {
        //        __result = Enum.ToObject(typeof(MovementOrder).GetNestedType("MovementStateEnum", BindingFlags.NonPublic), 1);
        //        return false;
        //    }

        //    return true;
        //}
    }
}
