using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.View.ViewModelCollection;
using MissionSharedLibrary.View.ViewModelCollection.Options;
using MissionSharedLibrary.View.ViewModelCollection.Options.Selection;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Config
{
    public class CommandSystemOptionClassFactory
    {
        public static IProvider<AOptionClass> CreateOptionClassProvider(AMenuClassCollection menuClassCollection)
        {
            return ProviderCreator.Create(() =>
            {
                var contourView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().FormationColorSubLogic;

                var optionClass = new OptionClass(CommandSystemSubModule.ModuleId,
                    GameTexts.FindText("str_rts_camera_command_system_option_class"), menuClassCollection);
                var commandOptionCategory = new OptionCategory("Command", GameTexts.FindText("str_rts_camera_command_system_command_system_options"));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName", CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString()),
                    () => CommandSystemConfig.Get().ClickToSelectFormation, b =>
                    {
                        CommandSystemConfig.Get().ClickToSelectFormation = b;
                        contourView?.SetEnableContourForSelectedFormation(b);
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName", CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString()),
                    () => CommandSystemConfig.Get().AttackSpecificFormation, b =>
                    {
                        CommandSystemConfig.Get().AttackSpecificFormation = b;
                        if (b)
                            PatchChargeToFormation.Patch();
                    }));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_after_enemy_formation_eliminated"),
                    GameTexts.FindText("str_rts_camera_command_system_after_enemy_formation_eliminated_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().BehaviorAfterCharge = (BehaviorAfterCharge)i,
                        () => (int)CommandSystemConfig.Get().BehaviorAfterCharge, 2, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_after_charge_behavior", "charge"),
                            new SelectionItem(true, "str_rts_camera_command_system_after_charge_behavior", "hold")
                        }), false));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_highlight_selected_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_highlight_selected_formation_hint"),
                    () => CommandSystemConfig.Get().HighlightSelectedFormation, b =>
                    {
                        CommandSystemConfig.Get().HighlightSelectedFormation = b;
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_highlight_target_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_highlight_target_formation_hint"),
                    () => CommandSystemConfig.Get().HighlightTargetFormation, b =>
                    {
                        CommandSystemConfig.Get().HighlightTargetFormation = b;
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_highlight_on_rts_view_only"),
                    GameTexts.FindText("str_rts_camera_command_system_highlight_on_rts_view_only_hint"),
                    () => CommandSystemConfig.Get().HighlightOnRtsViewOnly, b =>
                    {
                        CommandSystemConfig.Get().HighlightOnRtsViewOnly = b;
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_more_visible_movement_target"),
                    GameTexts.FindText("str_rts_camera_command_system_more_visible_movement_target_hint"),
                    () => CommandSystemConfig.Get().MoreVisibleMovementTarget, b =>
                    {
                        CommandSystemConfig.Get().MoreVisibleMovementTarget = b;
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_more_visible_on_rts_view_only"),
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_more_visible_on_rts_view_only_hint"),
                    () => CommandSystemConfig.Get().MovementTargetMoreVisibleOnRtsViewOnly, b =>
                    {
                        CommandSystemConfig.Get().MovementTargetMoreVisibleOnRtsViewOnly = b;
                    }));
                optionClass.AddOptionCategory(0, commandOptionCategory);

                return optionClass;
            }, CommandSystemSubModule.ModuleId, new Version(1, 0, 0));
        }
    }
}
