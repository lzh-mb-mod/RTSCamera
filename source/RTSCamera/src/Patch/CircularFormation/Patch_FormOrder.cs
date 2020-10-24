using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.CircularFormation
{
    public class Patch_FormOrder
    {
        private static readonly MethodInfo GetUnitCountOf =
            typeof(FormOrder).GetMethod(nameof(GetUnitCountOf), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo GetFileCount =
            typeof(FormOrder).GetMethod(nameof(GetFileCount), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo CustomWidth =
            typeof(FormOrder).GetProperty(nameof(CustomWidth), BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    typeof(FormOrder).GetMethod("OnApplyToArrangement",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_FormOrder).GetMethod(nameof(Prefix_OnApplyToArrangement),
                        BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool Prefix_OnApplyToArrangement(FormOrder __instance, Formation formation, IFormationArrangement arrangement)
        {
            try
            {
                switch (arrangement)
                {
                    case TaleWorlds.MountAndBlade.CircularFormation circularFormation:
                    {
                        int? unitCountOf1 = (int?)GetUnitCountOf?.Invoke(null, new object[] { formation });
                        int? fileCount1 = (int?)GetFileCount?.Invoke(__instance, new object[] { unitCountOf1 });
                        if (unitCountOf1.HasValue && fileCount1.HasValue)
                        {
                            int depth = Math.Max(1, (int)Math.Ceiling(unitCountOf1.Value * 1.0 / fileCount1.Value));
                            circularFormation.FormFromDepth(depth);
                            return false;
                        }

                        float? customWidth = (float?)CustomWidth?.GetValue(__instance);
                        if (!customWidth.HasValue)
                            return true;

                        circularFormation.FormFromCircumference((float)CustomWidth.GetValue(__instance) -
                                                                formation.UnitDiameter);

                        int countWithOverride = formation.OverridenUnitCount ?? circularFormation.UnitCount;
                        int maximumDepth = GetMaximumDepth(countWithOverride, formation.Distance, formation.Interval,
                            formation.UnitDiameter);
                        FormFromCircumference(circularFormation, (float)CustomWidth.GetValue(__instance) -
                                                                 formation.UnitDiameter, countWithOverride, maximumDepth,
                            formation.Distance, formation.Interval, formation.UnitDiameter);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }



        private static void FormFromCircumference(TaleWorlds.MountAndBlade.CircularFormation circularFormation, float circumference,
            int countWithOverride, int maximumDepth, float distance, float interval, float unitDiameter)
        {
            float num1 = (float)(6.28318548202515 * (distance + (double)unitDiameter) / (interval + (double)unitDiameter));
            int num2 = MBMath.Round(maximumDepth * (maximumDepth - 1) / 2 * num1);
            float minValue =
                Math.Max(0, Math.Min(MBMath.Round((countWithOverride + num2) / maximumDepth), countWithOverride)) *
                (interval + unitDiameter);
            float maxValue = Math.Max(0, countWithOverride - 1) * (interval + unitDiameter);
            circumference = MBMath.ClampFloat(circumference, minValue, maxValue);
            circularFormation.Width = circumference + unitDiameter;
        }

        private static int GetMaximumDepth(int unitCount, float distance, float interval, float unitDiameter)
        {
            int val1 = 0;
            int num = 0;
            while (num < unitCount)
            {
                int val2 = MBMath.Floor((float)(6.28318548202515 *
                                                 (val1 * (distance + unitDiameter)) /
                                                 (interval + (double)unitDiameter)));
                num += Math.Max(1, val2);
                ++val1;
            }
            return Math.Max(val1, 1);
        }
    }
}
