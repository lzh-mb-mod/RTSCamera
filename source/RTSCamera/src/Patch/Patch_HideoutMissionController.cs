using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using SandBox.Missions.MissionLogics;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_HideoutMissionController
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
                    typeof(HideoutMissionController).GetMethod(nameof(HideoutMissionController.StartBossFightBattleMode),
                        BindingFlags.Static | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_HideoutMissionController).GetMethod(
                        nameof(Postfix_StartBossFightBattleMode), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static void Postfix_StartBossFightBattleMode()
        {
            var rtsCameraLogic = Mission.Current.GetMissionBehavior<RTSCameraLogic>();
            if (rtsCameraLogic == null)
            {
                return;
            }
            if (RTSCameraConfig.Get().FastForwardHideout == FastForwardHideout.Always)
            {
                rtsCameraLogic.SwitchFreeCameraLogic.FastForwardHideoutNextTick = true;
            }
        }
    }
}
