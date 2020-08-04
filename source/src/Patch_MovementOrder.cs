using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

        public static bool GetPosition_Prefix(MovementOrder __instance, ref WorldPosition __result, Formation f)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget && __instance.TargetFormation.CountOfUnits > 0)
            {
                FormationQuerySystem querySystem = f.QuerySystem;
                FormationQuerySystem targetFormation = __instance.TargetFormation.QuerySystem;
                if (targetFormation == null)
                {
                    __result = f.OrderPosition;
                    return false;
                }
                WorldPosition targetMedianPosition = targetFormation.MedianPosition;
                if (querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation)
                {
                    if ((double)targetMedianPosition.AsVec2.DistanceSquared(querySystem.AveragePosition) <= (double)querySystem.MissileRange * (double)querySystem.MissileRange)
                    {
                        Vec2 direction = (targetFormation.MedianPosition.AsVec2 - querySystem.AveragePosition)
                            .Normalized();
                        targetMedianPosition.SetVec2(targetMedianPosition.AsVec2 - direction * querySystem.MissileRange);
                    }
                }
                else
                {
                    Vec2 vec2 = (targetFormation.AveragePosition - f.QuerySystem.AveragePosition).Normalized();
                    float num = 2f;
                    if ((double)targetFormation.FormationPower < (double)f.QuerySystem.FormationPower * 0.200000002980232)
                        num = 0.1f;
                    targetMedianPosition.SetVec2(targetMedianPosition.AsVec2 - vec2 * num);
                }
                __result = targetMedianPosition;
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
