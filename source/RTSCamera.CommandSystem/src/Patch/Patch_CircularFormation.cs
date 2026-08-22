using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_CircularFormation
    {
        private static bool _patched;
        private static FieldInfo Owner = AccessTools.Field(typeof(LineFormation), "owner");

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(CircularFormation).GetMethod("GetCircumferenceAux",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_CircularFormation).GetMethod(nameof(Prefix_GetCircuferenceAux),
                            BindingFlags.Static | BindingFlags.Public)));

                //harmony.Patch(
                //    typeof(CircularFormation).GetMethod("FormFromCircumference",
                //        BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(
                //        typeof(Patch_CircularFormation).GetMethod(nameof(Prefix_FormFromCircumference),
                //            BindingFlags.Static | BindingFlags.Public)));

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

        public static bool Prefix_GetCircuferenceAux(CircularFormation __instance, int unitCount, int rankCount, float radialInterval, float distanceInterval, ref float __result)
        {
            if (CommandSystemConfig.Get().CircleFormationUnitSpacingPreference == CircleFormationUnitSpacingPreference.Loose)
                return true;
            var formation = Owner.GetValue(__instance) as Formation;
            if (formation == null ||
                formation.Team != null && !Utilities.Utility.ShouldEnablePlayerOrderControllerPatchForFormation(formation))
                return true;
            __result = Utilities.Utility.GetCircumferenceAuxOfCircularFormation(unitCount, rankCount, radialInterval, distanceInterval);
            return false;
        }

        //public static bool Prefix_FormFromCircumference(CircularFormation __instance, float circumference)
        //{
        //    var owner = Owner.GetValue(__instance) as Formation;
        //    int countWithOverride = owner.OverridenUnitCount ?? __instance.UnitCount;
        //    int maximumRankCount = Utilities.Utility.GetMaximumRankCountOfCircularFormation(owner, countWithOverride, owner.UnitSpacing);
        //    float radialInterval = owner.Interval + owner.UnitDiameter;
        //    float distanceInterval = owner.Distance + owner.UnitDiameter;
        //    float circumferenceAux = Utilities.Utility.GetCircumferenceAuxOfCircularFormation(countWithOverride, maximumRankCount, radialInterval, distanceInterval);
        //    float maxValue = TaleWorlds.Library.MathF.Max(0.0f, (float)countWithOverride * radialInterval);
        //    circumference = MBMath.ClampFloat(circumference, circumferenceAux, maxValue);
        //    // original
        //    //__instance.FlankWidth = Math.Max(circumference - owner.Interval, owner.UnitDiameter);
        //    __instance.FlankWidth = Math.Max(circumference, owner.UnitDiameter);
        //    return false;
        //}
    }
}
