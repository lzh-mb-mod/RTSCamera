using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.View.ViewModelCollection;
using MissionSharedLibrary.View.ViewModelCollection.Options;
using MissionSharedLibrary.View.ViewModelCollection.Options.Selection;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.CommandSystem.View;
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
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_selected_formation_highlight_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_selected_formation_highlight_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().SelectedFormationHighlightMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().SelectedFormationHighlightMode, (int)ShowMode.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_selected_formation_highlight_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_selected_formation_highlight_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_selected_formation_highlight_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_target_formation_highlight_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_target_formation_highlight_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().TargetFormationHighlightMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().TargetFormationHighlightMode, (int)ShowMode.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_target_formation_highlight_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_target_formation_highlight_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_target_formation_highlight_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().MovementTargetHighlightMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().MovementTargetHighlightMode, (int)ShowMode.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_flag_show_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_flag_show_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CommandQueueFlagShowMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().CommandQueueFlagShowMode, (int)ShowMode.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_arrow_show_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_arrow_show_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CommandQueueArrowShowMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().CommandQueueArrowShowMode, (int)ShowMode.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_formation_lock_condition"),
                    GameTexts.FindText("str_rts_camera_command_system_formation_lock_condition_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().FormationLockCondition = (FormationLockCondition)i,
                        () => (int)CommandSystemConfig.Get().FormationLockCondition, (int)FormationLockCondition.Count, new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "Never"),
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "WhenPressed"),
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "WhenNotPressed")
                        }), false));
                //commandOptionCategory.AddOption(new NumericOptionViewModel(
                //    new TaleWorlds.Localization.TextObject("r"), null,
                //    () => CommandQueuePreview.r, f =>
                //    {
                //        CommandQueuePreview.r = f;
                //        CommandQueuePreview.ClearArrows();
                //    }, 0, 1, false, true));
                //commandOptionCategory.AddOption(new NumericOptionViewModel(
                //    new TaleWorlds.Localization.TextObject("g"), null,
                //    () => CommandQueuePreview.g, f =>
                //    {
                //        CommandQueuePreview.g = f;
                //        CommandQueuePreview.ClearArrows();
                //    }, 0, 1, false, true));
                //commandOptionCategory.AddOption(new NumericOptionViewModel(
                //    new TaleWorlds.Localization.TextObject("b"), null,
                //    () => CommandQueuePreview.b, f =>
                //    {
                //        CommandQueuePreview.b = f;
                //        CommandQueuePreview.ClearArrows();
                //    }, 0, 1, false, true));
                //commandOptionCategory.AddOption(new NumericOptionViewModel(
                //    new TaleWorlds.Localization.TextObject("a"), null,
                //    () => CommandQueuePreview.a, f =>
                //    {
                //        CommandQueuePreview.a = f;
                //    }, 0, 1, false, true));
                optionClass.AddOptionCategory(0, commandOptionCategory);

                return optionClass;
            }, CommandSystemSubModule.ModuleId, new Version(1, 0, 0));
        }
    }
}
