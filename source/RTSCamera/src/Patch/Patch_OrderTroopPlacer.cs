using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

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
                       BindingFlags.Instance | BindingFlags.Public),
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
                var missionOrderVM = Utility.GetMissionOrderVM(Mission.Current);
                // Fix the issue that movement order message may not be shown in free camera, when order UI is kept open.
                AccessTools.Property(typeof(MissionOrderVM), "DisplayedOrderMessageForLastOrder").SetValue(missionOrderVM, false);
            }
            return true;
        }
    }
}
