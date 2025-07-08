using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

namespace RTSCamera.Patch
{
    public class Patch_ScoreboardScreenWidget
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
                    typeof(ScoreboardScreenWidget).GetMethod("UpdateControlButtons",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_ScoreboardScreenWidget).GetMethod(
                        nameof(Postfix_UpdateControlButtons), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static void Postfix_UpdateControlButtons(ScoreboardScreenWidget __instance, Widget ____fastForwardWidget)
        {
            // show fast forward button when ControlAllyAfterDeath is enabled
            if (RTSCameraConfig.Instance?.ControlAllyAfterDeath == true)
            {
                ____fastForwardWidget.IsVisible = __instance.ShowScoreboard;
            }
        }
    }
}
