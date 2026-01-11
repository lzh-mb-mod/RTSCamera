using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RTSCamera.Patch
{
    public class Patch_MissionScreen
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
                    typeof(MissionScreen).GetMethod("OnMissionModeChange",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_MissionScreen).GetMethod(
                        nameof(Prefix_OnMissionModeChange), BindingFlags.Static | BindingFlags.Public)));
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
        public static bool Prefix_OnMissionModeChange(MissionScreen __instance, MissionMode oldMissionMode, bool atStart)
        {
            if (__instance.Mission.Mode == MissionMode.Battle && oldMissionMode == MissionMode.Deployment)
            {
                Utility.SmoothMoveToAgent(__instance, true);
                //Utility.SetIsPlayerAgentAdded(__instance, false);
                return false;
            }

            return true;
        }
    }
}
