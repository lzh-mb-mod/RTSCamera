using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        public static bool LeaveDetachment_Prefix(
            Formation __instance,
            List<IDetachment> ____detachments,
            IDetachment detachment)
        {

            foreach (Agent agent in detachment.Agents.Where(a => a.Formation == __instance && a.IsAIControlled).ToList())
            {
                detachment.RemoveAgent(agent);
                __instance.AttachUnit(agent);
            }

            ____detachments.Remove(detachment);

            __instance.Team.DetachmentManager.OnFormationLeaveDetachment(__instance, detachment);
            return false;
        }
    }
}
