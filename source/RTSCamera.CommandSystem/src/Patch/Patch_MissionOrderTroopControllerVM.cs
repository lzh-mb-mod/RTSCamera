using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Orders;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionOrderTroopControllerVM
    {
        private static FieldInfo ActiveOrders = typeof(OrderSubjectVM).GetField("ActiveOrders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static PropertyInfo _orderSubType = typeof(OrderItemVM).GetProperty("OrderSubType",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderTroopControllerVM).GetMethod("OrderController_OnTroopOrderIssued",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderTroopControllerVM).GetMethod(
                        nameof(Prefix_OrderController_OnTroopOrderIssued), BindingFlags.Static | BindingFlags.Public)));
                //harmony.Patch(
                //    typeof(MissionOrderTroopControllerVM).GetMethod("SetTroopActiveOrders",
                //        BindingFlags.NonPublic | BindingFlags.Instance),
                //    new HarmonyMethod(typeof(Patch_MissionOrderTroopControllerVM).GetMethod(
                //        nameof(Prefix_SetTroopActiveOrders), BindingFlags.Static | BindingFlags.Public)));
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

        // hide facing order
        public static bool Prefix_OrderController_OnTroopOrderIssued(MissionOrderTroopControllerVM __instance,
            OrderType orderType,
            IEnumerable<Formation> appliedFormations,
            OrderController orderController)
        {
            DisableSelectTargetMode();
            return true;
        }

        //public static void CloseFacingOrderSet(MissionOrderVM missionOrderVM)
        //{
        //    var orderSets = typeof(MissionOrderVM).GetField("OrderSetsWithOrdersByType", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(missionOrderVM) as Dictionary<OrderSetType, OrderSetVM>;
        //    if (orderSets != null)
        //    {
        //        // hide facing orders
        //        if (orderSets.ContainsKey(OrderSetType.Facing))
        //            orderSets[OrderSetType.Facing].ShowOrders = false;
        //        // fix the issue that in legacy order layour type,
        //        // after giving facing orders by clicking on ground and then press escape, the order UI cannot be closed.
        //        if (missionOrderVM.LastSelectedOrderSetType == OrderSetType.Facing)
        //            missionOrderVM.LastSelectedOrderSetType = OrderSetType.None;
        //    }
        //}

        private static void DisableSelectTargetMode()
        {
            RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
        }
    }
}
