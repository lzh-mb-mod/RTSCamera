using MissionLibrary;
using MissionLibrary.Controller;
using MissionLibrary.View;
using MissionSharedLibrary;
using MissionSharedLibrary.Provider;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Patch;
using System;
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
        public static bool EnableChargeToFormationForInfantry = true;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Module.CurrentModule.GlobalTextManager.LoadGameTexts();

            Utility.ShouldDisplayMessage = true;
            Initialize();

            // If RBM is loaded, disable the ChargeToFormation feature for infantry to not break RBM frontline behavior
            EnableChargeToFormationForInfantry =
                TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).FirstOrDefault(info =>
                info.Id == "RBM") == null
            &&
                TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).FirstOrDefault(info =>
                info.Id == "RealisticBattleAiModule") == null;
        }

        private void Initialize()
        {
            if (!Initializer.Initialize(ModuleId))
                return;
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();


            if (!SecondInitialize())
                return;
        }

        private bool SecondInitialize()
        {
            if (!Initializer.SecondInitialize())
                return false;

            CommandSystemGameKeyCategory.RegisterGameKeyCategory();
            AMenuManager.Get().OnMenuClosedEvent += CommandSystemConfig.OnMenuClosed;
            var menuClassCollection = AMenuManager.Get().MenuClassCollection;
            menuClassCollection.AddOptionClass(CommandSystemOptionClassFactory.CreateOptionClassProvider(menuClassCollection));
            Global.GetProvider<AMissionStartingManager>().AddHandler(new CommandSystemMissionStartingHandler());
            Global.RegisterProvider(
                VersionProviderCreator.Create(() => new RTSCameraAgentComponent.MissionStartingHandler(),
                    new Version(1, 0, 0)), "RTSCameraAgentComponent.MissionStartingHandler");

            bool successPatch = true;
            successPatch &=  Patch_OrderTroopPlacer.Patch();

            if (!successPatch)
            {
                InformationManager.DisplayMessage(new InformationMessage("RTS Camera Command System: patch failed"));
            }
            return true;
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            //game.GameTextManager.LoadGameTexts();
        }
    }
}
