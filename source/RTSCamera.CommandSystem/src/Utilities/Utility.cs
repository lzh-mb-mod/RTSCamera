﻿using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Utilities
{
    public static class Utility
    {
        public static Color MessageColor = new Color(0.2f, 0.9f, 0.7f);
        public static void PrintOrderHint()
        {
            //if (CommandSystemConfig.Get().ClickToSelectFormation)
            //{
            //    MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
            //        .FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName",
            //            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
            //        .ToString());
            //}

            if (CommandSystemConfig.Get().AttackSpecificFormation)
            {
                MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
                    .FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
                    .ToString());
                MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager
                    .FindText("str_rts_camera_command_system_attack_specific_formation_alt_hint")
                    .ToString());
            }
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

        public static void DisplayFocusAttackMessage(Formation formation, Formation target)
        {
            var message = GameTexts.FindText("str_rts_camera_command_system_defensive_attack");
            message.SetTextVariable("TROOP_NAME", GameTexts.FindText("str_troop_group_name", ((int)formation.PhysicalClass).ToString()));
            message.SetTextVariable("TARGET_NAME", GameTexts.FindText("str_troop_group_name", ((int)target.PhysicalClass).ToString()));
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString(), MessageColor);
        }

        public static void ChargeToFormation(OrderController playerController, Formation targetFormation, bool keepMovementOrder)
        {
            //BeforeSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });
            if (keepMovementOrder)
            {
                foreach (Formation selectedFormation in playerController.SelectedFormations)
                {
                    selectedFormation.SetTargetFormation(targetFormation);
                    DisplayFocusAttackMessage(selectedFormation, targetFormation);
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
                    DisplayFormationChargeMessage(selectedFormation);
                }
                // In current game version, set ChargeWithTarget has no effect except voice and gesture
                // so movement order will not be changed here
                playerController.SetOrderWithFormation(OrderType.ChargeWithTarget, targetFormation);

            }

            //AfterSetOrder?.Invoke(playerController, new object[] { OrderType.ChargeWithTarget });

            //DisplayChargeToFormationMessage(playerController.SelectedFormations,
            //    targetFormation);
        }
    }
}
