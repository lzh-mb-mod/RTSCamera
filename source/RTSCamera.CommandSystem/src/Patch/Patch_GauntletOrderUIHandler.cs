using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
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

        private static List<Action> _callbackList = new List<Action>();

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
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }
        }

        public static void OnBehaviorInitialize()
        {
            _callbackList = new List<Action>();
        }

        public static void OnRemoveBehavior()
        {
            _callbackList = null;
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
            if (__instance.Mission.IsNavalBattle)
                return false;
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
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
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
                            bool keepMovementOrder = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepMovementOrder).IsKeyDownInOrder(__instance.Input);
                            bool shouldIgnore = CommandSystemConfig.Get().DisableNativeAttack && RTSCommandVisualOrder.OrderToSelectTarget == SelectTargetMode.None;
                            var orderTroopPlacer = _orderTroopPlacer.GetValue(__instance) as OrderTroopPlacer;
                            if (orderTroopPlacer != null && (!shouldIgnore || keepMovementOrder))
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
                                orderToAdd.ShouldAdjustFormationSpeed = Utilities.Utility.ShouldLockFormation();
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
                                if (keepMovementOrder)
                                {
                                    Utilities.Utility.FocusOnFormation(dataSource.OrderController, focusedFormationCache[0]);
                                    skipNativeOrder = true;
                                    return null;
                                }
                                if (shouldIgnore)
                                {
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
                                            var usable = focusedOrderableObject as UsableMachine;
                                            bool shouldFollowEntity = false;
                                            if (usable == null)
                                            {
                                                shouldFollowEntity = true;
                                            }
                                            else
                                            {
                                                IEnumerable<Formation> source = selectedFormations.Where(new Func<Formation, bool>(usable.IsUsedByFormation));
                                                shouldFollowEntity = source.IsEmpty();
                                            }
                                            if (shouldFollowEntity)
                                            {
                                                if (usable != null)
                                                {
                                                    var waitEntity = usable.WaitEntity;
                                                    if (waitEntity != null)
                                                    {
                                                        var direction = waitEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
                                                        Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, selectedFormations);
                                                        foreach (var formation in selectedFormations)
                                                        {
                                                            Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                                        }
                                                    }
                                                }
                                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, selectedFormations, null, null, focusedOrderableObject);
                                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                                if (!queueCommand)
                                                {
                                                    var siegeWeapon = usable as SiegeWeapon;
                                                    if (siegeWeapon != null)
                                                    {
                                                        siegeWeapon.SetForcedUse(true);
                                                    }
                                                }
                                                Utilities.Utility.DisplayExecuteOrderMessage(selectedFormations, orderToAdd);
                                                break;
                                            }
                                            else
                                            {
                                                orderToAdd.CustomOrderType = CustomOrderType.StopUsing;
                                                orderToAdd.OrderType = OrderType.Use;
                                                orderToAdd.IsStopUsing = true;
                                                if (!queueCommand)
                                                {
                                                    // native order will follow entity.
                                                    skipNativeOrder = true;
                                                    var siegeWeapon = usable as SiegeWeapon;
                                                    if (siegeWeapon != null)
                                                    {
                                                        siegeWeapon.SetForcedUse(false);
                                                    }
                                                    foreach (var formation in selectedFormations)
                                                    {
                                                        formation.SetControlledByAI(false);
                                                        formation.StopUsingMachine(usable, true);
                                                    }
                                                    Utilities.Utility.CallAfterSetOrder(dataSource.OrderController, OrderType.StandYourGround);
                                                    CommandQueueLogic.OnCustomOrderIssued(orderToAdd, dataSource.OrderController);
                                                }
                                                Utilities.Utility.DisplayExecuteOrderMessage(selectedFormations, orderToAdd);
                                                // This is required to keep MissionOrderVM open in rts mode and close it in player mode.
                                                var missionOrderVM = MissionSharedLibrary.Utilities.Utility.GetMissionOrderVM(Mission.Current);
                                                var orderItem = MissionSharedLibrary.Utilities.Utility.FindOrderWithId(missionOrderVM, "order_movement_stop");
                                                if (orderItem != null)
                                                {
                                                    missionOrderVM.OnOrderExecuted(orderItem);
                                                }
                                                break;
                                            }
                                        }
                                    case OrderType.Use:
                                        {
                                            var usable = focusedOrderableObject as UsableMachine;
                                            IEnumerable<Formation> source = selectedFormations.Where(new Func<Formation, bool>(usable.IsUsedByFormation));
                                            orderToAdd.OrderType = OrderType.Use;
                                            orderToAdd.TargetEntity = usable;
                                            if (source.IsEmpty())
                                            {
                                                if (usable.HasWaitFrame)
                                                {
                                                    orderToAdd.OrderType = OrderType.FollowEntity;
                                                    var waitEntity = usable.WaitEntity;
                                                    if (waitEntity != null)
                                                    {
                                                        var direction = waitEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
                                                        Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, selectedFormations);
                                                        foreach (var formation in selectedFormations)
                                                        {
                                                            Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                                        }
                                                    }
                                                    Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, selectedFormations, null, null, focusedOrderableObject);
                                                }
                                                if (!queueCommand)
                                                {
                                                    var siegeWeapon = usable as SiegeWeapon;
                                                    if (siegeWeapon != null)
                                                    {
                                                        siegeWeapon.SetForcedUse(true);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                orderToAdd.CustomOrderType = CustomOrderType.StopUsing;
                                                orderToAdd.OrderType = OrderType.Use;
                                                orderToAdd.IsStopUsing = true;
                                                if (!queueCommand)
                                                {
                                                    var siegeWeapon = usable as SiegeWeapon;
                                                    if (siegeWeapon != null)
                                                    {
                                                        siegeWeapon.SetForcedUse(false);
                                                    }
                                                    Utilities.Utility.CallAfterSetOrder(dataSource.OrderController, OrderType.StandYourGround);
                                                }
                                            }
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

                                            Utilities.Utility.DisplayExecuteOrderMessage(selectedFormations, orderToAdd);
                                            break;
                                        }
                                    case OrderType.AttackEntity:
                                        {
                                            orderToAdd.OrderType = OrderType.AttackEntity;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.AttackEntity, selectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            Utilities.Utility.DisplayExecuteOrderMessage(selectedFormations, orderToAdd);
                                            break;
                                        }
                                    case OrderType.PointDefence:
                                        {
                                            orderToAdd.OrderType = OrderType.PointDefence;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.PointDefence, selectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                                            Utilities.Utility.DisplayExecuteOrderMessage(selectedFormations, orderToAdd);
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
                        orderToAdd.ShouldAdjustFormationSpeed = Utilities.Utility.ShouldLockFormation();
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
                            var missionOrderVM = MissionSharedLibrary.Utilities.Utility.GetMissionOrderVM(Mission.Current);
                            var orderItem = MissionSharedLibrary.Utilities.Utility.FindOrderWithId(missionOrderVM, "order_toggle_facing");
                            if (orderItem != null)
                            {
                                missionOrderVM.OnOrderExecuted(orderItem);
                            }
                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                        }
                        dataSource.SelectedOrderSet?.ExecuteDeSelect();
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
            UpdateOrderTroopPlacerDrawingFacing(__instance, ____dataSource, ____gauntletLayer, ____orderTroopPlacer);
            UpdateCallbackList();
            //return true;
        }

        private static void UpdateMouseVisibility(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, ref bool ____isTransferEnabled)
        {
            if (__instance == null)
                return;

            bool mouseVisibility =
                (__instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ||
                 ____dataSource.IsToggleOrderShown && (__instance.Input.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null));
            var inputUsageMask = __instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ? InputUsageMask.All : CommandSystemConfig.Get().OrderUIClickable ? InputUsageMask.All : InputUsageMask.Invalid;
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

        private static void UpdateOrderTroopPlacerDrawingFacing(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, OrderTroopPlacer ____orderTroopPlacer)
        {
            if (__instance.IsValidForTick && ____dataSource != null && ____gauntletLayer.IsActive && ____dataSource.IsToggleOrderShown && CommandSystemConfig.Get().OrderUIClickable)
            {
                ____orderTroopPlacer.IsDrawingFacing = ____dataSource.SelectedOrderSet?.OrderIconId == "order_type_facing" || RTSCommandVisualOrder.OrderToSelectTarget == SelectTargetMode.LookAtDirection;
            }
        }

        private static void SetSiegeWeaponForceUseNextTick(SiegeWeapon siegeWeapon, bool forceUse)
        {
            _callbackList.Add(() =>
            {
                siegeWeapon.SetForcedUse(forceUse);
            });
        }

        private static void UpdateCallbackList()
        {
            if (_callbackList.Count == 0)
                return;

            var next = _callbackList[_callbackList.Count - 1];
            _callbackList.RemoveAt(_callbackList.Count - 1);
            next();
        }
    }
}
