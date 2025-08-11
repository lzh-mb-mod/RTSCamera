using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionGauntletSingleplayerOrderUIHandler
    {
        private static FieldInfo _focusedFormationsCache = typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_focusedFormationsCache", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo _dataSource =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _targetFormationOrderGivenWithActionButton =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_targetFormationOrderGivenWithActionButton),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _orderTroopPlacer =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_orderTroopPlacer),
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
                typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod("TickInput",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                    transpiler: new HarmonyMethod(typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(Transpile_TickInput), BindingFlags.Static | BindingFlags.Public)));


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
                new CodeInstruction(OpCodes.Call, typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(nameof(TryAddSelectedOrderToQueue), BindingFlags.Static | BindingFlags.NonPublic)),
                new CodeInstruction(OpCodes.Brtrue, codes[index_get_ActiveTargetState + 1].operand)
            });
            //codes.InsertRange(index_get_cursorState - 1, new List<CodeInstruction>
            //{
            //    new CodeInstruction(OpCodes.Ldarg_0),
            //    new CodeInstruction(OpCodes.Call, typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(nameof(TryAddSelectedOrderToQueue), BindingFlags.Static | BindingFlags.NonPublic)),
            //    new CodeInstruction(OpCodes.Brtrue, codes[index_switch + 1].operand)
            //});
        }

        private static bool TryAddSelectedOrderToQueue(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            var dataSource = _dataSource.GetValue(__instance) as MissionOrderVM;
            if (dataSource.ActiveTargetState == 0 && (__instance.Input.IsKeyReleased(InputKey.LeftMouseButton) || __instance.Input.IsKeyReleased(InputKey.ControllerRTrigger)))
            {
                OrderItemVM selectedOrderItem = dataSource.LastSelectedOrderItem;
                if (selectedOrderItem != null && !selectedOrderItem.IsTitle && Input.IsGamepadActive)
                {
                    // return false to run original code.
                    return false;
                }
                else
                {
                    var order = GetOrderToAdd(__instance, dataSource);
                    if (order != null)
                    {
                        CommandQueueLogic.AddOrderToQueue(order);
                        return true;
                    }
                }
            }
            return false;
        }

        private static OrderInQueue GetOrderToAdd(MissionGauntletSingleplayerOrderUIHandler __instance, MissionOrderVM dataSource)
        {
            var missionScreen = __instance.MissionScreen;
            if (dataSource == null)
                return null;
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(dataSource.OrderController.SelectedFormations));
            }
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = dataSource.OrderController.SelectedFormations
            };
            switch (__instance.cursorState)
            {
                case MissionOrderVM.CursorState.Move:
                    {
                        var focusedFormationCache = _focusedFormationsCache.GetValue(__instance) as MBReadOnlyList<Formation>;
                        if (focusedFormationCache != null && focusedFormationCache.Count > 0)
                        {
                            orderToAdd.OrderType = OrderType.Charge;
                            orderToAdd.TargetFormation = focusedFormationCache[0];
                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Charge, dataSource.OrderController.SelectedFormations, focusedFormationCache[0], null, null);
                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                            var orderTroopPlacer = _orderTroopPlacer.GetValue(__instance) as OrderTroopPlacer;
                            if (orderTroopPlacer != null)
                            {
                                orderTroopPlacer.SuspendTroopPlacer = true;
                                _targetFormationOrderGivenWithActionButton?.SetValue(__instance, true);
                            }
                            break;
                        }
                        var focusedOrderableObject = __instance.MissionScreen.OrderFlag.FocusedOrderableObject;
                        if (focusedOrderableObject != null)
                        {
                            if (dataSource.OrderController.SelectedFormations.Count > 0)
                            {
                                BattleSideEnum side = dataSource.OrderController.SelectedFormations[0].Team.Side;
                                var orderType = focusedOrderableObject.GetOrder(side);
                                var missionObject = focusedOrderableObject as MissionObject;
                                switch (orderType)
                                {
                                    case OrderType.Move:
                                        {
                                            WorldPosition position = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, missionObject.GameEntity.GlobalPosition, false);
                                            orderToAdd.OrderType = OrderType.Move;
                                            orderToAdd.PositionBegin = position;
                                            foreach (var formation in dataSource.OrderController.SelectedFormations)
                                            {
                                                Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                            }
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Move, dataSource.OrderController.SelectedFormations, null, null, null);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                                            break;
                                        }
                                    case OrderType.MoveToLineSegment:
                                    case OrderType.MoveToLineSegmentWithHorizontalLayout:
                                        {
                                            IPointDefendable pointDefendable = focusedOrderableObject as IPointDefendable;
                                            Vec3 globalPosition1 = pointDefendable.DefencePoints.Last().GameEntity.GlobalPosition;
                                            Vec3 globalPosition2 = pointDefendable.DefencePoints.First().GameEntity.GlobalPosition;
                                            IEnumerable<Formation> formations = dataSource.OrderController.SelectedFormations.Where((f => f.CountOfUnitsWithoutDetachedOnes > 0));
                                            if (formations.Any() && queueCommand)
                                            {
                                                orderToAdd.OrderType = orderType == OrderType.MoveToLineSegment ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout;
                                                orderToAdd.SelectedFormations = formations.ToList();
                                                WorldPosition targetLineSegmentBegin = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, globalPosition1, false);
                                                WorldPosition targetLineSegmentEnd = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, globalPosition2, false);
                                                OrderController.SimulateNewOrderWithPositionAndDirection(formations, dataSource.OrderController.simulationFormations,
                                                    targetLineSegmentBegin, targetLineSegmentEnd, out var formationChanges, out var isLineShort, orderType == OrderType.MoveToLineSegment);
                                                orderToAdd.IsLineShort = isLineShort;
                                                orderToAdd.ActualFormationChanges = formationChanges;
                                                orderToAdd.PositionBegin = targetLineSegmentBegin;
                                                orderToAdd.PositionEnd = targetLineSegmentEnd;
                                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(orderType == OrderType.MoveToLineSegment ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout, dataSource.OrderController.SelectedFormations, null, null, null);
                                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(formations);
                                                break;
                                            }
                                            return null;
                                        }
                                    case OrderType.FollowEntity:
                                        {
                                            orderToAdd.OrderType = OrderType.FollowEntity;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, dataSource.OrderController.SelectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                                            break;
                                        }
                                    case OrderType.Use:
                                        {
                                            var usable = focusedOrderableObject as UsableMachine;
                                            IEnumerable<Formation> source = dataSource.OrderController.SelectedFormations.Where(new Func<Formation, bool>(usable.IsUsedByFormation));
                                            if (source.IsEmpty())
                                            {
                                                foreach (Formation formation in dataSource.OrderController.SelectedFormations)
                                                    formation.StartUsingMachine(usable, true);
                                                if (!usable.HasWaitFrame)
                                                    // will not be added to queue because orderToAdd.OrderType is OrderType.None.
                                                    break;
                                                orderToAdd.OrderType = OrderType.FollowEntity;
                                                orderToAdd.TargetEntity = usable;
                                                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.FollowEntity, dataSource.OrderController.SelectedFormations, null, null, focusedOrderableObject);
                                                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                                            }
                                            else
                                            {
                                                foreach (Formation formation in source)
                                                    formation.StopUsingMachine(usable, true);
                                                // will not be added to queue because orderToAdd.OrderType is OrderType.None.
                                            }
                                            break;
                                        }
                                    case OrderType.AttackEntity:
                                        {
                                            orderToAdd.OrderType = OrderType.AttackEntity;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.AttackEntity, dataSource.OrderController.SelectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                                            break;
                                        }
                                    case OrderType.PointDefence:
                                        {
                                            orderToAdd.OrderType = OrderType.PointDefence;
                                            orderToAdd.TargetEntity = focusedOrderableObject;
                                            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.PointDefence, dataSource.OrderController.SelectedFormations, null, null, focusedOrderableObject);
                                            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                                            break;
                                        }
                                }
                            }
                        }
                        break;
                    }
                case MissionOrderVM.CursorState.Face:
                    {
                        orderToAdd.OrderType = OrderType.LookAtDirection;
                        if (queueCommand)
                        {
                            Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, dataSource.OrderController, missionScreen);
                            Patch_MissionOrderTroopControllerVM.CloseFacingOrderSet(dataSource);
                        }
                        Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, dataSource.OrderController.SelectedFormations);
                        orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(dataSource.OrderController.SelectedFormations);
                        break;
                    }
                case MissionOrderVM.CursorState.Form:
                    break;
            }
            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(dataSource.OrderController.SelectedFormations, orderToAdd);
                return null;
            }
            return orderToAdd;
        }
    }
}
