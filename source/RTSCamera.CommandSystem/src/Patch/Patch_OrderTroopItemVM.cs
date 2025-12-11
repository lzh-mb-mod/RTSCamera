using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderTroopItemVM
    {
        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // always show current orders.
                //harmony.Patch(
                //    typeof(OrderTroopItemVM).GetMethod("RefreshTargetedOrderVisual",
                //        BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(
                //        typeof(Patch_OrderTroopItemVM).GetMethod(nameof(Prefix_RefreshTargetedOrderVisual),
                //            BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopItemVM).GetMethod(nameof(OrderTroopItemVM.ExecuteAction), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
                    prefix: new HarmonyMethod(
                        typeof(Patch_OrderTroopItemVM).GetMethod(nameof(Prefix_ExecuteAction), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)));
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
        //public static bool Prefix_RefreshTargetedOrderVisual(OrderTroopItemVM __instance)
        //{
        //    bool flag = false;
        //    string str1 = "";
        //    string str2 = "";
        //    for (int index = 0; index < __instance.ActiveOrders.Count; ++index)
        //    {
        //        OrderItemVM activeOrder = __instance.ActiveOrders[index];
        //        if (activeOrder.Order.IsTargeted())
        //        {
        //            Formation targetFormation = __instance.Formation.TargetFormation;
        //            if (targetFormation != null)
        //            {
        //                str2 = MissionFormationMarkerTargetVM.GetFormationType(targetFormation.PhysicalClass);
        //                flag = true;
        //            }
        //            str1 = activeOrder.OrderIconId;
        //        }
        //    }
        //    __instance.HasTarget = flag;
        //    __instance.CurrentOrderIconId = str1;
        //    __instance.CurrentTargetFormationType = str2;
        //    return false;
        //}

        public static bool Prefix_ExecuteAction(OrderTroopItemVM __instance)
        {
            if (__instance.SetSelected == null)
                return false;

            if (!__instance.IsSelectable)
                return false;

            return true;
        }
    }
}
