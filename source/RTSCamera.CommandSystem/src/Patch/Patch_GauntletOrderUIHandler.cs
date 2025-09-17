using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_GauntletOrderUIHandler
    {
        private static FieldInfo _focusedFormationsCache = typeof(GauntletOrderUIHandler).GetField("_focusedFormationsCache", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo _dataSource =
            typeof(GauntletOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _targetFormationOrderGivenWithActionButton =
            typeof(GauntletOrderUIHandler).GetField(nameof(_targetFormationOrderGivenWithActionButton),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _orderTroopPlacer =
            typeof(GauntletOrderUIHandler).GetField(nameof(_orderTroopPlacer),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                typeof(GauntletOrderUIHandler).GetMethod("TickInput",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                    transpiler: new HarmonyMethod(typeof(Patch_GauntletOrderUIHandler).GetMethod(
                        nameof(Transpile_TickInput), BindingFlags.Static | BindingFlags.Public)));

                harmony.Patch(
                    typeof(GauntletOrderUIHandler).GetMethod(
                        nameof(GauntletOrderUIHandler.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_GauntletOrderUIHandler).GetMethod(
                        nameof(Postfix_OnMissionScreenTick), BindingFlags.Static | BindingFlags.Public), before: new string[] {"RTSCameraPatch"}));
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                return false;
            }
        }

        public static IEnumerable<CodeInstruction> Transpile_TickInput(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            ApplyCommandQueueChange(codes);
            return codes.AsEnumerable();

        }

        private static void ApplyCommandQueueChange(List<CodeInstruction> codes)
        {
            bool found_get_ActiveTargetState = false;
            //bool found_get_cursorState = false;
            //bool found_switch = false;
            int index_get_ActiveTargetState = -1;
            //int index_get_cursorState = -1;
            //int index_switch = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!found_get_ActiveTargetState)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        if ((codes[i].operand as MethodInfo).Name == "get_ActiveTargetState")
                        {
                            found_get_ActiveTargetState = true;
                            index_get_ActiveTargetState = i;
                            break;
                        }
                    }
                }
                //if (!found_get_cursorState)
                //{
                //    if (codes[i].opcode == OpCodes.Call)
                //    {
                //        if ((codes[i].operand as MethodInfo).Name == "get_cursorState")
                //        {
                //            found_get_cursorState = true;
                //            index_get_cursorState = i;
                //        }
                //    }
                //}
                //else if (!found_switch)
                //{
                //    if (codes[i].opcode == OpCodes.Switch)
                //    {
                //        found_switch = true;
                //        index_switch = i;
                //        break;
                //    }
                //}
            }
            if (!found_get_ActiveTargetState)
            {
                throw new Exception("get_ActiveTargetState not found");
            }
            //if (!found_get_cursorState)
            //{
            //    throw new Exception("get_cursorState not found");
            //}
            //if (!found_switch)
            //{
            //    throw new Exception("switch not found");
            //}
            codes.InsertRange(index_get_ActiveTargetState - 2, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(Patch_GauntletOrderUIHandler).GetMethod(nameof(TryAddSelectedOrderToQueue), BindingFlags.Static | BindingFlags.NonPublic)),
                new CodeInstruction(OpCodes.Brtrue, codes[index_get_ActiveTargetState + 1].operand)
            });
            //codes.InsertRange(index_get_cursorState - 1, new List<CodeInstruction>
            //{
            //    new CodeInstruction(OpCodes.Ldarg_0),
            //    new CodeInstruction(OpCodes.Call, typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(nameof(TryAddSelectedOrderToQueue), BindingFlags.Static | BindingFlags.NonPublic)),
            //    new CodeInstruction(OpCodes.Brtrue, codes[index_switch + 1].operand)
            //});
        }

        private static bool TryAddSelectedOrderToQueue(GauntletOrderUIHandler __instance)
        {
            var dataSource = _dataSource.GetValue(__instance) as MissionOrderVM;
            if (dataSource.ActiveTargetState == 0 && (__instance.Input.IsKeyReleased(InputKey.LeftMouseButton) || __instance.Input.IsKeyReleased(InputKey.ControllerRTrigger)))
            {
                OrderSetVM selectedOrderSet = dataSource.SelectedOrderSet;
                if (selectedOrderSet != null && Input.IsGamepadActive)
                {
                    // return false to run original code.
                    return false;
                }
                else
                {
                    var order = GetOrderToAdd(__instance, dataSource, out var skipNativeOrder);
                    if (order != null)
                    {
                        CommandQueueLogic.AddOrderToQueue(order);
                        return true;
                    }
                    return skipNativeOrder;
                }
            }
            return false;
        }

        private static OrderInQueue GetOrderToAdd(GauntletOrderUIHandler __instance, MissionOrderVM dataSource, out bool skipNativeOrder)
        {
            var missionScreen = __instance.MissionScreen;
            skipNativeOrder = false;
            if (dataSource == null)
                return null;
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            var selectedFormations = dataSource.OrderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            if (selectedFormations.Count == 0)
                return null;
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(selectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(selectedFormations));
            }
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            switch (__instance.CursorState)
            {
                case MissionOrderVM.CursorStates.Move:
                    {
                        var focusedFormationCache = _focusedFormationsCache.GetValue(__instance) as MBReadOnlyList<Formation>;
                        if (focusedFormationCache != null && focusedFormationCache.Count > 0)
                        {
                            var orderTroopPlacer = _orderTroopPlacer.GetValue(__instance) as OrderTroopPlacer;
                            if (orderTroopPlacer != null)
                            {
                                orderTroopPlacer.SuspendTroopPlacer = true;
                                _targetFormationOrderGivenWithActionButton?.SetValue(__instance, true);
                            }
                            if (RTSCommandVisualOrder.OrderToSelectTarget == SelectTargetMode.Advance)
                            {
                                orderToAdd.OrderType = OrderType.Advance;
                                orderToAdd.TargetFormation = focusedFormationCache[0];
                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Advance, selectedFormations, focusedFormationCache[0], null, null);
                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
                                if (!queueCommand)
                                {
                                    skipNativeOrder = true;
                                    dataSource.OrderController.SetOrderWithFormation(OrderType.Advance, focusedFormationCache[0]);
                                }
                            }
                            else if (RTSCommandVisualOrder.OrderToSelectTarget == SelectTargetMode.LookAtEnemy)
                            {
                                orderToAdd.OrderType = OrderType.LookAtEnemy;
                                orderToAdd.TargetFormation = focusedFormationCache[0];
                                Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, focusedFormationCache[0]);
                                if (!queueCommand)
                                {
                                    skipNativeOrder = true;
                                    Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, orderToAdd.TargetFormation);
                                    dataSource.OrderController.SetOrder(OrderType.LookAtEnemy);
                                }
                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
                            }
                            else
                            {
                                bool keepMovementOrder = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepMovementOrder).IsKeyDownInOrder(__instance.Input);
                                if (keepMovementOrder)
                                {
                                    Utilities.Utility.FocusOnFormation(dataSource.OrderController, focusedFormationCache[0]);
                                    skipNativeOrder = true;
                                    return null;
                                }
                                orderToAdd.OrderType = OrderType.Charge;
                                orderToAdd.TargetFormation = focusedFormationCache[0];
                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Charge, selectedFormations, focusedFormationCache[0], null, null);
                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                            }
                            break;
                        }
                        var focusedOrderableObject = __instance.MissionScreen.OrderFlag.FocusedOrderableObject;
                        if (focusedOrderableObject != null)
                        {
                            if (selectedFormations.Count > 0)
                            {
                                BattleSideEnum side = selectedFormations[0].Team.Side;
                                var orderType = focusedOrderableObject.GetOrder(side);
                                var missionObject = focusedOrderableObject as MissionObject;
                                switch (orderType)
                                {
                                    case OrderType.Move:
                                        {
                                            WorldPosition position = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, missionObject.GameEntity.GlobalPosition, false);
                                            orderToAdd.OrderType = OrderType.Move;
                                            orderToAdd.PositionBegin = position;
                                            foreach (var formation in selectedFormations)
                                            {
                                                Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                            }
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Move, selectedFormations, null, null, null);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            break;
                                        }
                                    case OrderType.MoveToLineSegment:
                                    case OrderType.MoveToLineSegmentWithHorizontalLayout:
                                        {
                                            IPointDefendable pointDefendable = focusedOrderableObject as IPointDefendable;
                                            Vec3 globalPosition1 = pointDefendable.DefencePoints.Last().GameEntity.GlobalPosition;
                                            Vec3 globalPosition2 = pointDefendable.DefencePoints.First().GameEntity.GlobalPosition;
                                            if (selectedFormations.Count > 0 && queueCommand)
                                            {
                                                orderToAdd.OrderType = orderType == OrderType.MoveToLineSegment ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout;
                                                WorldPosition targetLineSegmentBegin = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, globalPosition1, false);
                                                WorldPosition targetLineSegmentEnd = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, globalPosition2, false);
                                                OrderController.SimulateNewOrderWithPositionAndDirection(selectedFormations, dataSource.OrderController.simulationFormations,
                                                    targetLineSegmentBegin, targetLineSegmentEnd, out var formationChanges, out var isLineShort, orderType == OrderType.MoveToLineSegment);
                                                orderToAdd.IsLineShort = isLineShort;
                                                orderToAdd.ActualFormationChanges = formationChanges;
                                                orderToAdd.PositionBegin = targetLineSegmentBegin;
                                                orderToAdd.PositionEnd = targetLineSegmentEnd;
                                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(orderType == OrderType.MoveToLineSegment ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout, selectedFormations, null, null, null);
                                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                                break;
                                            }
                                            return null;
                                        }
                                    case OrderType.FollowEntity:
                                        {
                                            orderToAdd.OrderType = OrderType.FollowEntity;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, selectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            break;
                                        }
                                    case OrderType.Use:
                                        {
                                            var usable = focusedOrderableObject as UsableMachine;
                                            IEnumerable<Formation> source = selectedFormations.Where(new Func<Formation, bool>(usable.IsUsedByFormation));
                                            if (source.IsEmpty())
                                            {
                                                //foreach (Formation formation in selectedFormations)
                                                //    formation.StartUsingMachine(usable, true);
                                                if (!usable.HasWaitFrame)
                                                    // will not be added to queue because orderToAdd.OrderType is OrderType.None.
                                                    break;
                                                orderToAdd.OrderType = OrderType.FollowEntity;
                                                orderToAdd.TargetEntity = usable;
                                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, selectedFormations, null, null, focusedOrderableObject);
                                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            }
                                            else
                                            {
                                                //foreach (Formation formation in source)
                                                //    formation.StopUsingMachine(usable, true);
                                                // will not be added to queue because orderToAdd.OrderType is OrderType.None.
                                            }
                                            break;
                                        }
                                    case OrderType.AttackEntity:
                                        {
                                            orderToAdd.OrderType = OrderType.AttackEntity;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.AttackEntity, selectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            break;
                                        }
                                    case OrderType.PointDefence:
                                        {
                                            orderToAdd.OrderType = OrderType.PointDefence;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.PointDefence, selectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            break;
                                        }
                                }
                            }
                        }
                        break;
                    }
                case MissionOrderVM.CursorStates.Face:
                    {
                        orderToAdd.OrderType = OrderType.LookAtDirection;
                        RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
                        if (queueCommand)
                        {
                            Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, dataSource.OrderController, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, __instance.MissionScreen.GetOrderFlagPosition(), false));
                        }
                        else
                        {
                            skipNativeOrder = true;
                            // only pending order for formations that should be locked.
                            orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                            dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, __instance.MissionScreen.GetOrderFlagPosition(), false));
                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                        }
                        break;
                    }
                case MissionOrderVM.CursorStates.Form:
                    return null;
            }
            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                return null;
            }
            return orderToAdd;
        }
        public static void Postfix_OnMissionScreenTick(GauntletOrderUIHandler __instance, ref float ____latestDt, ref bool ____isReceivingInput, float dt, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, OrderTroopPlacer ____orderTroopPlacer, ref bool ____isTransferEnabled)
        {
            UpdateMouseVisibility(__instance, ____dataSource, ____gauntletLayer, ref ____isTransferEnabled);
            //return true;
        }

        private static void UpdateMouseVisibility(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, ref bool ____isTransferEnabled)
        {
            if (__instance == null)
                return;

            bool mouseVisibility =
                (__instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ||
                 ____dataSource.IsToggleOrderShown && (__instance.Input.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null));
            var inputUsageMask = __instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ? InputUsageMask.All : InputUsageMask.MouseButtons;
            var layer = ____gauntletLayer;
            if (mouseVisibility != layer.InputRestrictions.MouseVisibility || inputUsageMask != layer.InputRestrictions.InputUsageMask)
            {
                layer.InputRestrictions.SetInputRestrictions(mouseVisibility,
                    inputUsageMask);
            }
            if (____dataSource.TroopController.IsTransferActive != ____isTransferEnabled)
            {
                ____isTransferEnabled = ____dataSource.TroopController.IsTransferActive;
                if (!____isTransferEnabled)
                {
                    ____gauntletLayer.UIContext.ContextAlpha = BannerlordConfig.HideBattleUI ? 0.0f : 1f;
                    ____gauntletLayer.IsFocusLayer = false;
                    ScreenManager.TryLoseFocus(____gauntletLayer);
                }
                else
                {
                    ____gauntletLayer.UIContext.ContextAlpha = 1f;
                    ____gauntletLayer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(____gauntletLayer);
                }
            }

            //if (__instance.MissionScreen.OrderFlag != null)
            //{
            //    bool orderFlagVisibility = (____dataSource.IsToggleOrderShown || IsAnyDeployment(__instance)) &&
            //                               !____dataSource.TroopController.IsTransferActive &&
            //                               !_rightButtonDraggingMode && !_earlyDraggingMode;
            //    if (orderFlagVisibility != __instance.MissionScreen.OrderFlag.IsVisible)
            //    {
            //        __instance.MissionScreen.SetOrderFlagVisibility(orderFlagVisibility);
            //    }
            //}
        }
    }
}
