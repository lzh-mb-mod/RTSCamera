using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.Patch.Naval
{
    public class Patch_NavalShipVisualOrderProvider
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
                harmony.Patch(AccessTools.TypeByName("NavalShipVisualOrderProvider").Method("GetOrders"),
                    postfix: new HarmonyMethod(typeof(Patch_NavalShipVisualOrderProvider).GetMethod(nameof(Postfix_GetOrders), BindingFlags.Static | BindingFlags.Public)));
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
                if (Input.IsGamepadActive)
                {
                    //var movementOrderSet = __result.Where(orderSet => orderSet.StringId == "order_type_movement").FirstOrDefault();
                    //if (movementOrderSet != null)
                    //{
                    //    var stopOrderIndex = movementOrderSet.Orders.FindIndex(order => order.StringId == "order_movement_stop");
                    //    if (stopOrderIndex != -1)
                    //    {
                    //        movementOrderSet.Orders[stopOrderIndex] = newOrder;
                    //    }
                    //}
                    var lastOrderIndex = __result.Count - 1;
                    if (lastOrderIndex >= 0)
                    {
                        __result.Insert(lastOrderIndex, CreateSingleOrderSetFor(newOrder));
                    }
                }
                else
                {
                    var stopOrderIndex = __result.FindIndex(orderSet => orderSet.StringId == "order_movement_stop");
                    if (stopOrderIndex != -1)
                    {
                        __result[stopOrderIndex] = CreateSingleOrderSetFor(newOrder);
                    }
                }
            }
            if (!Input.IsGamepadActive)
            {
                if (RTSCameraConfig.Get().SwitchNavalRetreatAndDelegateCommand)
                {
                    var retreatOrderIndex = __result.FindIndex(orderSet => orderSet.StringId == "order_movement_retreat");
                    var toggleAIIndex = __result.FindIndex(orderSet => orderSet.StringId == "order_toggle_ai");
                    if (retreatOrderIndex == -1 || toggleAIIndex == -1)
                        return;

                    var retreatOrder = __result[retreatOrderIndex];
                    var toggleAIOrder = __result[toggleAIIndex];
                    __result[retreatOrderIndex] = toggleAIOrder;
                    __result[toggleAIIndex] = retreatOrder;
                }
            }
        }

        private static SingleVisualOrderSet CreateSingleOrderSetFor(VisualOrder order)
        {
            return new SingleVisualOrderSet(order);
        }
    }
}
