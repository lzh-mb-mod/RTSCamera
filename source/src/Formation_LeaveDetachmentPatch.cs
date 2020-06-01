using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Formation_LeaveDetachmentPatch
    {
        public static bool Prefix(
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
    }
}
