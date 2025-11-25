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
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
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
            // Disable built-in take control hint because RTS provides it.
            ____dataSource.IsTakeControlEnabled = false;
            if (RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true)
            {
                // update 
                if (Mission.Current.GetMissionBehavior<FlyCameraMissionView>()?.LockToAgent == true)
                {
                    // Do not consider main agent dead when lock to agent.
                    ____dataSource.SetMainAgentStatus(false);
                    ____dataSource.IsEnabled = false;
                }
                else
                {
                    ____dataSource.IsEnabled = false;
                }
            }
        }
    }
}
