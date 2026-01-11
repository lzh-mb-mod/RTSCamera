using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderTroopPlacer
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
                   typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawing",
                       BindingFlags.Instance | BindingFlags.NonPublic),
                   prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(
                       nameof(Prefix_UpdateFormationDrawing), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }
        }

        public static bool Prefix_UpdateFormationDrawing(OrderTroopPlacer __instance, bool giveOrder, ref float? ____formationDrawingStartingTime)
        {
            if (__instance.MissionScreen.MouseVisible)
            {
                // Fix the issue that can't drag when slow motion is enabled and mouse is visible.
                ____formationDrawingStartingTime = 0;
            }
            return true;
        }
    }
}
