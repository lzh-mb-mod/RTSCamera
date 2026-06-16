using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
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
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static void Postfix_GetOrders(ref MBReadOnlyList<VisualOrderSet> __result)
        {
            var newOrder = new NavalToggleShipOrderOrder("order_soldier_pilot_ship", "order_toggle_ai", GameTexts.FindText("str_rts_camera_ai_control_ship_on"), GameTexts.FindText("str_rts_camera_ai_control_ship_off"));
            if (Utilities.Utility.ShouldAddToggleShipOrderOrder())
            {
                var toggleAIIndex = __result.FindIndex(order => order.StringId == "order_toggle_ai");
                if (Input.IsGamepadActive)
                {
                    //var orderSet = __result.Where(orderSet => orderSet.StringId == "troop_visual_orders").FirstOrDefault();
                    //if (orderSet != null)
                    //{
                    //    orderSet.AddOrder(newOrder);
                    //}
                    var insertIndex = toggleAIIndex >= 0 ? toggleAIIndex : __result.Count - 1;
                    var lastOrderIndex = __result.Count - 1;
                    if (lastOrderIndex >= 0)
                    {
                        __result.Insert(lastOrderIndex, CreateSingleOrderSetFor(newOrder));
                    }
                }
                else
                {
                    var insertIndex = toggleAIIndex >= 0 ? toggleAIIndex : __result.Count;
                    __result.Insert(insertIndex, CreateSingleOrderSetFor(newOrder));
                }
            }
        }

        private static SingleVisualOrderSet CreateSingleOrderSetFor(VisualOrder order)
        {
            return new SingleVisualOrderSet(order);
        }
    }
}
