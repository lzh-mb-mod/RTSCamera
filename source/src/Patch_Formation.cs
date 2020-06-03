using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        public static bool LeaveDetachment_Prefix(
            Formation __instance,
            List<IDetachment> ____detachments,
            IDetachment detachment)
        {
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;

            foreach (Agent agent in detachment.Agents.Where<Agent>((Func<Agent, bool>)(a => a.Formation == __instance && a.IsAIControlled)).ToList<Agent>())
            {
                detachment.RemoveAgent(agent);
                typeof(Formation).GetMethod("AttachUnit", bindingAttr).Invoke(__instance, new object[] { agent });
            }

            ____detachments.Remove(detachment);
            var detachmentManager = (DetachmentManager) typeof(Team).GetProperty("DetachmentManager", bindingAttr)
                ?.GetValue(__instance.Team);
            typeof(DetachmentManager).GetMethod("OnFormationLeaveDetachment", bindingAttr).Invoke(detachmentManager, new object[2]
            {
                __instance,
                detachment
            });
            return false;
        }

        //public static bool GetOrderPositionOfUnit_Prefix(Formation __instance, Agent unit, List<Agent> ___detachedUnits, ref WorldPosition __result)
        //{
        //    if (!___detachedUnits.Contains(unit) && __instance.MovementOrder.OrderType == OrderType.ChargeWithTarget)
        //    {

        //        __result = (WorldPosition)(typeof(Formation)
        //            .GetMethod("GetOrderPositionOfUnitAux", BindingFlags.Instance | BindingFlags.NonPublic)?
        //            .Invoke(__instance, new object[] {unit}) ?? new WorldPosition());
        //        return false;
        //    }

        //    return true;
        //}
    }
}
