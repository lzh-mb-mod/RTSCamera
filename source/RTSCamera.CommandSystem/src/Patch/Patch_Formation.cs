using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_Formation
    {
        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                var minimumDistanceInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MinimumDistance), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var minimumDistanceMap = typeof(Formation).GetInterfaceMap(minimumDistanceInterfaceMethod.DeclaringType);
                var minimumDistanceIndex = Array.IndexOf(minimumDistanceMap.InterfaceMethods, minimumDistanceInterfaceMethod);
                var minimumDistanceTargetMethod = minimumDistanceMap.TargetMethods[minimumDistanceIndex];
                var maximumDistanceInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MaximumDistance), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var maximumDistanceMap = typeof(Formation).GetInterfaceMap(maximumDistanceInterfaceMethod.DeclaringType);
                var maximumDistanceIndex = Array.IndexOf(maximumDistanceMap.InterfaceMethods, maximumDistanceInterfaceMethod);
                var maximumDistanceTargetMethod = maximumDistanceMap.TargetMethods[maximumDistanceIndex];
                var minimumIntervalInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MinimumInterval), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var minimumIntervalMap = typeof(Formation).GetInterfaceMap(minimumIntervalInterfaceMethod.DeclaringType);
                var minimumIntervalIndex = Array.IndexOf(minimumIntervalMap.InterfaceMethods, minimumIntervalInterfaceMethod);
                var minimumIntervalTargetMethod = minimumIntervalMap.TargetMethods[minimumIntervalIndex];
                var maximumIntervalInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MaximumInterval), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var maximumIntervalMap = typeof(Formation).GetInterfaceMap(maximumIntervalInterfaceMethod.DeclaringType);
                var maximumIntervalIndex = Array.IndexOf(maximumIntervalMap.InterfaceMethods, maximumIntervalInterfaceMethod);
                var maximumIntervalTargetMethod = maximumIntervalMap.TargetMethods[maximumIntervalIndex];
                harmony.Patch(minimumDistanceTargetMethod,
                   prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                       nameof(Prefix_MinimumDistance), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(maximumDistanceTargetMethod,
                   prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                       nameof(Prefix_MaximumDistance), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(minimumIntervalTargetMethod,
                   prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                       nameof(Prefix_MinimumInterval), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(maximumIntervalTargetMethod,
                   prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                       nameof(Prefix_MaximumInterval), BindingFlags.Static | BindingFlags.Public)));


                harmony.Patch(
                    typeof(Formation).GetMethod("TransformCustomWidthBetweenArrangementOrientations",
                    BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_TransformCustomWidthBetweenArrangementOrientations), BindingFlags.Static | BindingFlags.Public)));

                // Command Queue
                harmony.Patch(
                    typeof(Formation).GetMethod(nameof(Formation.Tick),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_Tick), BindingFlags.Static | BindingFlags.Public)));

                // resizable square formation
                harmony.Patch(
                    typeof(Formation).GetMethod("ReapplyFormOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_ReapplyFormOrder), BindingFlags.Static | BindingFlags.Public)));

                harmony.Patch(
                    typeof(Formation).GetProperty(nameof(Formation.CalculateHasSignificantNumberOfMounted)).GetGetMethod(),
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_CalculateHasSignificantNumberOfMounted), BindingFlags.Static | BindingFlags.Public)));
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

        public static bool Prefix_MinimumDistance(Formation __instance, ref float __result)
        {
            try
            {
                __result = Formation.GetDefaultMinimumDistance(__instance.CalculateHasSignificantNumberOfMounted && !(__instance.RidingOrder == RidingOrder.RidingOrderDismount));
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }

        public static bool Prefix_MaximumDistance(Formation __instance, ref float __result)
        {
            try
            {
                bool isMounted = __instance.CalculateHasSignificantNumberOfMounted && !(__instance.RidingOrder == RidingOrder.RidingOrderDismount);
                var maxUnitSpacing = ArrangementOrder.GetUnitSpacingOf(__instance.ArrangementOrder.OrderEnum);
                __result = isMounted ? Formation.CavalryDistance(maxUnitSpacing) : Formation.InfantryDistance(maxUnitSpacing);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }


        public static bool Prefix_MinimumInterval(Formation __instance, ref float __result)
        {
            try
            {
                __result = Formation.GetDefaultMinimumInterval(__instance.CalculateHasSignificantNumberOfMounted && !(__instance.RidingOrder == RidingOrder.RidingOrderDismount));
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }

        public static bool Prefix_MaximumInterval(Formation __instance, ref float __result)
        {
            try
            {
                bool isMounted = __instance.CalculateHasSignificantNumberOfMounted && !(__instance.RidingOrder == RidingOrder.RidingOrderDismount);
                var maxUnitSpacing = ArrangementOrder.GetUnitSpacingOf(__instance.ArrangementOrder.OrderEnum);
                __result = isMounted ? Formation.CavalryInterval(maxUnitSpacing) : Formation.InfantryInterval(maxUnitSpacing);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }

        // Fix the problem that switch from circle arrangement to line arrangement, the width becomes PI times larger than it should be.
        // Because for circle arrangement, the flank width is the circumference, and width is the diameter.
        // flank width is passed in, and we should convert it to diameter and set it as the width of line formation.
        public static bool Prefix_TransformCustomWidthBetweenArrangementOrientations(ArrangementOrder.ArrangementOrderEnum orderTypeOld, ArrangementOrder.ArrangementOrderEnum orderTypeNew, float currentCustomWidth, ref float __result)
        {
            if (orderTypeOld == ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                __result = (float)(currentCustomWidth / Math.PI);
                return false;
                //return true;
            }
            else if (orderTypeOld == ArrangementOrder.ArrangementOrderEnum.Square && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Square && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                __result = currentCustomWidth / 4f;
                return false;
            }
            else if (orderTypeOld != ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew == ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeOld != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                //__result = (float)(currentCustomWidth / Math.PI);
                //return false;
                return true;
            }
            return true;
        }

        public static bool Prefix_Tick(Formation __instance, float dt)
        {
            if (__instance.Team == null || !__instance.Team.IsPlayerTeam || __instance.IsAIControlled)
            {
                return true;
            }
            CommandQueueLogic.UpdateFormation(__instance);
            return true;
        }


        public static int ReapplyFormOrderExecutionCount = 0;

        public static bool Prefix_ReapplyFormOrder(Formation __instance)
        {
            if (ReapplyFormOrderExecutionCount < 3)
            {
                ++ReapplyFormOrderExecutionCount;
            }
            else
            {
                var assertMessage = $"RTS Command Warning: Detected that ReapplyFormOrder has been recursively call for 3 times. Skip execution to avoid issue. The current arrangement order is {__instance.ArrangementOrder.OrderType.ToString()}, UnitSpacing = {__instance.UnitSpacing}, FlankWith = {__instance.Arrangement.FlankWidth}, UnitCount = {__instance.Arrangement.UnitCount}";
                Utility.DisplayMessage(assertMessage);
                Debug.Print(assertMessage);
                return false;
            }
            try
            {
                FormOrder formOrder = __instance.FormOrder;
                if (__instance.FormOrder.OrderEnum == FormOrder.FormOrderEnum.Custom &&
                    __instance.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Circle &&
                    __instance.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Square)
                {
                    formOrder.CustomFlankWidth = __instance.Arrangement.FlankWidth;
                }
                __instance.FormOrder = formOrder;
                return false;
            }
            finally
            {
                --ReapplyFormOrderExecutionCount;
            }
        }

        public static bool Prefix_CalculateHasSignificantNumberOfMounted(Formation __instance, bool? ____overridenHasAnyMountedUnit, ref bool __result)
        {
            return ____overridenHasAnyMountedUnit.HasValue ? ____overridenHasAnyMountedUnit.Value : (double)__instance.QuerySystem.CavalryUnitRatio + (double)__instance.QuerySystem.RangedCavalryUnitRatio >= CommandSystemConfig.Get().MountedUnitsIntervalThreshold;
        }
    }
}
