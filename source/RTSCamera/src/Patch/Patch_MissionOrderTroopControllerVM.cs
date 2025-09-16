using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch
{
    public class Patch_MissionOrderTroopControllerVM
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                //harmony.Patch(
                //    typeof(MissionOrderTroopControllerVM).GetMethod("OrderController_OnTroopOrderIssued",
                //        BindingFlags.NonPublic | BindingFlags.Instance),
                //    new HarmonyMethod(typeof(Patch_MissionOrderTroopControllerVM).GetMethod(
                //        nameof(Prefix_OrderController_OnTroopOrderIssued), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderTroopControllerVM).GetMethod("OrderController_OnTroopOrderIssued",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderTroopControllerVM).GetMethod(
                        nameof(Postfix_OrderController_OnTroopOrderIssued), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
        }

        // hide facing order
        //public static bool Prefix_OrderController_OnTroopOrderIssued(MissionOrderTroopControllerVM __instance,
        //    OrderType orderType,
        //    IEnumerable<Formation> appliedFormations,
        //    OrderController orderController,
        //    object[] delegateParams,
        //    MissionOrderVM ____missionOrder)
        //{
        //    //CloseMovementOrderSet(____missionOrder);
        //    return true;
        //}

        //public static void CloseMovementOrderSet(MissionOrderVM missionOrderVM)
        //{
        //    // TODO: clear code
        //    //var orderSets = typeof(MissionOrderVM).GetField("OrderSetsWithOrdersByType", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(missionOrderVM) as Dictionary<OrderSetType, OrderSetVM>;
        //    //if (orderSets != null)
        //    //{
        //    //    // fix the issue that after open movement order set and click on ground, we have to press esc twice to close the order ui.
        //    //    if (missionOrderVM.LastSelectedOrderSetType == OrderSetType.Movement)
        //    //    {
        //    //        missionOrderVM.LastSelectedOrderSetType = OrderSetType.None;
        //    //        Patch_MissionOrderVM.UpdateTitleOrdersKeyVisualVisibility.Invoke(missionOrderVM, new object[] { });
        //    //    }
        //    //}
        //}

        public static void Postfix_OrderController_OnTroopOrderIssued(
            MissionOrderTroopControllerVM __instance,
            OrderType orderType,
            IEnumerable<Formation> appliedFormations,
            OrderController orderController,
            MissionOrderVM ____missionOrder)
        {
            if (!____missionOrder.IsToggleOrderShown && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                ____missionOrder.OpenToggleOrder(false);
            }
        }
    }
}
