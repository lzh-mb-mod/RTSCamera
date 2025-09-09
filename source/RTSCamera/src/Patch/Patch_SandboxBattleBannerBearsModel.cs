using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using SandBox;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_SandboxBattleBannerBearsModel
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Fix the issue that if player bears banner, switching to free camera will cause the banner effect to disappear
                harmony.Patch(
                    typeof(SandboxBattleBannerBearersModel).GetMethod("CanBannerBearerProvideEffectToFormation",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_SandboxBattleBannerBearsModel).GetMethod(
                        nameof(Prefix_CanBannerBearerProvideEffectToFormation), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(CustomBattleBannerBearersModel).GetMethod("CanBannerBearerProvideEffectToFormation",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_SandboxBattleBannerBearsModel).GetMethod(
                        nameof(Prefix_CanBannerBearerProvideEffectToFormation), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_CanBannerBearerProvideEffectToFormation(Agent agent, Formation formation, ref bool __result)
        {
            if (CommandBattleBehavior.CommandMode || RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera != true)
                return true;
            if (agent != null && agent.IsMainAgent && agent.Team == formation.Team)
            {
                __result = true;
                return false;
            }
            return true;

        }
    }
}
