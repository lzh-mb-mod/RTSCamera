using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Orders;
using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderSetVM
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
                    typeof(OrderSetVM).GetMethod("RefreshOrders",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_OrderSetVM).GetMethod(nameof(Prefix_RefreshOrders),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_RefreshOrders(OrderSetVM __instance, OrderController ____orderController)
        {
            // disabled for naval battle.
            if (Mission.Current.IsNavalBattle)
                return true;
            __instance.Orders.Clear();
            __instance.SoloOrder = (OrderItemVM)null;
            if (__instance.OrderSet == null)
                return false;
            MBReadOnlyList<VisualOrder> orders = __instance.OrderSet.Orders;
            for (int index = 0; index < orders.Count; ++index)
                __instance.Orders.Add(new RTSCommandOrderItemVM(____orderController, orders[index]));
            if (!__instance.OrderSet.IsSoloOrder)
                return false;
            __instance.SoloOrder = __instance.Orders[0];
            return false;
        }
    }
}
