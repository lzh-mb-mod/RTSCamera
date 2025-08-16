using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
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
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using static TaleWorlds.Engine.WorldPosition;
using static TaleWorlds.MountAndBlade.ArrangementOrder;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Utilities
{
    public static class Utility
    {
        public static Color MessageColor = new Color(0.2f, 0.9f, 0.7f);
        private static PropertyInfo MinimumFileCount = typeof(LineFormation).GetProperty("MinimumFileCount", BindingFlags.Instance | BindingFlags.NonPublic);
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
            var stringIdOn = "str_order_name_on";
            var stringIdOff = "str_order_name_off";
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
                                {
                                    variation = nameof(OrderSubType.MoveToPosition);
                                    break;
                                }
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                variation = nameof(OrderSubType.Charge);
                                break;
                            case OrderType.LookAtDirection:
                                stringId = stringIdOn;
                                variation = nameof(OrderSubType.ToggleFacing);
                                break;
                            case OrderType.LookAtEnemy:
                                stringId = stringIdOff;
                                variation = nameof(OrderSubType.ToggleFacing);
                                break;
                            case OrderType.FollowMe:
                                variation = nameof(OrderSubType.FollowMe);
                                break;
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
                                variation = nameof(OrderSubType.Advance);
                                break;
                            case OrderType.FallBack:
                                variation = nameof(OrderSubType.Fallback);
                                break;
                            case OrderType.StandYourGround:
                                variation = nameof(OrderSubType.Stop);
                                break;
                            case OrderType.Retreat:
                                variation = nameof(OrderSubType.Retreat);
                                break;
                            case OrderType.ArrangementLine:
                                variation = nameof(OrderSubType.FormLine);
                                break;
                            case OrderType.ArrangementCloseOrder:
                                variation = nameof(OrderSubType.FormClose);
                                break;
                            case OrderType.ArrangementLoose:
                                variation = nameof(OrderSubType.FormLoose);
                                break;
                            case OrderType.ArrangementCircular:
                                variation = nameof(OrderSubType.FormCircular);
                                break;
                            case OrderType.ArrangementSchiltron:
                                variation = nameof(OrderSubType.FormSchiltron);
                                break;
                            case OrderType.ArrangementVee:
                                variation = nameof(OrderSubType.FormV);
                                break;
                            case OrderType.ArrangementColumn:
                                variation = nameof(OrderSubType.FormColumn);
                                break;
                            case OrderType.ArrangementScatter:
                                variation = nameof(OrderSubType.FormScatter);
                                break;
                            case OrderType.FireAtWill:
                                stringId = stringIdOn;
                                variation = nameof(OrderSubType.ToggleFire);
                                break;
                            case OrderType.HoldFire:
                                stringId = stringIdOff;
                                variation = nameof(OrderSubType.ToggleFire);
                                break;
                            case OrderType.Mount:
                                stringId = stringIdOn;
                                variation = nameof(OrderSubType.ToggleMount);
                                break;
                            case OrderType.Dismount:
                                stringId = stringIdOff;
                                variation = nameof(OrderSubType.ToggleMount);
                                break;
                            case OrderType.AIControlOn:
                                stringId = stringIdOn;
                                variation = nameof(OrderSubType.ToggleAI);
                                break;
                            case OrderType.AIControlOff:
                                stringId = stringIdOff;
                                variation = nameof(OrderSubType.ToggleAI);
                                break;
                            default:
                                MissionSharedLibrary.Utilities.Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.FollowMainAgent:
                    variation = nameof(OrderSubType.FollowMe);
                    break;
                case CustomOrderType.SetTargetFormation:
                    var orderMessage = GameTexts.FindText("str_rts_camera_command_system_defensive_attack");
                    orderMessage.SetTextVariable("TARGET_NAME", GameTexts.FindText("str_troop_group_name", ((int)order.TargetFormation.PhysicalClass).ToString()));
                    return orderMessage;
            }
            return GameTexts.FindText(stringId, variation);
        }

        public static void ChargeToFormation(OrderController playerController, Formation targetFormation, bool keepMovementOrder)
        {
            var missionScreen = MissionSharedLibrary.Utilities.Utility.GetMissionScreen();
            //BeforeSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });
            var queueOrder = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
            if (!queueOrder)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(playerController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(playerController.SelectedFormations));
            }
            OrderInQueue order;
            if (keepMovementOrder)
            {
                order = new OrderInQueue
                {
                    CustomOrderType = CustomOrderType.SetTargetFormation,
                    SelectedFormations = playerController.SelectedFormations,
                    TargetFormation = targetFormation
                };
                order.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(playerController.SelectedFormations);
            }
            else
            {
                order = new OrderInQueue
                {
                    OrderType = OrderType.ChargeWithTarget,
                    SelectedFormations = playerController.SelectedFormations,
                    TargetFormation = targetFormation
                };
                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.ChargeWithTarget, playerController.SelectedFormations, targetFormation, null, null);
                order.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(playerController.SelectedFormations);
            }
            if (queueOrder)
            {
                CommandQueueLogic.AddOrderToQueue(order);
            }
            else
            {
                if (keepMovementOrder)
                {
                    foreach (Formation selectedFormation in playerController.SelectedFormations)
                    {
                        selectedFormation.SetTargetFormation(targetFormation);
                    }
                    // In current game version, set ChargeWithTarget has no effect except voice and gesture
                    // so movement order will not be changed here
                    playerController.SetOrderWithFormation(OrderType.ChargeWithTarget, targetFormation);
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
            }

            //AfterSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });

            //DisplayChargeToFormationMessage(playerController.SelectedFormations,
            //    targetFormation);
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
                switch (formationChange.MovementOrderType)
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
                            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder(missionScreen.SceneLayer.Input);
                            return queueCommand ? Patch_OrderController.GetFormationVirtualPosition(formation) : formation.QuerySystem.MedianPosition;
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
                            var gameEntity = missionObject.GameEntity;
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

        public static bool ShouldLockFormationDuringLookAtDirection(IEnumerable<Formation> formations)
        {
            return !IsAnyFormationHavingMovingOrderPostion(formations) && ShouldLockFormation();
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
                case OrderType.GuardMe:
                    return MovementOrder.MovementStateEnum.Charge;
                case OrderType.Retreat:
                    return MovementOrder.MovementStateEnum.Retreat;
                case OrderType.StandYourGround:
                    return MovementOrder.MovementStateEnum.StandGround;
                default:
                    return MovementOrder.MovementStateEnum.Hold;
            }
        }

        public static Type GetTypeOfArrangement(ArrangementOrderEnum orderEnum)
        {
            return orderEnum switch
            {
                ArrangementOrderEnum.Circle => typeof(CircularFormation),
                ArrangementOrderEnum.Column => typeof(ColumnFormation),
                ArrangementOrderEnum.Skein => typeof(SkeinFormation),
                ArrangementOrderEnum.Square => typeof(RectilinearSchiltronFormation),
                _ => typeof(LineFormation),
            };
        }

         public static ArrangementOrder GetArrangementOrder(ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            switch (arrangementOrder)
            {
                case ArrangementOrder.ArrangementOrderEnum.Line:
                    return ArrangementOrder.ArrangementOrderLine;
                case ArrangementOrder.ArrangementOrderEnum.ShieldWall:
                    return ArrangementOrder.ArrangementOrderShieldWall;
                case ArrangementOrder.ArrangementOrderEnum.Loose:
                    return ArrangementOrder.ArrangementOrderLoose;
                case ArrangementOrder.ArrangementOrderEnum.Circle:
                    return ArrangementOrder.ArrangementOrderCircle;
                case ArrangementOrder.ArrangementOrderEnum.Square:
                    return ArrangementOrder.ArrangementOrderSquare;
                case ArrangementOrder.ArrangementOrderEnum.Skein:
                    return ArrangementOrder.ArrangementOrderSkein;
                case ArrangementOrder.ArrangementOrderEnum.Column:
                    return ArrangementOrder.ArrangementOrderColumn;
                case ArrangementOrder.ArrangementOrderEnum.Scatter:
                    return ArrangementOrder.ArrangementOrderScatter;
            }

            return ArrangementOrder.ArrangementOrderLine;
        }

        public static OrderType ArrangementOrderEnumToOrderType(ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            return arrangementOrder switch
            {
                ArrangementOrder.ArrangementOrderEnum.Line => OrderType.ArrangementLine,
                ArrangementOrder.ArrangementOrderEnum.ShieldWall => OrderType.ArrangementCloseOrder,
                ArrangementOrder.ArrangementOrderEnum.Loose => OrderType.ArrangementLoose,
                ArrangementOrder.ArrangementOrderEnum.Circle => OrderType.ArrangementCircular,
                ArrangementOrder.ArrangementOrderEnum.Square => OrderType.ArrangementSchiltron,
                ArrangementOrder.ArrangementOrderEnum.Skein => OrderType.ArrangementVee,
                ArrangementOrder.ArrangementOrderEnum.Column => OrderType.ArrangementColumn,
                ArrangementOrder.ArrangementOrderEnum.Scatter => OrderType.ArrangementScatter,
                _ => OrderType.None
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

        public static float GetMinimumWidthOfLineFormation(Formation formation)
        {
            return (float)(GetMinimumFileCount(formation) - 1) * (formation.MinimumInterval + formation.UnitDiameter) + formation.UnitDiameter;
        }
        public static float GetMaximumWidthOfLineFormation(Formation formation)
        {
            float unitDiameter = formation.UnitDiameter;
            int countWithOverride = GetUnitCountWithOverride(formation);
            if (countWithOverride > 0)
                unitDiameter += (countWithOverride - 1) * (formation.MaximumInterval + formation.UnitDiameter);
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
            return MathF.Max(0.0f, (float)GetUnitCountWithOverride(formation) * (formation.MaximumInterval + formation.UnitDiameter)) / MathF.PI;
        }

        private static int GetMaximumRankCountOfCircularFormation(Formation formation, int unitCount, int unitSpacing)
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
        private static float GetCircumferenceAuxOfCircularFormation(
            int unitCount,
            int rankCount,
            float radialInterval,
            float distanceInterval)
        {
            float num = MathF.TwoPI * distanceInterval;
            float circumference = MathF.Max(0.0f, (float)unitCount * radialInterval);
            float circumferenceAux;
            do
            {
                circumferenceAux = circumference;
                circumference = MathF.Max(0.0f, circumferenceAux - num);
            }
            while (GetUnitCountAuxOfCircularFormation(circumference, rankCount, radialInterval, distanceInterval) > unitCount && (double)circumferenceAux > 0.0);
            return circumferenceAux;
        }

        // Copied from CircularFormation.GetUnitCountAux
        private static int GetUnitCountAuxOfCircularFormation(
          float circumference,
          int rankCount,
          float radialInterval,
          float distanceInterval)
        {
            int unitCountAux = 0;
            double num = 2.0 * Math.PI * (double)distanceInterval;
            for (int index = 1; index <= rankCount; ++index)
                unitCountAux += (int)(Math.Max(0.0, (double)circumference - (double)(rankCount - index) * num) / (double)radialInterval);
            return unitCountAux;
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
            if (CommandSystemConfig.Get().HollowSquare && ShouldEnablePlayerOrderControllerPatchForFormation(formation))
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
            int num = MathF.Round((float)f);
            if (num < sideFromRankCount && (num * num == countWithOverride || rankCount == 1))
                sideFromRankCount = num;
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

        public static int GetFileCountFromWidth(Formation formation, float flankWidth, int unitSpacing)
        {
            // TODO the formation may be a column formation. MinimumFileCount is a property of LineFormation, so it may not be available.
            return MathF.Max(MathF.Max(0, (int)(((double)flankWidth - (double)formation.UnitDiameter) / ((double)GetFormationInterval(formation, unitSpacing) + (double)formation.UnitDiameter) + 9.9999997473787516E-06)) + 1, formation.Arrangement is ColumnFormation ? 1 : (int)MinimumFileCount.GetValue(formation.Arrangement));
        }

        // Copied From LineFormation.get_FlankWidth
        public static float GetFlankWidthFromFileCount(Formation formation, int fileCount, int unitSpacing)
        {
            return MathF.Max(0, fileCount - 1) * (GetFormationInterval(formation, unitSpacing) + formation.UnitDiameter) + formation.UnitDiameter;
        }

        public static void UpdateActiveOrders()
        {
            var orderUIHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            if (orderUIHandler == null)
            {
                return;
            }
            var missionOrderVM = typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orderUIHandler) as MissionOrderVM;
            var setTroopActiveOrders = typeof(MissionOrderTroopControllerVM).GetMethod("SetTroopActiveOrders", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (OrderTroopItemVM orderTroopItemVm in missionOrderVM.TroopController.TroopList.Where((item => item.IsSelected)))
                setTroopActiveOrders.Invoke(missionOrderVM.TroopController, new object[] { orderTroopItemVm });
            var setActiveOrders = typeof(MissionOrderVM).GetMethod("SetActiveOrders", BindingFlags.Instance | BindingFlags.NonPublic);
            setActiveOrders.Invoke(missionOrderVM, new object[] { });
        }

        public static bool ShouldEnablePlayerOrderControllerPatchForFormation(IEnumerable<Formation> selectedFormations)
        {
            var team = selectedFormations.FirstOrDefault()?.Team;
            return selectedFormations.All(f => !f.IsAIControlled) && team != null && team == Mission.Current.PlayerTeam && (team.IsPlayerGeneral || team.IsPlayerSergeant && selectedFormations.All(f => f.PlayerOwner == Agent.Main));
        }


        public static bool ShouldEnablePlayerOrderControllerPatchForFormation(Formation formation)
        {
            var team = formation?.Team;
            return !formation.IsAIControlled && team != null && team == Mission.Current.PlayerTeam && (team.IsPlayerGeneral || team.IsPlayerSergeant && formation.PlayerOwner == Agent.Main);
        }

    }
}
