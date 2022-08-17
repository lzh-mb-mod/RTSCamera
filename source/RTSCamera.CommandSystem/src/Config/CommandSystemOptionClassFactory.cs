using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.Utilities;
using MissionSharedLibrary.View.ViewModelCollection;
using MissionSharedLibrary.View.ViewModelCollection.Options;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
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
                optionClass.AddOptionCategory(0, commandOptionCategory);

                var miscellaneousOptionCategory = new OptionCategory("Miscellaneous",
                                    GameTexts.FindText("str_rts_camera_miscellaneous_options"));
                miscellaneousOptionCategory.AddOption(new BoolOptionViewModel(
                    GameTexts.FindText("str_rts_camera_display_mod_message"),
                    GameTexts.FindText("str_rts_camera_display_message_hint"),
                    () => CommandSystemConfig.Get().DisplayMessage, b =>
                    {
                        CommandSystemConfig.Get().DisplayMessage = b;
                        Utility.ShouldDisplayMessage = b;
                    }));
                optionClass.AddOptionCategory(1, miscellaneousOptionCategory);

                return optionClass;
            }, CommandSystemSubModule.ModuleId);
        }
    }
}
