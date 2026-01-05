using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Orders.VisualOrders;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using static TaleWorlds.Engine.WorldPosition;
using static TaleWorlds.MountAndBlade.ArrangementOrder;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Utilities
{
    public static class Utility
    {
        public static Color MessageColor = new Color(0.2f, 0.9f, 0.7f);
        public static PropertyInfo MinimumFileCount = typeof(LineFormation).GetProperty("MinimumFileCount", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void PrintOrderHint()
        {
            //if (CommandSystemConfig.Get().ClickToSelectFormation)
            //{
            //    MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
            //        .FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName",
            //            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
            //        .ToString());
            //}

            //if (CommandSystemConfig.Get().AttackSpecificFormation)
            //{
            //    MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
            //        .FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName",
            //            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
            //        .ToString());
            //    MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
            //        .FindText("str_rts_camera_command_system_attack_specific_formation_alt_hint")
            //        .ToString());
            //}

            MissionSharedLibrary.Utilities.Utility.DisplayMessage(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
                .FindText("str_rts_camera_command_system_order_queue_usage").SetTextVariable("KeyName",
                    CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).ToSequenceString())
                .ToString());
        }

        public static void DisplayChargeToFormationMessage(MBReadOnlyList<Formation> selectedFormations,
            Formation targetFormation)
        {
            // From MissionOrderVM.OnOrder
            var formationNames = new List<TextObject>();
            foreach (var formation in selectedFormations)
            {
                formationNames.Add(GameTexts.FindText("str_formation_class_string", formation.PhysicalClass.GetName()));
            }

            if (!formationNames.IsEmpty())
            {
                var message = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                message.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNames, false));
                message.SetTextVariable("STR2",
                    GameTexts.FindText("str_formation_ai_sergeant_instruction_behavior_text",
                            nameof(BehaviorTacticalCharge))
                        .SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", targetFormation.AI.Side.ToString()))
                        // TODO: Verify PhysicalClass
                        .SetTextVariable("CLASS", GameTexts.FindText("str_troop_group_name", ((int)targetFormation.PhysicalClass).ToString())));
                //MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString());
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }

        public static void DisplayFormationReadyMessage(Formation formation)
        {
            var message = GameTexts.FindText("str_formation_ai_behavior_text", nameof(BehaviorStop));
            message.SetTextVariable("IS_PLURAL", 0);
            message.SetTextVariable("TROOP_NAMES_BEGIN", "");
            message.SetTextVariable("TROOP_NAMES_END", GameTexts.FindText("str_troop_group_name", ((int)formation.PhysicalClass).ToString()));
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString(), MessageColor);
        }
        public static void DisplayFormationChargeMessage(Formation formation)
        {
            var message = GameTexts.FindText("str_formation_ai_behavior_text", nameof(BehaviorTacticalCharge));
            message.SetTextVariable("IS_PLURAL", 0);
            message.SetTextVariable("TROOP_NAMES_BEGIN", "");
            message.SetTextVariable("TROOP_NAMES_END", GameTexts.FindText("str_troop_group_name", ((int)formation.PhysicalClass).ToString()));
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString(), MessageColor);
        }

        public static void DisplayVolleyEnabledMessage(IEnumerable<Formation> selectedFormations, bool enabled)
        {
            var formationNames = new List<TextObject>();
            foreach (var formation in selectedFormations)
            {
                formationNames.Add(GameTexts.FindText("str_formation_class_string", formation.PhysicalClass.GetName()));
            }
            if (!formationNames.IsEmpty())
            {
                var message = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                message.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNames, false));
                message.SetTextVariable("STR2",
                    enabled ? GameTexts.FindText("str_rts_camera_command_system_volley_enabled") : GameTexts.FindText("str_rts_camera_command_system_volley_disabled"));
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }

        public static void DisplayVolleyFireMessage(IEnumerable<Formation> selectedFormations)
        {
            var formationNames = new List<TextObject>();
            foreach (var formation in selectedFormations)
            {
                formationNames.Add(GameTexts.FindText("str_formation_class_string", formation.PhysicalClass.GetName()));
            }
            if (!formationNames.IsEmpty())
            {
                var message = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                message.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNames, false));
                message.SetTextVariable("STR2", GameTexts.FindText("str_rts_camera_command_system_volley_fire"));
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }


        public static bool ShouldChargeToFormation(Agent agent)
        {
            return agent.Formation != null && agent.Formation.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget &&
                   CommandSystemConfig.Get().AttackSpecificFormation &&
                       (QueryLibrary.IsCavalry(agent) ||
                        QueryLibrary.IsRangedCavalry(agent) && agent.Formation.FiringOrder.OrderType == OrderType.HoldFire ||
                        !CommandSystemSubModule.IsRealisticBattleModuleInstalled &&
                            (QueryLibrary.IsInfantry(agent) || QueryLibrary.IsRanged(agent) && agent.Formation.FiringOrder.OrderType == OrderType.HoldFire));
        }

        public static MethodInfo BeforeSetOrder = typeof(OrderController).GetMethod("BeforeSetOrder", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo AfterSetOrder = typeof(OrderController).GetMethod("AfterSetOrder", BindingFlags.NonPublic | BindingFlags.Instance);


        public static void CallAfterSetOrder(OrderController orderController, OrderType orderType)
        {
            AfterSetOrder?.Invoke(orderController, new object[] { orderType });
        }
        public static void DisplayFocusAttackMessage(IEnumerable<Formation> formations, Formation target)
        {
            List<TextObject> formationNameList = new List<TextObject>();
            foreach (var formation in formations)
                formationNameList.Add(GameTexts.FindText("str_formation_class_string", formation.PhysicalClass.GetName()));
            if (!formationNameList.IsEmpty())
            {
                TextObject textObject = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                textObject.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNameList, false));
                var orderMessage = GameTexts.FindText("str_rts_camera_command_system_defensive_attack");
                orderMessage.SetTextVariable("TARGET_NAME", GameTexts.FindText("str_troop_group_name", ((int)target.PhysicalClass).ToString()));
                textObject.SetTextVariable("STR2", orderMessage);

                MissionSharedLibrary.Utilities.Utility.DisplayMessage(textObject.ToString(), MessageColor);
            }
        }

        public static void DisplayAddOrderToQueueMessage()
        {
            MissionSharedLibrary.Utilities.Utility.DisplayLocalizedText("str_rts_camera_command_system_add_order_to_queue", null, MessageColor);
        }

        public static void DisplayAdjustFormationSpeedMessage(IEnumerable<Formation> formations)
        {
            List<TextObject> formationNameList = new List<TextObject>();
            foreach (var formation in formations)
                formationNameList.Add(GameTexts.FindText("str_troop_group_name", ((int)formation.PhysicalClass).ToString()));
            if (!formationNameList.IsEmpty())
            {
                TextObject textObject = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                textObject.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNameList, false));
                textObject.SetTextVariable("STR2", GameTexts.FindText("str_rts_camera_command_system_sync_locked_formation_speed_message"));
                MissionSharedLibrary.Utilities.Utility.DisplayMessage(textObject.ToString(), MessageColor);
            }
        }

        public static void DisplayExecuteOrderMessage(IEnumerable<Formation> selectedFormations, OrderInQueue order)
        {
            MissionSharedLibrary.Utilities.Utility.DisplayLocalizedText("str_rts_camera_command_system_execute_order_in_queue", null, MessageColor);
            List<TextObject> formationNameList = new List<TextObject>();
            foreach (var formation in selectedFormations)
                formationNameList.Add(GameTexts.FindText("str_troop_group_name", ((int)formation.PhysicalClass).ToString()));
            if (!formationNameList.IsEmpty())
            {
                TextObject textObject = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                textObject.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNameList, false));
                textObject.SetTextVariable("STR2", GetOrderString(order));
                MissionSharedLibrary.Utilities.Utility.DisplayMessage(textObject.ToString(), MessageColor);
            }
        }

        private static TextObject GetOrderString(OrderInQueue order)
        {
            var stringId = "str_order_name";
            string variation = null;
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.MoveToLineSegment:
                            case OrderType.MoveToLineSegmentWithHorizontalLayout:
                            case OrderType.Move:
                                return RTSCommandMoveVisualOrder.GetName();
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                return RTSCommandChargeVisualOrder.GetName();
                            case OrderType.LookAtDirection:
                                return RTSCommandToggleFacingVisualOrder.GetName(order.OrderType);
                            case OrderType.LookAtEnemy:
                                return RTSCommandToggleFacingVisualOrder.GetName(order.OrderType);
                            case OrderType.FollowMe:
                                return RTSCommandFollowMeVisualOrder.GetName();
                            case OrderType.FollowEntity:
                                stringId = "str_rts_camera_command_system_follow_entity";
                                break;
                            case OrderType.AttackEntity:
                                stringId = "str_rts_camera_command_system_attack_entity";
                                break;
                            case OrderType.PointDefence:
                                stringId = "str_rts_camera_command_system_point_defense";
                                break;
                            case OrderType.Advance:
                                return RTSCommandAdvanceVisualOrder.GetName();
                            case OrderType.FallBack:
                                return RTSCommandFallbackVisualOrder.GetName();
                            case OrderType.StandYourGround:
                                return RTSCommandStopVisualOrder.GetName();
                            case OrderType.Retreat:
                                return RTSCommandRetreatVisualOrder.GetName();
                            case OrderType.ArrangementLine:
                            case OrderType.ArrangementCloseOrder:
                            case OrderType.ArrangementLoose:
                            case OrderType.ArrangementCircular:
                            case OrderType.ArrangementSchiltron:
                            case OrderType.ArrangementVee:
                            case OrderType.ArrangementColumn:
                            case OrderType.ArrangementScatter:
                                return RTSCommandArrangementVisualOrder.GetName(OrderTypeToArrangementOrderEnum(order.OrderType));
                            case OrderType.FireAtWill:
                            case OrderType.HoldFire:
                            case OrderType.Mount:
                            case OrderType.Dismount:
                            case OrderType.AIControlOn:
                            case OrderType.AIControlOff:
                                return RTSCommandGenericToggleVisualOrder.GetName(order.OrderType);
                            default:
                                MissionSharedLibrary.Utilities.Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.FollowMainAgent:
                    return RTSCommandFollowMeVisualOrder.GetName();
                case CustomOrderType.SetTargetFormation:
                    var orderMessage = GameTexts.FindText("str_rts_camera_command_system_defensive_attack");
                    orderMessage.SetTextVariable("TARGET_NAME", GameTexts.FindText("str_troop_group_name", ((int)order.TargetFormation.PhysicalClass).ToString()));
                    return orderMessage;
                case CustomOrderType.EnableVolley:
                    return GameTexts.FindText("str_rts_camera_command_system_volley_enabled");
                case CustomOrderType.DisableVolley:
                    return GameTexts.FindText("str_rts_camera_command_system_volley_disabled");
                case CustomOrderType.VolleyFire:
                    return GameTexts.FindText("str_rts_camera_command_system_volley_fire");
            }
            return GameTexts.FindText(stringId, variation);
        }

        public static void FocusOnFormation(OrderController playerController, Formation targetFormation)
        {
            var missionScreen = MissionSharedLibrary.Utilities.Utility.GetMissionScreen();
            //BeforeSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });
            var queueOrder = Utilities.Utility.ShouldQueueCommand();
            if (!queueOrder)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(playerController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(playerController.SelectedFormations));
            }
            OrderInQueue order = new OrderInQueue
            {
                CustomOrderType = CustomOrderType.SetTargetFormation,
                SelectedFormations = playerController.SelectedFormations,
                TargetFormation = targetFormation
            };
            order.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(playerController.SelectedFormations);
            if (queueOrder)
            {
                CommandQueueLogic.AddOrderToQueue(order);
            }
            else
            {
                foreach (Formation selectedFormation in playerController.SelectedFormations)
                {
                    selectedFormation.SetTargetFormation(targetFormation);
                }
                // In current game version, set ChargeWithTarget has no effect except voice and gesture
                // so movement order will not be changed here
                //playerController.SetOrderWithFormation(OrderType.ChargeWithTarget, targetFormation);
                Mission.Current?.GetMissionBehavior<CommandSystemLogic>()?.OnMovementOrderChanged(playerController.SelectedFormations);
                DisplayFocusAttackMessage(playerController.SelectedFormations, order.TargetFormation);

                Utilities.Utility.CallAfterSetOrder(playerController, OrderType.ChargeWithTarget);
                CommandQueueLogic.OnCustomOrderIssued(order, playerController);
            }
            // Call OnOrderExecuted because OrderController will not do it for OrderType.ChargeWithTarget
            // This is required to keep MissionOrderVM open in rts mode and close it in player mode.
            var missionOrderVM = MissionSharedLibrary.Utilities.Utility.GetMissionOrderVM(Mission.Current);
            var orderItem = MissionSharedLibrary.Utilities.Utility.FindOrderWithId(missionOrderVM, "order_movement_charge");
            if (orderItem != null)
            {
                missionOrderVM.OnOrderExecuted(orderItem);
            }
        }

        public static void ChargeToFormation(OrderController playerController, Formation targetFormation)
        {
            //BeforeSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });
            var queueOrder = Utilities.Utility.ShouldQueueCommand();
            if (!queueOrder)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(playerController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(playerController.SelectedFormations));
            }
            OrderInQueue order = new OrderInQueue
            {
                OrderType = OrderType.ChargeWithTarget,
                SelectedFormations = playerController.SelectedFormations,
                TargetFormation = targetFormation
            };
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.ChargeWithTarget, playerController.SelectedFormations, targetFormation, null, null);
            order.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(playerController.SelectedFormations);
            if (queueOrder)
            {
                CommandQueueLogic.AddOrderToQueue(order);
            }
            else
            {
                foreach (Formation selectedFormation in playerController.SelectedFormations)
                {
                    selectedFormation.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(targetFormation));
                    selectedFormation.SetTargetFormation(targetFormation);
                }
                CommandQueueLogic.TryPendingOrder(playerController.SelectedFormations, order);
                // In current game version, set ChargeWithTarget has no effect except voice and gesture
                // so movement order will not be changed here
                playerController.SetOrderWithFormation(OrderType.ChargeWithTarget, targetFormation);
            }

            // Call OnOrderExecuted because OrderController will not do it for OrderType.ChargeWithTarget
            // This is required to keep MissionOrderVM open in rts mode and close it in player mode.
            var missionOrderVM = MissionSharedLibrary.Utilities.Utility.GetMissionOrderVM(Mission.Current);
            var orderItem = MissionSharedLibrary.Utilities.Utility.FindOrderWithId(missionOrderVM, "order_movement_charge");
            if (orderItem != null)
            {
                missionOrderVM.OnOrderExecuted(orderItem);
            }
        }

        public static bool ShouldLockFormation()
        {
            var config = CommandSystemConfig.Get();
            var missionScreen = MissionSharedLibrary.Utilities.Utility.GetMissionScreen();
            if (config == null || missionScreen == null)
            {
                return false;
            }
            switch (config.FormationLockCondition)
            {
                case FormationLockCondition.Never:
                    return false;
                case FormationLockCondition.WhenPressed:
                    return CommandSystemGameKeyCategory.GetKey(GameKeyEnum.FormationLockMovement).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
                case FormationLockCondition.WhenNotPressed:
                    return !CommandSystemGameKeyCategory.GetKey(GameKeyEnum.FormationLockMovement).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
            }
            return false;
        }

        public static bool IsFormationOrderPositionMoving(Formation formation)
        {
            if (Patch_OrderController.LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
            {
                return IsMovementOrderMoving(formationChange.MovementOrderType);
            }
            return false;
        }

        public static bool IsMovementOrderMoving(OrderType? movementOrderType)
        {
            switch (movementOrderType)
            {
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                case OrderType.Advance:
                case OrderType.FollowEntity:
                case OrderType.AttackEntity:
                case OrderType.FollowMe:
                case OrderType.FallBack:
                    return true;
            }
            return false;
        }

        public static WorldPosition? GetFormationMovingOrderPosition(Formation formation)
        {
            if (Patch_OrderController.LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
            {
                switch (formationChange.MovementOrderType)
                {
                    case OrderType.Charge:
                    case OrderType.ChargeWithTarget:
                        {
                            var missionScreen = MissionSharedLibrary.Utilities.Utility.GetMissionScreen();
                            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
                            return queueCommand ? Patch_OrderController.GetFormationVirtualPosition(formation) : formation.CachedMedianPosition;
                        }
                    case OrderType.Advance:
                        {
                            return Patch_OrderController.GetAdvanceOrderPosition(formation, WorldPositionEnforcedCache.None, formationChange.TargetFormation);
                        }
                    case OrderType.FollowEntity:
                        {
                            var waitEntity = (formationChange.TargetEntity as UsableMachine).WaitEntity;
                            return Patch_OrderController.GetFollowEntityOrderPosition(formation, waitEntity);
                        }
                    case OrderType.AttackEntity:
                        {
                            var missionObject = formationChange.TargetEntity as MissionObject;
                            var gameEntity = GameEntity.CreateFromWeakEntity(missionObject.GameEntity);
                            return Patch_OrderController.GetAttackEntityWaitPosition(formation, gameEntity);
                        }
                    case OrderType.FollowMe:
                        {
                            return Patch_OrderController.GetFollowOrderPosition(formation, formationChange.TargetAgent);
                        }
                    case OrderType.FallBack:
                        {
                            return Patch_OrderController.GetFallbackOrderPosition(formation, WorldPositionEnforcedCache.None, formationChange.TargetFormation);
                        }
                }
            }
            return null;
        }

        public static Vec2 GetFormationMovingDirection(Formation formation)
        {
            if (Patch_OrderController.LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
            {
                switch (formationChange.MovementOrderType)
                {
                    case OrderType.FollowEntity:
                        {
                            var waitEntity = (formationChange.TargetEntity as UsableMachine).WaitEntity;
                            return Patch_OrderController.GetFollowEntityDirection(formation, waitEntity);
                        }
                    case OrderType.Advance:
                    case OrderType.FallBack:
                    case OrderType.AttackEntity:
                    case OrderType.FollowMe:
                        {
                            return Vec2.Invalid;
                        }
                }
            }
            return Vec2.Invalid;
        }

        public static bool IsAnyFormationHavingMovingOrderPostion(IEnumerable<Formation> formations)
        {
            if (Patch_OrderController.LivePreviewFormationChanges.CollectChanges(formations).Any(pair => IsFormationOrderPositionMoving(pair.Key)))
            {
                return true;
            }
            return false;
        }

        //public static bool ShouldLockFormationDuringLookAtDirection(IEnumerable<Formation> formations)
        //{
        //    return !IsAnyFormationHavingMovingOrderPostion(formations) && ShouldLockFormation();
        //}

        public static bool ShouldLockFormationDuringLookAtDirection(Formation formation)
        {
            return !IsFormationOrderPositionMoving(formation) && Patch_OrderController.GetFormationVirtualFacingOrder(formation) == OrderType.LookAtDirection && ShouldLockFormation();
        }

        public static bool ShouldKeepFormationWidth()
        {
            var missionScreen = MissionSharedLibrary.Utilities.Utility.GetMissionScreen();
            if (missionScreen == null)
                return false;
            return CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepFormationWidth).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
        }

        public static MovementOrder.MovementStateEnum MovementStateFromMovementOrderType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                    return MovementOrder.MovementStateEnum.Charge;
                case OrderType.Retreat:
                    return MovementOrder.MovementStateEnum.Retreat;
                case OrderType.StandYourGround:
                    return MovementOrder.MovementStateEnum.StandGround;
                default:
                    return MovementOrder.MovementStateEnum.Hold;
            }
        }

        public static Type GetTypeOfArrangement(ArrangementOrderEnum orderEnum, bool hollowSquareAllowed = false)
        {
            return orderEnum switch
            {
                ArrangementOrderEnum.Circle => typeof(CircularFormation),
                ArrangementOrderEnum.Column => typeof(ColumnFormation),
                ArrangementOrderEnum.Skein => typeof(SkeinFormation),
                ArrangementOrderEnum.Square => CommandSystemConfig.Get().HollowSquare && hollowSquareAllowed ? typeof(SquareFormation) : typeof(RectilinearSchiltronFormation),
                _ => typeof(LineFormation),
            };
        }

        public static ArrangementOrderEnum GetOrderEnumOfArrangement(IFormationArrangement arrangement)
        {
            var type = arrangement.GetType();
            if (type == typeof(LineFormation))
                return ArrangementOrderEnum.Line;
            if (type == typeof(ColumnFormation))
                return ArrangementOrderEnum.Column;
            if (type == typeof(SkeinFormation))
                return ArrangementOrderEnum.Skein;
            if (type == typeof(CircularFormation) || type == typeof(CircularSchiltronFormation))
                return ArrangementOrderEnum.Circle;
            if (type == typeof(SquareFormation) || type == typeof(RectilinearSchiltronFormation))
                return ArrangementOrderEnum.Square;
            return ArrangementOrderEnum.Line;
        }

        public static ArrangementOrder GetArrangementOrder(ArrangementOrderEnum arrangementOrder)
        {
            switch (arrangementOrder)
            {
                case ArrangementOrderEnum.Line:
                    return ArrangementOrder.ArrangementOrderLine;
                case ArrangementOrderEnum.ShieldWall:
                    return ArrangementOrder.ArrangementOrderShieldWall;
                case ArrangementOrderEnum.Loose:
                    return ArrangementOrder.ArrangementOrderLoose;
                case ArrangementOrderEnum.Circle:
                    return ArrangementOrder.ArrangementOrderCircle;
                case ArrangementOrderEnum.Square:
                    return ArrangementOrder.ArrangementOrderSquare;
                case ArrangementOrderEnum.Skein:
                    return ArrangementOrder.ArrangementOrderSkein;
                case ArrangementOrderEnum.Column:
                    return ArrangementOrder.ArrangementOrderColumn;
                case ArrangementOrderEnum.Scatter:
                    return ArrangementOrder.ArrangementOrderScatter;
            }

            return ArrangementOrder.ArrangementOrderLine;
        }

        public static OrderType ArrangementOrderEnumToOrderType(ArrangementOrderEnum arrangementOrder)
        {
            return arrangementOrder switch
            {
                ArrangementOrderEnum.Line => OrderType.ArrangementLine,
                ArrangementOrderEnum.ShieldWall => OrderType.ArrangementCloseOrder,
                ArrangementOrderEnum.Loose => OrderType.ArrangementLoose,
                ArrangementOrderEnum.Circle => OrderType.ArrangementCircular,
                ArrangementOrderEnum.Square => OrderType.ArrangementSchiltron,
                ArrangementOrderEnum.Skein => OrderType.ArrangementVee,
                ArrangementOrderEnum.Column => OrderType.ArrangementColumn,
                ArrangementOrderEnum.Scatter => OrderType.ArrangementScatter,
                _ => OrderType.None
            };
        }

        public static ArrangementOrderEnum OrderTypeToArrangementOrderEnum(OrderType orderType)
        {
            return orderType switch
            {
                OrderType.ArrangementLine => ArrangementOrderEnum.Line,
                OrderType.ArrangementCloseOrder => ArrangementOrderEnum.ShieldWall,
                OrderType.ArrangementLoose => ArrangementOrderEnum.Loose,
                OrderType.ArrangementCircular => ArrangementOrderEnum.Circle,
                OrderType.ArrangementSchiltron => ArrangementOrderEnum.Square,
                OrderType.ArrangementVee => ArrangementOrderEnum.Skein,
                OrderType.ArrangementColumn => ArrangementOrderEnum.Column,
                OrderType.ArrangementScatter => ArrangementOrderEnum.Scatter,
                _ => ArrangementOrderEnum.Line
            };
        }

        public static int GetUnitCountWithOverride(Formation formation)
        {
            return !formation.OverridenUnitCount.HasValue ? formation.Arrangement.UnitCount : formation.OverridenUnitCount.Value;
        }

        public static int GetMinimumFileCount(Formation formation)
        {
            return MathF.Max(1, (int)MathF.Sqrt(GetUnitCountWithOverride(formation)));
        }

        public static float GetFormationInterval(Formation formation, int unitSpacing)
        {
            return formation.CalculateHasSignificantNumberOfMounted && !(formation.RidingOrder == RidingOrder.RidingOrderDismount) ? Formation.CavalryInterval(unitSpacing) : Formation.InfantryInterval(unitSpacing);
        }

        public static float GetFormationDistance(Formation formation, int unitSpacing)
        {
            return formation.CalculateHasSignificantNumberOfMounted && !(formation.RidingOrder == RidingOrder.RidingOrderDismount) ? Formation.CavalryDistance(unitSpacing) : Formation.InfantryDistance(unitSpacing);
        }

        public static float GetFormationMaximumWidthOfArrangementOrder(Formation formation, ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            var unitSpacing = ArrangementOrder.GetUnitSpacingOf(arrangementOrder);
            switch (arrangementOrder)
            {
                case ArrangementOrder.ArrangementOrderEnum.Square:
                    return Utilities.Utility.GetMaximumWidthOfSquareFormation(formation);
                case ArrangementOrder.ArrangementOrderEnum.Circle:
                    return Utilities.Utility.GetMaximumWidthOfCircularFormation(formation, unitSpacing);
                case ArrangementOrder.ArrangementOrderEnum.Column:
                    return Utilities.Utility.GetMaximumWidthOfColumnFormation(formation, unitSpacing);
                default:
                    return Utilities.Utility.GetMaximumWidthOfLineFormation(formation, unitSpacing);
            }
        }

        public static float GetFormationMinimumWidthOfArrangementOrder(Formation formation, ArrangementOrder.ArrangementOrderEnum arrangementOrder, int unitSpacing)
        {
            switch (arrangementOrder)
            {
                case ArrangementOrder.ArrangementOrderEnum.Square:
                    return Utilities.Utility.GetMinimumWidthOfSquareFormation(formation);
                case ArrangementOrder.ArrangementOrderEnum.Circle:
                    return Utilities.Utility.GetMinimumWidthOfCircularFormation(formation, unitSpacing);
                case ArrangementOrder.ArrangementOrderEnum.Column:
                    return Utilities.Utility.GetMinimumWidthOfColumnFormation(formation, unitSpacing);
                default:
                    return Utilities.Utility.GetMinimumWidthOfLineFormation(formation);
            }
        }

        public static float GetMinimumWidthOfLineFormation(Formation formation)
        {
            return (float)(GetMinimumFileCount(formation) - 1) * (formation.MinimumInterval + formation.UnitDiameter) + formation.UnitDiameter;
        }
        public static float GetMaximumWidthOfLineFormation(Formation formation, int unitSpacing)
        {
            float unitDiameter = formation.UnitDiameter;
            int countWithOverride = GetUnitCountWithOverride(formation);
            if (countWithOverride > 0)
                unitDiameter += (countWithOverride - 1) * (GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter);
            return unitDiameter;
        }

        public static float GetMinimumWidthOfCircularFormation(Formation formation, int unitSpacing)
        {
            int countWithOverride = GetUnitCountWithOverride(formation);
            int maximumRankCount = GetMaximumRankCountOfCircularFormation(formation, countWithOverride, unitSpacing);
            float radialInterval = formation.MinimumInterval + formation.UnitDiameter;
            float distanceInterval = formation.MinimumDistance + formation.UnitDiameter;
            return GetCircumferenceAuxOfCircularFormation(countWithOverride, maximumRankCount, radialInterval, distanceInterval) / MathF.PI;
        }

        public static float GetMaximumWidthOfCircularFormation(Formation formation, int unitSpacing)
        {
            return MathF.Max(0.0f, (float)GetUnitCountWithOverride(formation) * (GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter)) / MathF.PI;
        }

        public static int GetMaximumRankCountOfCircularFormation(Formation formation, int unitCount, int unitSpacing)
        {
            int rankCount = 0;
            int placedUnitCount = 0;
            float interval = GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter;
            float distance = GetFormationDistance(formation, unitSpacing) + formation.UnitDiameter;
            while (placedUnitCount < unitCount)
            {
                int unitCountInCurrentRank = (int)(MathF.TwoPI * (double)((float)rankCount * distance) / (double)interval);
                placedUnitCount += MathF.Max(1, unitCountInCurrentRank);
                ++rankCount;
            }
            return MathF.Max(rankCount, 1);
        }

        // Copied from CircularFormation.GetCircumferenceAux
        public static float GetCircumferenceAuxOfCircularFormation(
            int unitCount,
            int rankCount,
            float radialInterval,
            float distanceInterval)
        {
            float circuferenceDiffBetweenRank = (float)(TaleWorlds.Library.MathF.PI * 2.0 * (double)distanceInterval);
            float initialCircumference = TaleWorlds.Library.MathF.Max(0f, (float)unitCount * radialInterval);
            float OutmostCircumference;
            int unitCountAux;
            int rankToReduce = 0;
            do
            {
                OutmostCircumference = initialCircumference - rankToReduce * circuferenceDiffBetweenRank;
                OutmostCircumference -= OutmostCircumference % radialInterval;
                var nextCircumference = TaleWorlds.Library.MathF.Max(0f, initialCircumference - (rankToReduce + 1) * circuferenceDiffBetweenRank);
                unitCountAux = GetUnitCountAuxOfCircularFormation(nextCircumference, rankCount, radialInterval, distanceInterval);
                ++rankToReduce;
            }
            while (unitCountAux >= unitCount && OutmostCircumference > 0f);
            if (CommandSystemConfig.Get().CircleFormationUnitSpacingPreference == CircleFormationUnitSpacingPreference.Loose)
            {
                return OutmostCircumference;
            }
            else
            {
                int unitCountToReduceInOutmostRank = 0;
                initialCircumference = OutmostCircumference;
                do
                {
                    OutmostCircumference = initialCircumference - unitCountToReduceInOutmostRank * radialInterval;
                    var nextCircumference = TaleWorlds.Library.MathF.Max(0f, initialCircumference - (unitCountToReduceInOutmostRank + 1) * radialInterval);
                    unitCountAux = GetUnitCountAuxOfCircularFormation(nextCircumference, rankCount, radialInterval, distanceInterval);
                    ++unitCountToReduceInOutmostRank;
                }
                while (unitCountAux >= unitCount && OutmostCircumference > 0f);
                return OutmostCircumference;
            }
        }

        // Copied from CircularFormation.GetUnitCountAux
        private static int GetUnitCountAuxOfCircularFormation(
          float circumference,
          int rankCount,
          float radialInterval,
          float distanceInterval)
        {
            int num = 0;
            double circuferenceDiffBetweenRank = TaleWorlds.Library.MathF.PI * 2.0 * (double)distanceInterval;
            for (int i = 1; i <= rankCount; i++)
            {
                var numInCurrentRank = (int)(TaleWorlds.Library.MathF.Max(0.0, (double)circumference - (double)(rankCount - i) * circuferenceDiffBetweenRank) / (double)radialInterval);
                num += numInCurrentRank;
            }
            // original code
            //return num;
            return TaleWorlds.Library.MathF.Max(num, 1);
        }

        private static float GetDiameterOfCircularFormation(Formation formation, float circumference, int unitSpacing)
        {
            var countWithOverride = GetUnitCountWithOverride(formation);
            var maximumRankCount = GetMaximumRankCountOfCircularFormation(formation, countWithOverride, unitSpacing);
            float radialInterval = GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter;
            float distanceInterval = GetFormationDistance(formation, unitSpacing) + formation.UnitDiameter;
            float circumferenceAux = GetCircumferenceAuxOfCircularFormation(countWithOverride, maximumRankCount, radialInterval, distanceInterval);
            float maxValue = MathF.Max(0.0f, (float)countWithOverride * radialInterval);
            circumference = MBMath.ClampFloat(circumference, circumferenceAux, maxValue);
            return Math.Max(circumference - GetFormationInterval(formation, unitSpacing), formation.UnitDiameter) / MathF.PI;
        }

        // copied from SquareFormation.get_MinimumWidth
        public static float GetMinimumWidthOfSquareFormation(Formation formation)
        {
            return GetSideWidthFromUnitCountOfSquareFormation(GetUnitsPerSideFromRankCountOfSquareFormation(formation, GetMaximumRankCountOfSquareFormation(GetUnitCountWithOverride(formation), out int _)), formation.MinimumInterval, formation.UnitDiameter);
        }
        public static float GetMaximumWidthOfSquareFormation(Formation formation)
        {
            if (CommandSystemConfig.Get().HollowSquare && ShouldEnableHollowSquareFormationFor(formation))
            {
                return GetSideWidthFromUnitCountOfSquareFormation(GetUnitsPerSideFromRankCountOfSquareFormation(formation, 1), GetFormationInterval(formation, GetUnitSpacingOf(ArrangementOrderEnum.Square)), formation.UnitDiameter);
            }
            return GetSideWidthFromUnitCountOfSquareFormation(GetUnitsPerSideFromRankCountOfSquareFormation(formation, GetMaximumRankCountOfSquareFormation(GetUnitCountWithOverride(formation), out int _)), formation.MaximumInterval, formation.UnitDiameter);
        }

        private static int GetUnitsPerSideFromRankCountOfSquareFormation(Formation formation, int rankCount)
        {
            int countWithOverride = GetUnitCountWithOverride(formation);
            rankCount = MathF.Min(GetMaximumRankCountOfSquareFormation(countWithOverride, out int _), rankCount);
            double f = (double)countWithOverride / (4.0 * (double)rankCount) + (double)rankCount;
            int sideFromRankCount = MathF.Ceiling((float)f);
            // replaces Mathf.Round(f)
            // for example, if untiCount = 42, and rankCount = 1,
            // f = 11.5
            // Mathf.Round(f) would be 12.
            var floor = MathF.Floor(f);
            //int num = f - floor > 0.5f ? floor + 1 : floor;
            //int num = MathF.Round(f);
            if (floor < sideFromRankCount && (floor * floor == countWithOverride || rankCount == 1 && countWithOverride > 10))
                sideFromRankCount = floor;
            if (sideFromRankCount == 0)
                sideFromRankCount = 1;
            return sideFromRankCount;
        }


        private static int GetMaximumRankCountOfSquareFormation(int unitCount, out int minimumFlankCount)
        {
            int num = (int)MathF.Sqrt((float)unitCount);
            if (num * num != unitCount)
                ++num;
            minimumFlankCount = num;
            return MathF.Max(1, (num + 1) / 2);
        }

        private static float GetSideWidthFromUnitCountOfSquareFormation(
            int sideUnitCount,
            float interval,
            float unitDiameter)
        {
            return sideUnitCount > 0 ? (float)(sideUnitCount - 1) * (interval + unitDiameter) + unitDiameter : 0.0f;
        }

        public static float ConvertFromWidthToFlankWidthOfSquareFormation(
            Formation formation,
            int unitSpacing,
            float width)
        {
            // given that:
            // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
            // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
            // we have:
            // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
            // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
            // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
            // flankwidth = 4 * (width - unitdiameter) - interval
            return (width - formation.UnitDiameter) * 4f + GetFormationInterval(formation, unitSpacing);
        }

        public static float ConvertFromFlankWidthToWidthOfSquareFormation(
            Formation formation,
            int unitSpacing,
            float flankWidth)
        {
            // given that:
            // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
            // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
            // we have:
            // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
            // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
            // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
            // flankwidth = 4 * (width - unitdiameter) - interval
            return (flankWidth + GetFormationInterval(formation, unitSpacing)) / 4f + formation.UnitDiameter;
        }

        public static float ConvertFromWidthToFlankWidthOfCircularFormation(
            Formation formation,
            int unitSpacing,
            float width)
        {
            // For circle formation, Arrangement.FlankWidth = Circumference - interval
            // Circumference = FormOrder.FlankWidth * PI
            // Width = FormOrder.FlankWidth
            return width * MathF.PI - GetFormationInterval(formation, unitSpacing);
        }

        public static float ConvertFromFlankWidthToWidthOfCircularFormation(
            Formation formation,
            int unitSpacing,
            float flankWidth)
        {
            return  (flankWidth + GetFormationInterval(formation, unitSpacing)) / MathF.PI;
        }

        public static int GetFileCountFromWidth(Formation formation, float flankWidth, int unitSpacing)
        {
            // TODO the formation may be a column formation. MinimumFileCount is a property of LineFormation, so it may not be available.
            return MathF.Max(GetUnlimitedFileCountFromWidth(formation, flankWidth, unitSpacing), formation.Arrangement is ColumnFormation ? 1 : (int)MinimumFileCount.GetValue(formation.Arrangement));
        }

        public static int GetUnlimitedFileCountFromWidth(Formation formation, float flankWidth, int unitSpacing)
        {
            return MathF.Max(0, (int)(((double)flankWidth - (double)formation.UnitDiameter) / ((double)GetFormationInterval(formation, unitSpacing) + (double)formation.UnitDiameter) + 9.9999997473787516E-06)) + 1;
        }

        // Copied From LineFormation.get_FlankWidth
        public static float GetFlankWidthFromFileCount(Formation formation, int fileCount, int unitSpacing)
        {
            return MathF.Max(0, fileCount - 1) * (GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter) + formation.UnitDiameter;
        }

        public static float GetMinimumWidthOfColumnFormation(Formation formation, int unitSpacing)
        {
            return (MathF.Max(1, MathF.Ceiling(MathF.Sqrt((formation.Arrangement.UnitCount / ColumnFormation.ArrangementAspectRatio)))) - 1) * (formation.UnitDiameter + GetFormationInterval(formation, unitSpacing)) + formation.UnitDiameter;
        }

        public static float GetMaximumWidthOfColumnFormation(Formation formation, int unitSpacing)
        {
            return (float)(formation.Arrangement.UnitCount - 1) * (formation.UnitDiameter + GetFormationInterval(formation, unitSpacing)) + formation.UnitDiameter;
        }

        public static void UpdateActiveOrders()
        {
            var orderUIHandler = Mission.Current.GetMissionBehavior<GauntletOrderUIHandler>();
            if (orderUIHandler == null)
            {
                return;
            }
            var missionOrderVM = typeof(GauntletOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orderUIHandler) as MissionOrderVM;
            //foreach (OrderTroopItemVM orderTroopItemVm in missionOrderVM.TroopController.TroopList.Where((item => item.IsSelected)))
            //    missionOrderVM.TroopController.SetTroopActiveOrders(orderTroopItemVm);
            missionOrderVM.SetActiveOrders();
        }

        public static bool ShouldEnablePlayerOrderControllerPatchForFormation(IEnumerable<Formation> selectedFormations)
        {
            var team = selectedFormations.FirstOrDefault()?.Team;
            return selectedFormations.All(f => !f.IsAIControlled) && team != null && team == Mission.Current.PlayerTeam && (team.IsPlayerGeneral || team.IsPlayerSergeant && selectedFormations.All(f => f.PlayerOwner == Agent.Main));
        }

        public static bool ShouldEnablePlayerOrderControllerPatchForFormation(Formation formation)
        {
            var team = formation.Team;
            return !formation.IsAIControlled && team != null && team == Mission.Current.PlayerTeam && (team.IsPlayerGeneral || team.IsPlayerSergeant && formation.PlayerOwner == Agent.Main);
        }


        public static bool ShouldEnableHollowSquareFormationFor(Formation formation)
        {
            var team = formation?.Team;
            return !formation.IsAIControlled && team != null && team == Mission.Current.PlayerTeam && (team.IsPlayerGeneral || team.IsPlayerSergeant && formation.PlayerOwner == Agent.Main);
        }

        public static Vec3 GetColumnFormationCurrentPosition(Formation formation)
        {
            if (formation.Arrangement is ColumnFormation columnFormation && (columnFormation.GetUnit(columnFormation.VanguardFileIndex, 0) ?? columnFormation.Vanguard) is Agent { Position: var position })
            {
                return position;
            }
            return Vec3.Invalid;
        }
        public static bool DoesFormationHasOrderType(Formation formation, OrderType type)
        {
            MovementOrder readonlyMovementOrderReference = formation.GetReadonlyMovementOrderReference();
            switch (type)
            {
                case OrderType.FireAtWill:
                case OrderType.HoldFire:
                    return OrderController.GetActiveFiringOrderOf(formation) == type;
                case OrderType.Mount:
                case OrderType.Dismount:
                    return OrderController.GetActiveRidingOrderOf(formation) == type;
                case OrderType.AIControlOn:
                case OrderType.AIControlOff:
                    return OrderController.GetActiveAIControlOrderOf(formation) == type;
                case OrderType.LookAtDirection:
                case OrderType.LookAtEnemy:
                    return OrderController.GetActiveFacingOrderOf(formation) == type;
                case OrderType.ArrangementLine:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementScatter:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementColumn:
                    return OrderController.GetActiveArrangementOrderOf(formation) == type;
                default:
                    if (readonlyMovementOrderReference.OrderType != type && formation.ArrangementOrder.OrderType != type && formation.FacingOrder.OrderType != type && formation.FiringOrder.OrderType != type && formation.FormOrder.OrderType != type)
                    {
                        return formation.RidingOrder.OrderType == type;
                    }

                    return true;
            }
        }

        public static bool DoesFormationHasVolleyOrder(Formation formation)
        {
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.VolleyEnabledOrder != null)
                    {
                        return formationChange.VolleyEnabledOrder.Value;
                    }
                }
            }
            return CommandQueueLogic.IsFormationVolleyEnabled(formation);
        }

        public static bool ShouldQueueCommand()
        {
            // Disabled for naval battle for now.
            if (Mission.Current?.IsNavalBattle ?? false)
                return false;
            return CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
        }

        // Copied from MissionOrderTroopControllerVM.GetMaxAndCurrentAmmoOfAgent
        public static void GetMaxAndCurrentAmmoOfAgent(Agent agent, out int currentAmmo, out int maxAmmo)
        {
            currentAmmo = 0;
            maxAmmo = 0;
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; ++equipmentIndex)
            {
                MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
                if (!missionWeapon.IsEmpty)
                {
                    missionWeapon = agent.Equipment[equipmentIndex];
                    if (missionWeapon.CurrentUsageItem.IsRangedWeapon)
                    {
                        currentAmmo = agent.Equipment.GetAmmoAmount(equipmentIndex);
                        maxAmmo = agent.Equipment.GetMaxAmmo(equipmentIndex);
                    }
                }
            }
        }
    }
}
