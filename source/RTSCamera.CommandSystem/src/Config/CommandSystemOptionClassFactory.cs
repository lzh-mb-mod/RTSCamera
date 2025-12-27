using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.View.ViewModelCollection;
using MissionSharedLibrary.View.ViewModelCollection.Options;
using MissionSharedLibrary.View.ViewModelCollection.Options.Selection;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Orders;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Config
{
    public class CommandSystemOptionClassFactory
    {
        public static IProvider<AOptionClass> CreateOptionClassProvider(AMenuClassCollection menuClassCollection)
        {
            return ProviderCreator.Create(() =>
            {
                var outlineView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().OutlineColorSubLogic;
                var groundMarkerView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().GroundMarkerColorSubLogic;

                var optionClass = new OptionClass(CommandSystemSubModule.ModuleId,
                    GameTexts.FindText("str_rts_camera_command_system_option_class"), menuClassCollection);
                var commandOptionCategory = new OptionCategory("Command", GameTexts.FindText("str_rts_camera_command_system_command_system_options"),
                    () => CommandSystemConfig.Get().IsCommandOptionVisible, (b) => CommandSystemConfig.Get().IsCommandOptionVisible = b);
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName", CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString()),
                    () => CommandSystemConfig.Get().ClickToSelectFormation, b =>
                    {
                        CommandSystemConfig.Get().ClickToSelectFormation = b;
                        outlineView?.SetEnableColorForSelectedFormation(b);
                        groundMarkerView?.SetEnableColorForSelectedFormation(b);
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName", CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString()),
                    () => CommandSystemConfig.Get().AttackSpecificFormation, b =>
                    {
                        CommandSystemConfig.Get().AttackSpecificFormation = b;
                    }));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_after_enemy_formation_eliminated"),
                    GameTexts.FindText("str_rts_camera_command_system_after_enemy_formation_eliminated_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().BehaviorAfterCharge = (BehaviorAfterCharge)i,
                        () => (int)CommandSystemConfig.Get().BehaviorAfterCharge, () => 2, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_after_charge_behavior", "charge"),
                            new SelectionItem(true, "str_rts_camera_command_system_after_charge_behavior", "hold")
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_troop_highlight_character_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_troop_highlight_character_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().TroopHighlightStyleInCharacterMode = (TroopHighlightStyle)i,
                        () => (int)CommandSystemConfig.Get().TroopHighlightStyleInCharacterMode, () => (int)TroopHighlightStyle.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.No)),
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.Outline)),
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.GroundMarker))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_troop_highlight_rts_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_troop_highlight_rts_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().TroopHighlightStyleInRTSMode = (TroopHighlightStyle)i,
                        () => (int)CommandSystemConfig.Get().TroopHighlightStyleInRTSMode, () => (int)TroopHighlightStyle.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.No)),
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.Outline)),
                            new SelectionItem(true, "str_rts_camera_command_system_troop_highlight_option", nameof(TroopHighlightStyle.GroundMarker))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_highlight_troops_when_showing_indicators"),
                    GameTexts.FindText("str_rts_camera_command_system_highlight_troops_when_showing_indicators_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().HighlightTroopsWhenShowingIndicators = (ShowMode)i,
                    () => (int)CommandSystemConfig.Get().HighlightTroopsWhenShowingIndicators, () => (int)ShowMode.Count, () => new List<SelectionItem>
                    {
                        new SelectionItem(true, "str_rts_camera_command_system_highlight_troops_when_showing_indicators_option", nameof(ShowMode.Never)),
                        new SelectionItem(true, "str_rts_camera_command_system_highlight_troops_when_showing_indicators_option", nameof(ShowMode.FreeCameraOnly)),
                        new SelectionItem(true, "str_rts_camera_command_system_highlight_troops_when_showing_indicators_option", nameof(ShowMode.Always))
                    }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_character_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_character_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().MovementTargetHighlightStyleInCharacterMode = (MovementTargetHighlightStyle)i,
                        () => (int)CommandSystemConfig.Get().MovementTargetHighlightStyleInCharacterMode, () => (int)MovementTargetHighlightStyle.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.Original)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.NewModelOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.AlwaysVisible)),
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_rts_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_movement_target_highlight_rts_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().MovementTargetHighlightStyleInRTSMode = (MovementTargetHighlightStyle)i,
                        () => (int)CommandSystemConfig.Get().MovementTargetHighlightStyleInRTSMode, () => (int)MovementTargetHighlightStyle.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.Original)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.NewModelOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_movement_target_highlight_style_option", nameof(MovementTargetHighlightStyle.AlwaysVisible)),
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_flag_show_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_flag_show_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CommandQueueFlagShowMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().CommandQueueFlagShowMode, () => (int)ShowMode.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_flag_show_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_arrow_show_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_arrow_show_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CommandQueueArrowShowMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().CommandQueueArrowShowMode, () => (int)ShowMode.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_arrow_show_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_formation_shape_show_mode"),
                    GameTexts.FindText("str_rts_camera_command_system_command_queue_formation_shape_show_mode_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CommandQueueFormationShapeShowMode = (ShowMode)i,
                        () => (int)CommandSystemConfig.Get().CommandQueueFormationShapeShowMode, () => (int)ShowMode.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_formation_shape_show_mode_option", nameof(ShowMode.Never)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_formation_shape_show_mode_option", nameof(ShowMode.FreeCameraOnly)),
                            new SelectionItem(true, "str_rts_camera_command_system_command_queue_formation_shape_show_mode_option", nameof(ShowMode.Always))
                        }), false));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_formation_lock_condition"),
                    GameTexts.FindText("str_rts_camera_command_system_formation_lock_condition_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().FormationLockCondition = (FormationLockCondition)i,
                        () => (int)CommandSystemConfig.Get().FormationLockCondition, () => (int)FormationLockCondition.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "Never"),
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "WhenPressed"),
                            new SelectionItem(true, "str_rts_camera_command_system_formation_lock_condition_option", "WhenNotPressed")
                        }), false));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_sync_locked_formation_speed"),
                    GameTexts.FindText("str_rts_camera_command_system_sync_locked_formation_speed_hint"),
                    () => CommandSystemConfig.Get().ShouldSyncFormationSpeed,
                    b => CommandSystemConfig.Get().ShouldSyncFormationSpeed = b));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_hollow_square_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_hollow_square_formation_hint"),
                    () => CommandSystemConfig.Get().HollowSquare, b =>
                    {
                        CommandSystemConfig.Get().HollowSquare = b;
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_square_formation_corner_fix"),
                    GameTexts.FindText("str_rts_camera_command_system_square_formation_corner_fix_hint"),
                    () => CommandSystemConfig.Get().SquareFormationCornerFix, b =>
                    {
                        CommandSystemConfig.Get().SquareFormationCornerFix = b;
                    }));
                commandOptionCategory.AddOption(new SelectionOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_circle_formation_preference"),
                    GameTexts.FindText("str_rts_camera_command_system_circle_formation_preference_hint"),
                    new SelectionOptionData(i => CommandSystemConfig.Get().CircleFormationUnitSpacingPreference = (CircleFormationUnitSpacingPreference)i,
                        () => (int)CommandSystemConfig.Get().CircleFormationUnitSpacingPreference, () => (int)CircleFormationUnitSpacingPreference.Count, () => new List<SelectionItem>
                        {
                            new SelectionItem(true, "str_rts_camera_command_system_circle_formation_preference_option", "Tight"),
                            new SelectionItem(true, "str_rts_camera_command_system_circle_formation_preference_option", "Loose")
                        }), false));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_order_ui_clickable"),
                    GameTexts.FindText("str_rts_camera_command_system_order_ui_clickable_hint"),
                    () => CommandSystemConfig.Get().OrderUIClickable,
                    b => CommandSystemConfig.Get().OrderUIClickable = UIConfig.DoNotUseGeneratedPrefabs = b));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_order_ui_clickable_extension"),
                    GameTexts.FindText("str_rts_camera_command_system_order_ui_clickable_extension_hint").SetTextVariable("KeyName", CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectTargetForCommand).ToSequenceString()),
                    () => CommandSystemConfig.Get().OrderUIClickableExtension,
                    b => {
                        CommandSystemConfig.Get().OrderUIClickableExtension = b;
                        if (!b)
                        {
                            RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
                        }
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_face_enemy_by_default"),
                    GameTexts.FindText("str_rts_camera_command_system_face_enemy_by_default_hint"),
                    () => CommandSystemConfig.Get().FacingEnemyByDefault,
                    b => CommandSystemConfig.Get().FacingEnemyByDefault = b));
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


                var advanceOrderOptionCategory = new OptionCategory("AdvanceOrder", GameTexts.FindText("str_rts_camera_command_system_advance_order_options"),
                    () => CommandSystemConfig.Get().IsAdvanceOrderOptionVisible, (b) => CommandSystemConfig.Get().IsAdvanceOrderOptionVisible = b);
                advanceOrderOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_fix_advance_order_for_throwing_weapons"),
                    GameTexts.FindText("str_rts_camera_command_system_fix_advance_order_for_throwing_weapons_hint"),
                    () => CommandSystemConfig.Get().FixAdvaneOrderForThrowing,
                    b => CommandSystemConfig.Get().FixAdvaneOrderForThrowing = b));
                advanceOrderOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_apply_advance_order_fix_for_ai"),
                    GameTexts.FindText("str_rts_camera_command_system_apply_advance_order_fix_for_ai_hint"),
                    () => CommandSystemConfig.Get().ApplyAdvanceOrderFixForAI,
                    b => CommandSystemConfig.Get().ApplyAdvanceOrderFixForAI = b));
                advanceOrderOptionCategory.AddOption(new NumericOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_thrower_ratio_threshold"),
                    GameTexts.FindText("str_rts_camera_command_system_thrower_ratio_threshold_hint"),
                    () => CommandSystemConfig.Get().ThrowerRatioThreshold, f =>
                    {
                        CommandSystemConfig.Get().ThrowerRatioThreshold = f;
                    }, 0f, 1f, false, true));
                advanceOrderOptionCategory.AddOption(new NumericOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_remaining_ammo_ratio_threshold"),
                    GameTexts.FindText("str_rts_camera_command_system_remaining_ammo_ratio_threshold_hint"),
                    () => CommandSystemConfig.Get().RemainingAmmoRatioThreshold, f =>
                    {
                        CommandSystemConfig.Get().RemainingAmmoRatioThreshold = f;
                    }, 0f, 1f, false, true));
                advanceOrderOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_shorten_range_based_on_remaining_ammo"),
                    GameTexts.FindText("str_rts_camera_command_system_shorten_range_based_on_remaining_ammo_hint"),
                    () => CommandSystemConfig.Get().ShortenRangeBasedOnRemainingAmmo,
                    b => CommandSystemConfig.Get().ShortenRangeBasedOnRemainingAmmo = b));
                optionClass.AddOptionCategory(1, advanceOrderOptionCategory);

                return optionClass;
            }, CommandSystemSubModule.ModuleId, new Version(1, 0, 0));
        }
    }
}
