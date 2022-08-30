using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using RTSCamera.View;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_MissionSpectatorControl
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionSpectatorControl));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
                    typeof(MissionSpectatorControl).GetMethod("OnMissionTick",
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionSpectatorControl).GetMethod(
                        nameof(Postfix_OnMissionTick), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static void Postfix_OnMissionTick(MissionSpectatorControl __instance, MissionSpectatorControlVM ____dataSource)
        {
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera && !Mission.Current.GetMissionBehavior<FlyCameraMissionView>().LockToAgent)
            {
                ____dataSource.IsEnabled = false;
            }
        }
    }
}
