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
        private static readonly Harmony Harmony = new Harmony(CommandSystemSubModule.ModuleId + "_" + nameof(Patch_OrderTroopItemVM));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
                    typeof(OrderTroopItemVM).GetMethod("RefreshTargetedOrderVisual",
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(
                        typeof(Patch_OrderTroopItemVM).GetMethod(nameof(Postfix_RefreshTargetedOrderVisual),
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
        public static void Postfix_RefreshTargetedOrderVisual(OrderTroopItemVM __instance)
        {
            // show target formation icon event if current order is neither charge nor advance
            Formation targetFormation = __instance.Formation.TargetFormation;
            OrderSubType orderSubType = (OrderSubType)typeof(OrderUIHelper).GetMethod("GetActiveMovementOrderOfFormation", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(__instance, new object[] { __instance.Formation });
            __instance.HasTarget = true;
            __instance.CurrentOrderIconId = orderSubType.ToString();
            if (targetFormation != null)
            {
                __instance.CurrentTargetFormationType = MissionFormationMarkerTargetVM.GetFormationType(targetFormation.PhysicalClass);
            }
            else
            {
                __instance.CurrentTargetFormationType = "";
            }
        }
    }
}
