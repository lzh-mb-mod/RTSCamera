using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionAgentLabelView
    {
        public static bool IsAllyInAllyTeam_Prefix(MissionAgentLabelView __instance, Agent agent, ref bool __result)
        {
            if (agent == __instance?.Mission?.MainAgent)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
