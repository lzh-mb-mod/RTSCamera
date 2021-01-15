using MissionLibrary;
using MissionLibrary.Controller;
using MissionLibrary.View;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemSubModule : MBSubModuleBase
    {
        public static readonly string ModuleId = "RTSCamera.CommandSystem";
        public bool _isInitialized = false;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Module.CurrentModule.GlobalTextManager.LoadGameTexts(BasePath.Name +
                                                                 $"Modules/{ModuleId}/ModuleData/module_strings.xml");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (_isInitialized)
                return;

            _isInitialized = true;
            CommandSystemGameKeyCategory.RegisterGameKeyCategory();
            AMenuManager.Get().OnMenuClosedEvent += CommandSystemConfig.OnMenuClosed;
            var menuClassCollection = AMenuManager.Get().MenuClassCollection;
            menuClassCollection.AddOptionClass(CommandSystemOptionClassFactory.CreateOptionClassProvider(menuClassCollection));
            Global.GetProvider<AMissionStartingManager>().AddHandler(new CommandSystemMissionStartingHandler());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);


            game.GameTextManager.LoadGameTexts(BasePath.Name + $"Modules/{ModuleId}/ModuleData/module_strings.xml");
        }
    }
}
