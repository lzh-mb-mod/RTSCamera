using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionOrderVM
    {

        public static bool AllowEscape = true;
        public static bool AllowClosingOrderUI = false;

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("CheckCanBeOpened",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_CheckCanBeOpened), BindingFlags.Static | BindingFlags.Public)));
                //harmony.Patch(
                //    typeof(MissionOrderVM).GetMethod("AfterInitialize", BindingFlags.Instance | BindingFlags.Public),
                //    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_AfterInitialize),
                //        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod(nameof(MissionOrderVM.OnEscape),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnEscape),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnOrderExecuted",
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_OnOrderExecuted),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnTransferFinished",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_OnTransferFinished),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("TryCloseToggleOrder",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_TryCloseToggleOrder),
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

        public static bool Prefix_CheckCanBeOpened(MissionOrderVM __instance, bool displayMessage, ref bool __result)
        {
            // In free camera mode, order UI can be opened event if main agent is controller by AI
            if (Agent.Main != null && !Agent.Main.IsPlayerControlled && Mission.Current
                .GetMissionBehavior<RTSCameraLogic>()?.SwitchFreeCameraLogic.IsSpectatorCamera == true)
            {
                __result = true;
                return false;
            }

            return true;
        }

        //public static void Postfix_AfterInitialize(MissionOrderVM __instance)
        //{
        //    LastSelectedOrderSetType.SetValue(__instance, (object)OrderSetType.None);
        //    AllowEscape = true;
        //}

        public static bool Prefix_OnEscape(MissionOrderVM __instance)
        {
            AllowClosingOrderUI = AllowEscape;
            // Do nothing during draging camera using right mouse button.
            return AllowEscape;
            //if (!AllowEscape)
            //    return false;
            //if (!__instance.IsToggleOrderShown)
            //    return false;
            //if (____currentActivationType == MissionOrderVM.ActivationType.Hold)
            //{
            //    if (__instance.LastSelectedOrderItem == null)
            //        return false;
            //    UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
            //    ___OrderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
            //    LastSelectedOrderItem.SetValue(__instance, null);
            //}
            //else
            //{
            //    if (____currentActivationType != MissionOrderVM.ActivationType.Click)
            //        return false;
            //    LastSelectedOrderItem.SetValue(__instance, null);
            //    if (__instance.LastSelectedOrderSetType != OrderSetType.None)
            //    {
            //        ___OrderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
            //        __instance.LastSelectedOrderSetType = OrderSetType.None;
            //        UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
            //    }
            //    else
            //    {
            //        __instance.LastSelectedOrderSetType = OrderSetType.None;
            //        __instance.TryCloseToggleOrder();
            //    }
            //}
            //return false;
        }

        private static PropertyInfo _displayOrderMessageForLastOrder = AccessTools.Property(typeof(MissionOrderVM), "DisplayedOrderMessageForLastOrder");
        private static MethodInfo _Reset_OrderTrropPlacer = typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void UpdateOrderUIOnOrderExecuted(MissionOrderVM __instance)
        {
            // Close UI if needed
            bool shouldKeepOpen = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true &&
                RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepOrderUIOpen == true &&
                RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera || Mission.Current.Mode == TaleWorlds.Core.MissionMode.Deployment || __instance.TroopController.IsTransferActive;
            if (shouldKeepOpen)
            {
                if (!__instance.TroopController.IsTransferActive)
                {
                    var displayedOrderMessageForLastOrder = __instance.DisplayedOrderMessageForLastOrder;
                    TryCloseToggleOrder(__instance);
                    Patch_MissionOrderVM.OpenToggleOrder(__instance, false);
                    _displayOrderMessageForLastOrder.SetValue(__instance, displayedOrderMessageForLastOrder);
                }
            }
            else
            {
                var displayedOrderMessageForLastOrder = __instance.DisplayedOrderMessageForLastOrder;
                TryCloseToggleOrder(__instance);
                _displayOrderMessageForLastOrder.SetValue(__instance, displayedOrderMessageForLastOrder);
            }

            var orderTroopPlacer = Mission.Current.GetMissionBehavior<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
            {
                _Reset_OrderTrropPlacer.Invoke(orderTroopPlacer, null);
            }
        }

        public static void Postfix_OnOrderExecuted(MissionOrderVM __instance, OrderItemVM orderItem)
        {
            UpdateOrderUIOnOrderExecuted(__instance);
        }

        public static void Postfix_OnTransferFinished(MissionOrderVM __instance)
        {
            // Keep orders UI open after transfer finished in free camera mode.
            if (RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                // Close and open again, to fix the issue that MissionGauntletSingleplayerOrderUIHandler.OnTransferFinished may disable it's scene layer.
                // and cause Command System not highlighting the original formation.
                Patch_MissionOrderVM.TryCloseToggleOrder(__instance);
                Patch_MissionOrderVM.OpenToggleOrder(__instance, false);
            }
        }

        public static bool Prefix_TryCloseToggleOrder(MissionOrderVM __instance, bool applySelectedOrders, ref bool __result, MissionOrderCallbacks ____callbacks)
        {
            // Since Bannerlord v1.3.x, there're several places that calls TryCloseToggleOrder:
            // 1. MissionOrderTroopControllerVM.OrderController_OnTroopOrderIssued
            // 2. GauntletOrderUIHandler.TickInput
            // It's difficult to implement "Keep Order UI Open" by opening the UI after being closed.
            // and there's performance issue.
            // So it's implemented in this way:
            // Prevent the order UI from being closed in certain condition, and only close once when needed.
            if (__instance.IsToggleOrderShown)
            {
                bool shouldKeepOpen = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepOrderUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera;
                //bool shouldKeepOpen = !AllowClosingOrderUI;
                if (AllowClosingOrderUI)
                {
                    AllowClosingOrderUI = false;
                    shouldKeepOpen = false;
                }
                Mission.Current.IsOrderMenuOpen = shouldKeepOpen;
                if (!shouldKeepOpen)
                    return true;

                if (applySelectedOrders && __instance.SelectedOrderSet != null)
                {
                    OrderItemVM orderItemVm = __instance.SelectedOrderSet.Orders.FirstOrDefault(o => o.IsSelected);
                    if (orderItemVm != null && ____callbacks.GetVisualOrderExecutionParameters != null)
                    {
                        VisualOrderExecutionParameters executionParameters = ____callbacks.GetVisualOrderExecutionParameters();
                        orderItemVm.ExecuteAction(executionParameters);
                    }
                }
                return false;
            }
            __result = false;
            return false;
        }

        public static bool TryCloseToggleOrder(MissionOrderVM __instance, bool applySelectedOrders = false)
        {
            return __instance?.TryCloseToggleOrder(applySelectedOrders) ?? false;
        }

        public static void OpenToggleOrder(MissionOrderVM __instance, bool fromHold, bool displayMessage = true)
        {
            __instance?.OpenToggleOrder(fromHold, displayMessage);
        }


        // Called every tick so that the order UI can be closed only once per tick.
        public static void TickAllowClosingOrderUI()
        {
            Patch_MissionOrderVM.AllowClosingOrderUI = true;
        }
    }
}
