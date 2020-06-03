using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
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

        //public static bool GetPosition_Prefix(MovementOrder __instance, ref WorldPosition __result)
        //{
        //    if (__instance.OrderType == OrderType.ChargeWithTarget && __instance.TargetFormation.CountOfUnits > 0)
        //    {
        //        var targetFormation = __instance.TargetFormation;
        //        __result = targetFormation.CurrentPosition.ToVec3().ToWorldPosition(Mission.Current.Scene);
        //        Utility.DisplayMessage(__result.AsVec2.ToString());
        //        return false;
        //    }

        //    return true;
        //}

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
