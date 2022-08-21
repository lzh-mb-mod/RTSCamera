using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Utilities
{
    public static class Utility
    {
        public static void PrintOrderHint()
        {
            if (CommandSystemConfig.Get().ClickToSelectFormation)
            {
                MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(GameTexts
                    .FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
                    .ToString());
            }

            if (CommandSystemConfig.Get().AttackSpecificFormation)
            {
                MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(GameTexts
                    .FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString())
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
                formationNames.Add(GameTexts.FindText("str_formation_class_string", formation.PrimaryClass.GetName()));
            }

            if (!formationNames.IsEmpty())
            {
                var message = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
                message.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(formationNames, false));
                message.SetTextVariable("STR2",
                    GameTexts.FindText("str_formation_ai_sergeant_instruction_behavior_text",
                            nameof(BehaviorTacticalCharge))
                        .SetTextVariable("TARGET_FORMATION", GameTexts.FindText("str_troop_group_name", ((int)targetFormation.PrimaryClass).ToString())));
                MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString());
            }
        }

        public static void DisplayFormationReadyMessage(Formation formation)
        {
            var message = GameTexts.FindText("str_formation_ai_behavior_text", nameof(BehaviorStop));
            message.SetTextVariable("IS_PLURAL", 0);
            message.SetTextVariable("TROOP_NAMES_BEGIN", "");
            message.SetTextVariable("TROOP_NAMES_END", GameTexts.FindText("str_troop_group_name", ((int)formation.PrimaryClass).ToString()));
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(message.ToString());
        }

        public static bool ShouldChargeToFormation(Agent agent)
        {
            return agent.Formation != null && agent.Formation.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget &&
                   CommandSystemConfig.Get().AttackSpecificFormation &&
                       (QueryLibrary.IsCavalry(agent) ||
                        QueryLibrary.IsRangedCavalry(agent) && agent.Formation.FiringOrder.OrderType == OrderType.HoldFire ||
                        CommandSystemSubModule.EnableChargeToFormationForInfantry &&
                            (QueryLibrary.IsInfantry(agent) || QueryLibrary.IsRanged(agent) && agent.Formation.FiringOrder.OrderType == OrderType.HoldFire));
        }
    }
}
