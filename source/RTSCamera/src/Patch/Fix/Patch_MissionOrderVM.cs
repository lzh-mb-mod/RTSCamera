using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static void Postfix_OnOrderExecuted(MissionOrderVM __instance, OrderItemVM orderItem)
        {
            // Already Implemented in Patch_MissionGauntletSingleplayerOrderUIHandler.UpdateOrderUIVisibility
            // TODO: don't close the order ui and open it again.
            // Keep orders UI open after issuing an order in free camera mode.
            if (!__instance.IsToggleOrderShown && !__instance.TroopController.IsTransferActive && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                //var displayedOrderMessageForLastOrder = __instance.DisplayedOrderMessageForLastOrder;
                //__instance.OpenToggleOrder(false);
                //AccessTools.Property(typeof(MissionOrderVM), "DisplayedOrderMessageForLastOrder").SetValue(__instance, displayedOrderMessageForLastOrder);
            }
            var orderTroopPlacer = Mission.Current.GetMissionBehavior<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
            {
                typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(orderTroopPlacer, null);
            }
            //var orderUIHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            //if (orderUIHandler == null)
            //{
            //    return false;
            //}
            //var missionOrderVM = typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orderUIHandler) as MissionOrderVM;
            //var setActiveOrders = typeof(MissionOrderVM).GetMethod("SetActiveOrders", BindingFlags.Instance | BindingFlags.NonPublic);
            //setActiveOrders.Invoke(missionOrderVM, new object[] { });
        }

        public static void Postfix_OnTransferFinished(MissionOrderVM __instance)
        {
            // Keep orders UI open after transfer finished in free camera mode.
            if (!__instance.IsToggleOrderShown && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                __instance.OpenToggleOrder(false);
            }
        }

        public static bool Prefix_TryCloseToggleOrder(MissionOrderVM __instance, bool applySelectedOrders, ref bool __result, MissionOrderCallbacks ____callbacks)
        {
            if (__instance.IsToggleOrderShown)
            {
                bool shouldKeepOpen = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera;
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
    }
}
