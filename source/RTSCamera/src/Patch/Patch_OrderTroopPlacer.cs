using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Patch.Fix;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch
{
    public class Patch_OrderTroopPlacer
    {
        private static bool _patched;
        private static PropertyInfo _displayedOrderMessageForLastOrder = AccessTools.Property(typeof(MissionOrderVM), "DisplayedOrderMessageForLastOrder");
        private static FieldInfo _isMouseDownField = AccessTools.Field(typeof(OrderTroopPlacer), "_isMouseDown");

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
                harmony.Patch(
                    AccessTools.Method(typeof(OrderTroopPlacer), "GetScreenPoint"),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_GetScreenPoint),
                        BindingFlags.Static | BindingFlags.Public)));
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
                _displayedOrderMessageForLastOrder.SetValue(missionOrderVM, false);
            }
            return true;
        }

        public static bool Prefix_GetScreenPoint(OrderTroopPlacer __instance, ref Vec2 __result, ref Vec2 ____deltaMousePosition)
        {
            __result = GetMousePositionPixelKeepingPositionDuringDragging(__instance) + ____deltaMousePosition;
            return false;
        }

        private static Vec2 GetMousePositionPixelKeepingPositionDuringDragging(OrderTroopPlacer __instance)
        {
            return !__instance.MissionScreen.MouseVisible ?
                Patch_MissionGauntletSingleplayerOrderUIHandler.MousePositionRangedBeforeDragging ?? new Vec2(0.5f, 0.5f) :
                __instance.Input.GetMousePositionRanged();
        }


        public static bool IsMouseDown(OrderTroopPlacer __instance)
        {
            return (bool)_isMouseDownField.GetValue(__instance);
        }
    }
}
