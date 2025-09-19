using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
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

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
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
    }
}
