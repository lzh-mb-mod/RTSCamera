using MissionLibrary;
using MissionLibrary.Controller;
using MissionLibrary.View;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemSubModule : MBSubModuleBase
    {
        public static readonly string ModuleId = "RTSCamera.CommandSystem";
        public bool _isInitialized = false;
        public static bool EnableChargeToFormationForInfantry = true;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            EnableChargeToFormationForInfantry =
                TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).FirstOrDefault(info => info.Id == "RealisticBattleAiModule") == null;

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
