using SandBox.Source.Objects.SettlementObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_PassageUsePoint
    {
        public static bool IsDisabledForAgent_Prefix(PassageUsePoint __instance, Agent agent, ref bool __result)
        {
            if (agent.MountAgent != null || __instance.IsDeactivated || __instance.ToLocation == null)
            {
                __result = true;
                return false;
            }

            if (!agent.IsAIControlled || CampaignMission.Current.Location.GetLocationCharacter(agent.Origin) == null)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
