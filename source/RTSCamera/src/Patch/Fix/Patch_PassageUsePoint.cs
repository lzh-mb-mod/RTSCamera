using HarmonyLib;
using MissionSharedLibrary.Utilities;
using SandBox.Objects;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace RTSCamera.Patch.Fix
{
    public class Patch_PassageUsePoint
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(PassageUsePoint).GetMethod(nameof(PassageUsePoint.IsDisabledForAgent),
                        BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_PassageUsePoint).GetMethod(
                        nameof(Patch_PassageUsePoint.Prefix_DisabledForAgent),
                        BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        // TODO: verify the need of this patch.
        public static bool Prefix_DisabledForAgent(PassageUsePoint __instance, Agent agent, ref bool __result)
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
