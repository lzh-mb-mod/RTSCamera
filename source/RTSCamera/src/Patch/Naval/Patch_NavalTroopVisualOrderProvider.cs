using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.Patch.Naval
{
    public class Patch_NavalTroopVisualOrderProvider
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("NavalTroopVisualOrderProvider").Method("GetOrders"),
                    postfix: new HarmonyMethod(typeof(Patch_NavalTroopVisualOrderProvider).GetMethod(nameof(Postfix_GetOrders), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static void Postfix_GetOrders(ref MBReadOnlyList<VisualOrderSet> __result)
        {
            if (Utilities.Utility.ShouldAddToggleShipOrderOrder())
            {
                __result.Add(CreateSingleOrderSetFor(new NavalToggleShipOrderOrder("order_toggle_ai", GameTexts.FindText("str_rts_camera_ai_control_ship_on"), GameTexts.FindText("str_rts_camera_ai_control_ship_off"))));
            }
        }

        private static SingleVisualOrderSet CreateSingleOrderSetFor(VisualOrder order)
        {
            return new SingleVisualOrderSet(order);
        }
    }
}
