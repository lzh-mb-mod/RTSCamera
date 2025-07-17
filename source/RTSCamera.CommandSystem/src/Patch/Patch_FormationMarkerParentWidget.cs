using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_FormationMarkerParentWidget
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
                    typeof(FormationMarkerParentWidget).GetMethod("OnLateUpdate",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(
                        typeof(Patch_FormationMarkerParentWidget).GetMethod(nameof(Postfix_OnLateUpdate),
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
        public static void Postfix_OnLateUpdate(FormationMarkerParentWidget __instance)
        {
            if (string.IsNullOrEmpty(__instance.MarkerType))
            {
                __instance.TeamTypeMarker.IsVisible = false;
            }
            else
            {
                __instance.TeamTypeMarker.IsVisible = true;
            }
        }
    }
}
