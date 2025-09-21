using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_PassageUsePoint
    {
        // TODO: verify the need of this patch.
        public static bool IsDisabledForAgent_Prefix(PassageUsePoint __instance, Agent agent, ref bool __result)
        {
            if (agent.MountAgent != null || __instance.IsDeactivated || __instance.ToLocation == null && !__instance.IsMissionExit || __instance.IsDisabled)
            {
                __result = true;
                return false;
            }

            if (agent.IsAIControlled && CampaignMission.Current.Location.GetLocationCharacter(agent.Origin) == null)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
