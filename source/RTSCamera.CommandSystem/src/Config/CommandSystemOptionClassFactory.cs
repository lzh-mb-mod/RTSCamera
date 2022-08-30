using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.View.ViewModelCollection;
using MissionSharedLibrary.View.ViewModelCollection.Options;
using MissionSharedLibrary.View.ViewModelCollection.Options.Selection;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Config
{
    public class CommandSystemOptionClassFactory
    {
        public static IIdProvider<AOptionClass> CreateOptionClassProvider(IMenuClassCollection menuClassCollection)
        {
            return IdProviderCreator.Create(() =>
            {
                var contourView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().FormationColorSubLogic;

                var optionClass = new OptionClass(CommandSystemSubModule.ModuleId,
                    GameTexts.FindText("str_rts_camera_command_system_option_class"), menuClassCollection);
                var commandOptionCategory = new OptionCategory("Command", GameTexts.FindText("str_rts_camera_command_system_command_system_options"));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_click_to_select_formation_hint"),
                    () => CommandSystemConfig.Get().ClickToSelectFormation, b =>
                    {
                        CommandSystemConfig.Get().ClickToSelectFormation = b;
                        contourView?.SetEnableContourForSelectedFormation(b);
                    }));
                commandOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation"),
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation_hint"),
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
                optionClass.AddOptionCategory(0, commandOptionCategory);

                return optionClass;
            }, CommandSystemSubModule.ModuleId);
        }
    }
}
