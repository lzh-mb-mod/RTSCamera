using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using RTSCamera.View;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_MissionGauntletSpectatorControl
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionGauntletSpectatorControl));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
                    typeof(MissionGauntletSpectatorControl).GetMethod(nameof(MissionGauntletSpectatorControl.OnMissionTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionGauntletSpectatorControl).GetMethod(
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
        public static void Postfix_OnMissionTick(MissionGauntletSpectatorControl __instance, MissionSpectatorControlVM ____dataSource)
        {
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera && !Mission.Current.GetMissionBehavior<FlyCameraMissionView>().LockToAgent)
            {
                ____dataSource.IsEnabled = false;
            }
        }
    }
}
