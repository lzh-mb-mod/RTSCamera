using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Naval
{
    public class Patch_NarvalShipTargetSelectionHandler
    {
        private static PropertyInfo _globalFrame;
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(
                    AccessTools.Method("NavalDLC.View.MissionViews.NavalShipTargetSelectionHandler:GetShipDistanceToCenter"),
                    prefix: new HarmonyMethod(typeof(Patch_NarvalShipTargetSelectionHandler).GetMethod(
                        nameof(Prefix_GetShipDistanceToCenter), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        // Use mouse position instead of screen center to target formation.
        public static bool Prefix_GetShipDistanceToCenter(MissionView __instance, Object ship, Vec3 cameraPosition, ref float __result)
        {
            if (!__instance.MissionScreen.MouseVisible)
            {
                return true;
            }

            _globalFrame = AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:GlobalFrame");
            Vec3 origin = ((MatrixFrame)_globalFrame.GetValue(ship)).origin;
            float num = origin.AsVec2.Distance(cameraPosition.AsVec2);
            if ((double)num >= 1000.0)
            {
                __result = int.MaxValue;
                return false;
            }
            //if ((double)num <= 10.0)
            //{
            //    __result = 0.0f;
            //    return false;
            //}
            float screenX = 0.0f;
            float screenY = 0.0f;
            float w = 0.0f;
            var activeCamera = __instance.MissionScreen.CustomCamera ?? __instance.MissionScreen.CombatCamera;
            double insideUsableArea = (double)MBWindowManager.WorldToScreenInsideUsableArea(activeCamera, origin + new Vec3(z: 3f), ref screenX, ref screenY, ref w);
            // use mouse position
            __result = (double)w <= 0.0 ? (float)int.MaxValue : new Vec2(screenX, screenY).Distance(__instance.Input.GetMousePositionPixel());

            return false;
        }
    }
}
