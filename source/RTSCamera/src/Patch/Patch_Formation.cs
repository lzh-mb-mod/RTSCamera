using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RTSCamera.Logic.SubLogic.Component;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        private static readonly MethodInfo AttachUnit =
            typeof(Formation).GetMethod("AttachUnit", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo DetachmentManager =
            typeof(Team).GetProperty("DetachmentManager", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo OnFormationLeaveDetachment =
            typeof(DetachmentManager).GetMethod("OnFormationLeaveDetachment",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo GetOrderPositionOfUnitAux =
            typeof(Formation).GetMethod("GetOrderPositionOfUnitAux", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool LeaveDetachment_Prefix(
            Formation __instance,
            List<IDetachment> ____detachments,
            IDetachment detachment)
        {

            foreach (Agent agent in detachment.Agents.Where(a => a.Formation == __instance && a.IsAIControlled).ToList())
            {
                detachment.RemoveAgent(agent);
                AttachUnit?.Invoke(__instance, new object[] { agent });
            }

            ____detachments.Remove(detachment);
            var detachmentManager = (DetachmentManager)DetachmentManager?.GetValue(__instance.Team);
            OnFormationLeaveDetachment?.Invoke(detachmentManager, new object[2]
            {
                __instance,
                detachment
            });
            return false;
        }

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
