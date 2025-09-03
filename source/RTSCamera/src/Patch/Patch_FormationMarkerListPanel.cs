using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

namespace RTSCamera.Patch
{
    public class Patch_FormationMarkerListPanel
    {
        public static float MinAlpha = 0.2f;
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                harmony.Patch(
                   typeof(FormationMarkerListPanel).GetMethod("GetDistanceRelatedAlphaTarget",
                       BindingFlags.Instance | BindingFlags.NonPublic),
                   prefix: new HarmonyMethod(typeof(Patch_FormationMarkerListPanel).GetMethod(
                       nameof(Prefix_GetDistanceRelatedAlphaTarget), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_GetDistanceRelatedAlphaTarget(FormationMarkerListPanel __instance, float distance, ref float __result)
        {
            if ((double)distance > (double)__instance.FarDistanceCutoff)
                __result = __instance.FarAlphaTarget;
            else if ((double)distance <= (double)__instance.FarDistanceCutoff && (double)distance >= (double)__instance.CloseDistanceCutoff)
                __result = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Lerp(1f, __instance.FarAlphaTarget, (float)Math.Pow(((double)distance - (double)__instance.CloseDistanceCutoff) / ((double)__instance.FarDistanceCutoff - (double)__instance.CloseDistanceCutoff), 1.0 / 3.0)), __instance.FarAlphaTarget, 1f);
            else
                __result = (double)distance < (double)__instance.CloseDistanceCutoff && (double)distance > (double)__instance.CloseDistanceCutoff - (double)__instance.ClosestFadeoutRange ? TaleWorlds.Library.MathF.Lerp(MinAlpha, 1f, (distance - (__instance.CloseDistanceCutoff - __instance.ClosestFadeoutRange)) / __instance.ClosestFadeoutRange) : MinAlpha;
            return false;
        }
    }
}
