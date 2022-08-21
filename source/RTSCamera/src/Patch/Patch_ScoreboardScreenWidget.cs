using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using RTSCamera.View;
using System;
using System.Reflection;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_ScoreboardScreenWidget
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_ScoreboardScreenWidget));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
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
            if (RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false)
            {
                ____fastForwardWidget.IsVisible = __instance.ShowScoreboard;
            }
        }
    }
}
