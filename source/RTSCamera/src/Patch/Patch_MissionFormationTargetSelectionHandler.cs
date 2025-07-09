using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Patch;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch
{
    public class Patch_MissionFormationTargetSelectionHandler
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
                    typeof(MissionFormationTargetSelectionHandler).GetMethod("GetFormationDistanceToCenter",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionFormationTargetSelectionHandler).GetMethod(
                        nameof(Prefix_GetFormationDistanceToCenter), BindingFlags.Static | BindingFlags.Public)));
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
        public static bool Prefix_GetFormationDistanceToCenter(MissionFormationTargetSelectionHandler __instance, Formation formation, Vec3 cameraPosition, ref float __result)
        {
            if (!__instance.MissionScreen.MouseVisible)
            {
                return true;
            }

            WorldPosition medianPosition = formation.QuerySystem.MedianPosition;
            medianPosition.SetVec2(formation.QuerySystem.AveragePosition);
            float num = formation.QuerySystem.AveragePosition.Distance(cameraPosition.AsVec2);
            if ((double)num >= 1000.0)
            {
                __result = int.MaxValue;
                return false;
            }
            if ((double)num <= 10.0)
            {
                __result = 0.0f;
                return false;
            }
            float screenX = 0.0f;
            float screenY = 0.0f;
            float w = 0.0f;
            var activeCamera = __instance.MissionScreen.CustomCamera ?? __instance.MissionScreen.CombatCamera;
            double insideUsableArea = (double)MBWindowManager.WorldToScreenInsideUsableArea(activeCamera, medianPosition.GetGroundVec3() + new Vec3(z: 3f), ref screenX, ref screenY, ref w);
            __result = (double)w <= 0.0 ? (float)int.MaxValue : new Vec2(screenX, screenY).Distance(__instance.Input.GetMousePositionPixel());

            return false;
        }
    }
}
