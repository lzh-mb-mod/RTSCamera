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
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionOrderVM
    {
        private static PropertyInfo _orderSubType = typeof(OrderItemVM).GetProperty("OrderSubType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _focusedFormationsCache = typeof(MissionOrderVM).GetField("_focusedFormationsCache", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _patched;
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
                new CodeInstruction(OpCodes.Brtrue, codes[index__getOrderFlagPosition - 2].operand)
            });
        }

        private static bool TryAddSelectedOrderToQueue(MissionOrderVM __instance)
        {
            var order = GetOrderToAdd(__instance);
            if (order != null)
            {
                CommandQueueLogic.AddOrderToQueue(order);
                return true;
            }
            return false;
        }

        // all the supported command will be added to queue.
        private static OrderInQueue GetOrderToAdd(MissionOrderVM __instance)
        {
            var missionScreen = Utility.GetMissionScreen();
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(__instance.OrderController.SelectedFormations));
            }
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = __instance.OrderController.SelectedFormations
            };
            MBReadOnlyList<Formation> focusedFormations = null;
            switch ((OrderSubType)_orderSubType.GetValue(__instance.LastSelectedOrderItem))
            {
                case OrderSubType.None:
                    return null;
                case OrderSubType.MoveToPosition:
                    Vec3 orderFlagPosition = missionScreen.GetOrderFlagPosition();
                    WorldPosition unitPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, orderFlagPosition, false);
                    if (Mission.Current.IsFormationUnitPositionAvailable(ref unitPosition, Mission.Current.PlayerTeam) && queueCommand)
                    {
                        Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.MoveToLineSegment, __instance.OrderController.SelectedFormations, null, null, null);
                        OrderController.SimulateNewOrderWithPositionAndDirection(__instance.OrderController.SelectedFormations, __instance.OrderController.simulationFormations, unitPosition, unitPosition, out var formationChanges, out var isLineShort, true);
                        orderToAdd.CustomOrderType = CustomOrderType.MoveToLineSegment;
                        orderToAdd.IsLineShort = isLineShort;
                        orderToAdd.ActualFormationChanges = formationChanges;
                        orderToAdd.PositionBegin = unitPosition;
                        orderToAdd.PositionEnd = unitPosition;
                        orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                        break;
                    }
                    return null;
                case OrderSubType.Charge:
                    orderToAdd.OrderType = OrderType.Charge;
                    focusedFormations = (MBReadOnlyList<Formation>)_focusedFormationsCache.GetValue(__instance);
                    if (focusedFormations != null && focusedFormations.Count > 0)
                    {
                        orderToAdd.TargetFormation = focusedFormations[0];
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Charge, __instance.OrderController.SelectedFormations, orderToAdd.TargetFormation, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.FollowMe:
                    // is it ok to save main agent here, and access it later, even if the main agent may become inactive?
                    orderToAdd.OrderType = OrderType.FollowMe;
                    orderToAdd.TargetAgent = Agent.Main;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowMe, __instance.OrderController.SelectedFormations, null, Agent.Main, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.Advance:
                    orderToAdd.OrderType = OrderType.Advance;
                    focusedFormations = (MBReadOnlyList<Formation>)_focusedFormationsCache.GetValue(__instance);
                    if (focusedFormations != null && focusedFormations.Count > 0)
                    {
                        orderToAdd.TargetFormation = focusedFormations[0];
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Advance, __instance.OrderController.SelectedFormations, orderToAdd.TargetFormation, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.Fallback:
                    orderToAdd.OrderType = OrderType.FallBack;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FallBack, __instance.OrderController.SelectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.Stop:
                    orderToAdd.OrderType = OrderType.StandYourGround;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.StandYourGround, __instance.OrderController.SelectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.Retreat:
                    orderToAdd.OrderType = OrderType.Retreat;
                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Retreat, __instance.OrderController.SelectedFormations, null, null, null);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.FormLine:
                    orderToAdd.OrderType = OrderType.ArrangementLine;
                    break;
                case OrderSubType.FormClose:
                    orderToAdd.OrderType = OrderType.ArrangementCloseOrder;
                    break;
                case OrderSubType.FormLoose:
                    orderToAdd.OrderType = OrderType.ArrangementLoose;
                    break;
                case OrderSubType.FormCircular:
                    orderToAdd.OrderType = OrderType.ArrangementCircular;
                    break;
                case OrderSubType.FormSchiltron:
                    orderToAdd.OrderType = OrderType.ArrangementSchiltron;
                    break;
                case OrderSubType.FormV:
                    orderToAdd.OrderType = OrderType.ArrangementVee;
                    break;
                case OrderSubType.FormColumn:
                    orderToAdd.OrderType = OrderType.ArrangementColumn;
                    break;
                case OrderSubType.FormScatter:
                    orderToAdd.OrderType = OrderType.ArrangementScatter;
                    break;
                case OrderSubType.ToggleFacing:
                    orderToAdd.CustomOrderType = CustomOrderType.ToggleFacing;
                    if (queueCommand)
                    {
                        Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, __instance.OrderController, missionScreen);
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(__instance.OrderController.SelectedFormations.Any(f => Patch_OrderController.GetFormationVirtualFacingOrder(f) == OrderType.LookAtDirection) ? OrderType.LookAtEnemy : OrderType.LookAtDirection, __instance.OrderController.SelectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.ToggleFire:
                    orderToAdd.CustomOrderType = CustomOrderType.ToggleFire;
                    break;
                case OrderSubType.ToggleMount:
                    orderToAdd.CustomOrderType = CustomOrderType.ToggleMount;
                    break;
                case OrderSubType.ToggleAI:
                    orderToAdd.CustomOrderType = CustomOrderType.ToggleAI;
                    break;
                case OrderSubType.ToggleTransfer:
                    return null;
                case OrderSubType.ActivationFaceDirection:
                    orderToAdd.OrderType = OrderType.LookAtDirection;
                    if (queueCommand)
                    {
                        Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, __instance.OrderController, missionScreen);
                    }
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, __instance.OrderController.SelectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.FaceEnemy:
                    orderToAdd.OrderType = OrderType.LookAtEnemy;
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, __instance.OrderController.SelectedFormations);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.OrderController.SelectedFormations);
                    break;
                case OrderSubType.Return:
                    return null;
            }
            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(__instance.OrderController.SelectedFormations, orderToAdd);
                return null;
            }
            return orderToAdd;
        }
    }
}
