using HarmonyLib;
using MissionSharedLibrary.Utilities;
using NetworkMessages.FromClient;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionOrderVM
    {
        private static PropertyInfo _orderSubType = typeof(OrderItemVM).GetProperty("OrderSubType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _focusedFormationsCache = typeof(MissionOrderVM).GetField("_focusedFormationsCache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo LastSelectedOrderSetType =
            typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderSetType),
                BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo OrderSubTypeProperty = typeof(OrderItemVM).GetProperty("OrderSubType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo LastSelectedOrderItem = typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderItem),
                BindingFlags.Instance | BindingFlags.Public);
        public static readonly MethodInfo UpdateTitleOrdersKeyVisualVisibility = typeof(MissionOrderVM).GetMethod("UpdateTitleOrdersKeyVisualVisibility", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _patched;

        public static OrderSubType OrderToSelectTarget = OrderSubType.None;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("ApplySelectedOrder",
                        BindingFlags.Public | BindingFlags.Instance),
                    transpiler: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Transpile_ApplySelectedOrder), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    AccessTools.PropertyGetter(typeof(MissionOrderVM), nameof(MissionOrderVM.IsFacingSubOrdersShown)),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_GetIsFacingSubOrdersShown), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod(nameof(MissionOrderVM.OnEscape),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnEscape),
                        BindingFlags.Static | BindingFlags.Public), after: new string[] { "RTSCameraPatch" }));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnOrder),
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

        public static IEnumerable<CodeInstruction> Transpile_ApplySelectedOrder(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            ApplyCommandQueueChange(codes, generator);
            return codes.AsEnumerable();
        }

        private static void ApplyCommandQueueChange(List<CodeInstruction> codes, ILGenerator generator)
        {
            bool found__getOrderFlagPosition = false;
            bool found_get_LastSelectedOrderItem = false;
            int index__getOrderFlagPosition = -1;
            int index_get_LastSelectedOrderItem = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!found__getOrderFlagPosition)
                {
                    if (codes[i].opcode == OpCodes.Ldfld)
                    {
                        if (((FieldInfo)codes[i].operand).Name == "_getOrderFlagPosition")
                        {
                            found__getOrderFlagPosition = true;
                            index__getOrderFlagPosition = i;
                            break;
                        }
                    }
                }
            }
            if (!found__getOrderFlagPosition)
            {
                throw new Exception("_getOrderFlagPosition not found");
            }
            for (int i = index__getOrderFlagPosition; i >= 0; --i)
            {
                if (!found_get_LastSelectedOrderItem)
                {
                    if (codes[i].opcode == OpCodes.Call)
                    {
                        if (((MethodInfo)codes[i].operand).Name == "get_LastSelectedOrderItem")
                        {
                            found_get_LastSelectedOrderItem = true;
                            index_get_LastSelectedOrderItem = i;
                            break;
                        }
                    }
                }
            }
            if (!found_get_LastSelectedOrderItem)
            {
                throw new Exception("get_LastSelectedOrderItem not found");
            }

            codes.InsertRange(index_get_LastSelectedOrderItem - 1, new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(Patch_MissionOrderVM).GetMethod(nameof(TryAddSelectedOrderToQueue), BindingFlags.Static | BindingFlags.NonPublic)),
                // skip native execution if TryAddSelectedOrderToQueue return true.
                new CodeInstruction(OpCodes.Brtrue, codes[index__getOrderFlagPosition - 2].operand)
            });
        }

        private static bool TryAddSelectedOrderToQueue(MissionOrderVM __instance)
        {
            var order = GetOrderToAdd(__instance, out var skipNativeOrder);
            if (order != null)
            {
                CommandQueueLogic.AddOrderToQueue(order);
                return true;
            }
            return skipNativeOrder;
        }

        // all the supported command will be added to queue.
        private static OrderInQueue GetOrderToAdd(MissionOrderVM __instance, out bool skipNativeOrder)
        {
            var missionScreen = Utility.GetMissionScreen();
            skipNativeOrder = false;
            var selectedFormations = __instance.OrderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(selectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(selectedFormations));
            }
            OrderSubType orderSubType = (OrderSubType)_orderSubType.GetValue(__instance.LastSelectedOrderItem);
            bool isSelectTargetForMouseClickingKeyDown = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectTargetForCommand).IsKeyDownInOrder();
            if (!isSelectTargetForMouseClickingKeyDown)
            {
                OrderToSelectTarget = OrderSubType.None;
            }
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            MBReadOnlyList<Formation> focusedFormations = null;
            switch (orderSubType)
            {
                case OrderSubType.None:
                    return null;
                case OrderSubType.MoveToPosition:
                    Vec3 orderFlagPosition = missionScreen.GetOrderFlagPosition();
                    WorldPosition unitPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, orderFlagPosition, false);
                    if (Mission.Current.IsFormationUnitPositionAvailable(ref unitPosition, Mission.Current.PlayerTeam))
                    {
                        Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.MoveToLineSegment, selectedFormations, null, null, null);
                        orderToAdd.OrderType = OrderType.MoveToLineSegment;
                        orderToAdd.PositionBegin = unitPosition;
                        orderToAdd.PositionEnd = unitPosition;
                        if (!queueCommand)
                        {
                            Patch_OrderController.SimulateNewOrderWithPositionAndDirection(selectedFormations, __instance.OrderController.simulationFormations, unitPosition, unitPosition, true, out var simulationAgentFrames, false, out _, out var isLineShort, true, true);
                            orderToAdd.IsLineShort = isLineShort;
                        }
                        else
                        {
                            OrderController.SimulateNewOrderWithPositionAndDirection(selectedFormations, __instance.OrderController.simulationFormations, unitPosition, unitPosition, out var formationChanges, out var isLineShort, true);
                            orderToAdd.IsLineShort = isLineShort;
                            orderToAdd.ActualFormationChanges = formationChanges;
                        }
                        orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    }
                    break;
                case OrderSubType.Charge:
                    orderToAdd.OrderType = OrderType.Charge;
                    focusedFormations = (MBReadOnlyList<Formation>)_focusedFormationsCache.GetValue(__instance);
                    if (focusedFormations != null && focusedFormations.Count > 0)
                    {
                        orderToAdd.TargetFormation = focusedFormations[0];
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Charge, selectedFormations, orderToAdd.TargetFormation, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.FollowMe:
                    // is it ok to save main agent here, and access it later, even if the main agent may become inactive?
                    orderToAdd.OrderType = OrderType.FollowMe;
                    orderToAdd.TargetAgent = Agent.Main;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowMe, selectedFormations, null, Agent.Main, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.Advance:
                    if (isSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == OrderSubType.None)
                    {
                        // Allows to click enemy to select target to advance to.
                        OrderToSelectTarget = OrderSubType.Advance;
                        skipNativeOrder = true;
                        return null;
                    }
                    orderToAdd.OrderType = OrderType.Advance;
                    focusedFormations = (MBReadOnlyList<Formation>)_focusedFormationsCache.GetValue(__instance);
                    if (focusedFormations != null && focusedFormations.Count > 0)
                    {
                        orderToAdd.TargetFormation = focusedFormations[0];
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Advance, selectedFormations, orderToAdd.TargetFormation, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.Fallback:
                    orderToAdd.OrderType = OrderType.FallBack;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FallBack, selectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.Stop:
                    orderToAdd.OrderType = OrderType.StandYourGround;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.StandYourGround, selectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.Retreat:
                    orderToAdd.OrderType = OrderType.Retreat;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Retreat, selectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.FormLine:
                    orderToAdd.OrderType = OrderType.ArrangementLine;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Line, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormClose:
                    orderToAdd.OrderType = OrderType.ArrangementCloseOrder;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.ShieldWall, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormLoose:
                    orderToAdd.OrderType = OrderType.ArrangementLoose;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Loose, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormCircular:
                    orderToAdd.OrderType = OrderType.ArrangementCircular;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Circle, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormSchiltron:
                    orderToAdd.OrderType = OrderType.ArrangementSchiltron;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Square, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormV:
                    orderToAdd.OrderType = OrderType.ArrangementVee;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Skein, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.FormColumn:
                    orderToAdd.OrderType = OrderType.ArrangementColumn;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Column, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        return null;
                    }
                    break;
                case OrderSubType.FormScatter:
                    orderToAdd.OrderType = OrderType.ArrangementScatter;
                    Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, __instance.OrderController.simulationFormations, ArrangementOrder.ArrangementOrderEnum.Scatter, false, out _, true, out _);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        ExecuteArrangementOrder(__instance, orderToAdd);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.ToggleFacing:
                    // see OrderItemVM.OrderSelectionState
                    if (__instance.LastSelectedOrderItem.SelectionState == 3)
                    {
                        // OrderLayoutType 0 means default layout without facing order set.
                        if (BannerlordConfig.OrderLayoutType == 0 && isSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == OrderSubType.None)
                        {
                            // Allows to click ground to select direction to facing to.
                            OrderToSelectTarget = OrderSubType.ActivationFaceDirection;
                            skipNativeOrder = true;
                            return null;
                        }
                        orderToAdd.OrderType = OrderType.LookAtDirection;
                    }
                    else
                    {
                        orderToAdd.OrderType = OrderType.LookAtEnemy;
                    }
                    if (queueCommand && orderToAdd.OrderType == OrderType.LookAtDirection)
                    {
                        Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, __instance.OrderController, missionScreen);
                    }
                    if (!queueCommand)
                    {
                        if (orderToAdd.OrderType == OrderType.LookAtDirection)
                        {
                            __instance.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, missionScreen.GetOrderFlagPosition(), false));
                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                        }
                        else
                        {
                            Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations);
                            __instance.OrderController.SetOrder(OrderType.LookAtEnemy);
                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                        }
                        skipNativeOrder = true;
                        break;
                    }
                    break;
                case OrderSubType.ToggleFire:
                    // selectionState 2 means there's at least 1 formation holding fire, 3 means all formations are holding fire.
                    if (__instance.LastSelectedOrderItem.SelectionState >= 2)
                    {
                        orderToAdd.OrderType = OrderType.FireAtWill;
                    }
                    else
                    {
                        orderToAdd.OrderType = OrderType.HoldFire;
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetFiringOrder(orderToAdd.OrderType, selectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        __instance.OrderController.SetOrder(orderToAdd.OrderType);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.ToggleMount:
                    if (__instance.LastSelectedOrderItem.SelectionState >= 2)
                    {
                        orderToAdd.OrderType = OrderType.Mount;
                    }
                    else
                    {
                        orderToAdd.OrderType = OrderType.Dismount;
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetRidingOrder(orderToAdd.OrderType, selectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        __instance.OrderController.SetOrder(orderToAdd.OrderType);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.ToggleAI:
                    if (__instance.LastSelectedOrderItem.SelectionState >= 2)
                    {
                        orderToAdd.OrderType = OrderType.AIControlOff;
                    }
                    else
                    {
                        orderToAdd.OrderType = OrderType.AIControlOn;
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetAIControlOrder(orderToAdd.OrderType, selectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    if (!queueCommand)
                    {
                        __instance.OrderController.SetOrder(orderToAdd.OrderType);
                        CommandQueueLogic.CancelPendingOrder(selectedFormations);
                        skipNativeOrder = true;
                        return null;
                    }
                    break;
                case OrderSubType.ToggleTransfer:
                    return null;
                case OrderSubType.ActivationFaceDirection:
                    if (isSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == OrderSubType.None)
                    {
                        // Allows to click ground to select direction to facing to.
                        OrderToSelectTarget = OrderSubType.ActivationFaceDirection;
                        skipNativeOrder = true;
                        return null;
                    }
                    orderToAdd.OrderType = OrderType.LookAtDirection;

                    if (queueCommand)
                    {
                        Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, __instance.OrderController, missionScreen);
                    }
                    else
                    {
                        __instance.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, missionScreen.GetOrderFlagPosition(), false));
                        orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                        skipNativeOrder = true;
                        break;
                    }
                    break;
                case OrderSubType.FaceEnemy:
                    orderToAdd.OrderType = OrderType.LookAtEnemy;
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                    break;
                case OrderSubType.Return:
                    return null;
            }
            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(selectedFormations, orderToAdd);
                return null;
            }
            return orderToAdd;
        }

        private static void ExecuteArrangementOrder(MissionOrderVM __instance, OrderInQueue order)
        {
            Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(order.SelectedFormations));
            __instance.OrderController.SetOrder(order.OrderType);
            foreach (var pair in order.VirtualFormationChanges)
            {
                var formation = pair.Key;
                var change = pair.Value;
                formation.SetPositioning(unitSpacing: change.UnitSpacing);
                if (change.Width != null)
                {
                    formation.FormOrder = FormOrder.FormOrderCustom(change.Width.Value);
                }
            }
            CommandQueueLogic.TryTeleportSelectedFormationInDeployment(__instance.OrderController, order.SelectedFormations);
            CommandQueueLogic.CurrentFormationChanges.SetChanges(order.VirtualFormationChanges);
        }

        public static bool Prefix_GetIsFacingSubOrdersShown(MissionOrderVM __instance, ref bool __result)
        {
            if (OrderToSelectTarget == OrderSubType.ActivationFaceDirection)
            {
                __result = true;
                Patch_OrderTroopPlacer.SetIsDraingFacing(true);
                return false;
            }
            return true;
        }

        public static bool Prefix_OnEscape(MissionOrderVM __instance)
        {
            if (OrderToSelectTarget != OrderSubType.None)
            {
                OrderToSelectTarget = OrderSubType.None;
                return false;
            }
            return true;
        }

        public static bool Prefix_OnOrder(MissionOrderVM __instance, OrderItemVM orderItem, OrderSetType orderSetType, bool fromSelection, ref MissionOrderVM.ActivationType ____currentActivationType)
        {

            if (__instance.LastSelectedOrderItem == orderItem && fromSelection)
                return false;
            (typeof(MissionOrderVM).GetField("_onBeforeOrderDelegate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as OnBeforeOrderDelegate).Invoke();
            if (__instance.LastSelectedOrderItem != null)
                __instance.LastSelectedOrderItem.IsSelected = false;
            //if (orderItem.IsTitle)
            //    this.LastSelectedOrderSetType = orderSetType;
            LastSelectedOrderItem.SetValue(__instance, orderItem);

            var orderSetsWithOrdersByType = typeof(MissionOrderVM).GetField("OrderSetsWithOrdersByType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as Dictionary<OrderSetType, OrderSetVM>;

            if (__instance.LastSelectedOrderItem != null)
            {
                __instance.LastSelectedOrderItem.IsSelected = true;
                if ((OrderSubType)OrderSubTypeProperty.GetValue(__instance.LastSelectedOrderItem) == OrderSubType.None)
                {
                    if (orderSetsWithOrdersByType.ContainsKey(__instance.LastSelectedOrderSetType))
                        orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
                    LastSelectedOrderSetType.SetValue(__instance, orderSetType);
                    orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = true;
                }
            }
            if (__instance.LastSelectedOrderItem != null && (OrderSubType)OrderSubTypeProperty.GetValue(__instance.LastSelectedOrderItem) != OrderSubType.None && !fromSelection)
            {
                OrderSetVM orderSetVm;
                if ((OrderSubType)OrderSubTypeProperty.GetValue(__instance.LastSelectedOrderItem) == OrderSubType.Return && orderSetsWithOrdersByType.TryGetValue(__instance.LastSelectedOrderSetType, out orderSetVm))
                {
                    orderSetVm.ShowOrders = false;
                    UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
                    LastSelectedOrderSetType.SetValue(__instance, OrderSetType.None);
                }
                else if (____currentActivationType == MissionOrderVM.ActivationType.Hold && __instance.LastSelectedOrderSetType != OrderSetType.None)
                {
                    __instance.ApplySelectedOrder();
                    if (__instance.LastSelectedOrderItem != null && __instance.LastSelectedOrderSetType != OrderSetType.None)
                    {
                        orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
                        UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
                        LastSelectedOrderItem.SetValue(__instance, (object)null);
                    }
                    __instance.OrderSets.ApplyActionOnAllItems((Action<OrderSetVM>)(s => s.Orders.ApplyActionOnAllItems((Action<OrderItemVM>)(o => o.IsSelected = false))));
                }
                else if (__instance.IsDeployment)
                    __instance.ApplySelectedOrder();
                else
                    __instance.TryCloseToggleOrder();
            }
            if (fromSelection)
                return false;
            UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);


            // TODO: don't close the order ui and open it again.
            // Keep orders UI open after issuing an order in free camera mode.
            //if (!__instance.IsToggleOrderShown && !__instance.TroopController.IsTransferActive && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            //{
            //    __instance.OpenToggleOrder(false);
            //}
            //var orderTroopPlacer = Mission.Current.GetMissionBehavior<OrderTroopPlacer>();
            //if (orderTroopPlacer != null)
            //{
            //    typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic)
            //        .Invoke(orderTroopPlacer, null);
            //}
            //var orderUIHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            //if (orderUIHandler == null)
            //{
            //    return false;
            //}
            //var missionOrderVM = typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orderUIHandler) as MissionOrderVM;
            //var setActiveOrders = typeof(MissionOrderVM).GetMethod("SetActiveOrders", BindingFlags.Instance | BindingFlags.NonPublic);
            //setActiveOrders.Invoke(missionOrderVM, new object[] { });

            return false;
        }
    }
}
