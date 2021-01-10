using MissionLibrary.Provider;
using MissionLibrary.View;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.View.ViewModelCollection;
using TaleWorlds.Core;

namespace RTSCamera.CommandSystem.Config
{
    public class CommandSystemOptionClassFactory
    {
        public static IIdProvider<AOptionClass> CreateOptionClassProvider(IMenuClassCollection menuClassCollection)
        {
            return IdProviderCreator.Create(() =>
            {
                var optionClass = new OptionClass(CommandSystemSubModule.ModuleId,
                    GameTexts.FindText("str_rts_camera_mod_name"), menuClassCollection);
                var commandOptionCategory = new OptionCategory("Command", GameTexts.FindText(""));

                return optionClass;
            }, CommandSystemSubModule.ModuleId);
        }
    }
}
