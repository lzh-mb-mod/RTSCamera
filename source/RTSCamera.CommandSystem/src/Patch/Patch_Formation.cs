using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Reflection;
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

                var distanceInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MinimumDistance), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var distanceMap = typeof(Formation).GetInterfaceMap(distanceInterfaceMethod.DeclaringType);
                var distanceIndex = Array.IndexOf(distanceMap.InterfaceMethods, distanceInterfaceMethod);
                var distanceTargetMethod = distanceMap.TargetMethods[distanceIndex];
                var intervalInterfaceMethod = typeof(IFormation).GetProperty(nameof(IFormation.MinimumInterval), BindingFlags.Instance | BindingFlags.Public)
                        .GetMethod;
                var intervalMap = typeof(Formation).GetInterfaceMap(intervalInterfaceMethod.DeclaringType);
                var intervalIndex = Array.IndexOf(intervalMap.InterfaceMethods, intervalInterfaceMethod);
                var intervalTargetMethod = intervalMap.TargetMethods[intervalIndex];
                harmony.Patch(distanceTargetMethod,
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_MinimumDistance), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(intervalTargetMethod,
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_MinimumInterval), BindingFlags.Static | BindingFlags.Public)));


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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
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

        // Fix the problem that switch from circle arrangement to line arrangement, the width becomes PI times larger than it should be.
        // Because for circle arrangement, the flank width is the circumference, and width is the diameter.
        // flank width is passed in, and we should convert it to diameter and set it as the width of line formation.
        public static bool Prefix_TransformCustomWidthBetweenArrangementOrientations(ArrangementOrder.ArrangementOrderEnum orderTypeOld, ArrangementOrder.ArrangementOrderEnum orderTypeNew, float currentCustomWidth, ref float __result)
        {
            if (orderTypeOld == ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                __result = (float)(currentCustomWidth / Math.PI);
                return false;
            }
            else if (orderTypeOld != ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew == ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeOld != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                __result = (float)(currentCustomWidth / Math.PI);
                return false;
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

        public static bool Prefix_ReapplyFormOrder(Formation __instance)
        {
            FormOrder formOrder = __instance.FormOrder;
            if (__instance.FormOrder.OrderEnum == FormOrder.FormOrderEnum.Custom &&
                __instance.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Circle ||
                __instance.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Square)
            {
                __instance.FormOrder = formOrder;
                return false;
            }
            return true;
        }
    }
}
