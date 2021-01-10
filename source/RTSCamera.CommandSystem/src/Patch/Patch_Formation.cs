using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RTSCamera.Logic.SubLogic.Component;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        public static bool GetOrderPositionOfUnit_Prefix(Formation __instance, Agent unit, List<Agent> ___detachedUnits, ref WorldPosition __result)
        {
            if (!___detachedUnits.Contains(unit) &&
                __instance.MovementOrder.OrderType == OrderType.ChargeWithTarget)
            {
                var component = unit.GetComponent<RTSCameraAgentComponent>();
                if (component != null)
                {
                    __result = component.CurrentTargetPosition.Value;
                    return false;
                }
            }

            return true;
        }
    }
}
