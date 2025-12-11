using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_Mission
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
                    typeof(Mission).GetMethod("UpdateSceneTimeSpeed",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_Mission).GetMethod(
                        nameof(Prefix_UpdateSceneTimeSpeed), BindingFlags.Static | BindingFlags.Public)));
                // recover player formation from general formation
                harmony.Patch(
                    typeof(Mission).GetMethod("OnTeamDeployed",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Mission).GetMethod(nameof(Prefix_OnTeamDeployed),
                            BindingFlags.Static | BindingFlags.Public)));
                // recover player formation from general formation
                harmony.Patch(
                    typeof(Mission).GetMethod("OnDeploymentFinished",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Mission).GetMethod(nameof(Prefix_OnDeploymentFinished),
                            BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(Mission).GetMethod(nameof(Mission.CanTakeControlOfAgent),
                    BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Mission).GetMethod(nameof(Prefix_CanTakeControlOfAgent),
                            BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_UpdateSceneTimeSpeed(Mission __instance)
        {
            if (RTSCameraConfig.Get().SlowMotionMode && __instance.Mode != MissionMode.Deployment && !__instance.IsFastForward)
            {
                __instance.Scene.TimeSpeed = RTSCameraConfig.Get().SlowMotionFactor;
                return false;
            }

            return true;
        }

        public static void Prefix_OnTeamDeployed(Team team)
        {
            RTSCameraLogic.Instance?.SwitchFreeCameraLogic.OnEarlyTeamDeployed(team);
        }

        public static void Prefix_OnDeploymentFinished(Mission __instance)
        {
            RTSCameraLogic.Instance?.SwitchFreeCameraLogic.OnEarlyDeploymentFinished();
        }

        public static bool Prefix_CanTakeControlOfAgent(Mission __instance, Agent agentToTakeControlOf, ref bool __result)
        {
            // Disable build-in take control because RTS provides it.
            __result = false;
            return false;
        }
    }
}
