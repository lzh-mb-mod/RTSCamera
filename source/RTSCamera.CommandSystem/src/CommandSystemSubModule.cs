using HarmonyLib;
using MissionLibrary.Controller;
using MissionLibrary.View;
using MissionSharedLibrary;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.CampaignGame;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Orders;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.CommandSystem.Usage;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemSubModule : MBSubModuleBase
    {
        public static readonly string ShortModuleId = "RTSCommand";
        public static readonly string ModuleId = "RTSCamera.CommandSystem";
        public static bool IsRealisticBattleModuleInstalled = true;

        private readonly Harmony _harmony = new Harmony("RTSCommandPatch");
        private bool _successPatch;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // If RBM is loaded, disable the ChargeToFormation feature for infantry to not break RBM frontline behavior
            IsRealisticBattleModuleInstalled =
                TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).FirstOrDefault(info =>
                    info.Id == "RBM") != null
                &&
                TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).FirstOrDefault(info =>
                    info.Id == "RealisticBattleAiModule") == null;

            Module.CurrentModule.GlobalTextManager.LoadGameTexts();

            Utility.ShouldDisplayMessage = true;
            Initialize();

            if (!UIConfig.DoNotUseGeneratedPrefabs && CommandSystemConfig.Get().OrderUIClickable)
            {
                UIConfig.DoNotUseGeneratedPrefabs = true;
            }

            VisualOrderFactory.RegisterProvider(new RTSCommandVisualOrderProvider());
        }

        private void Initialize()
        {
            if (!Initializer.Initialize(ShortModuleId))
                return;
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();


            if (!ThirdInitialize())
                return;

            Utilities.Utility.PrintOrderHint();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            Initializer.OnApplicationTick(dt);
        }

        private bool ThirdInitialize()
        {
            if (!Initializer.ThirdInitialize())
                return false;

            CommandSystemGameKeyCategory.RegisterGameKeyCategory();
            CommandSystemUsageCategory.RegisterUsageCategory();
            AMenuManager.Get().OnMenuClosedEvent += CommandSystemConfig.OnMenuClosed;
            var menuClassCollection = AMenuManager.Get().MenuClassCollection;
            menuClassCollection.RegisterItem(CommandSystemOptionClassFactory.CreateOptionClassProvider(menuClassCollection));
            var missionStartingManager = AMissionStartingManager.Get();
            missionStartingManager.AddHandler(new CommandSystemMissionStartingHandler());
            missionStartingManager.AddSingletonHandler("RTSCameraAgentComponent.MissionStartingHandler",
                new RTSCameraAgentComponent.MissionStartingHandler(), new Version(1, 0, 0));

            _successPatch = true;
            _successPatch &=  Patch_OrderTroopPlacer.Patch(_harmony);
            _successPatch &= Patch_OrderTroopItemVM.Patch(_harmony);
            //_successPatch &= Patch_FormationMarkerParentWidget.Patch(_harmony);
            _successPatch &= Patch_MissionOrderTroopControllerVM.Patch(_harmony);
            // Patch issue that order troop placer is inconsistent with actual order issued during dragging
            _successPatch &= Patch_OrderController.Patch(_harmony);
            _successPatch &= Patch_Formation.Patch(_harmony);

            // command queue
            _successPatch &= Patch_MissionOrderVM.Patch(_harmony);
            _successPatch &= Patch_GauntletOrderUIHandler.Patch(_harmony);

            // resizable square formation
            _successPatch &= Patch_ArrangementOrder.Patch(_harmony);

            // fix unit direction of square formation in the corner
            _successPatch &= Patch_SquareFormation.Patch(_harmony);

            // allows setting target formation to face to when facing enemy
            _successPatch &= Patch_FacingOrder.Patch(_harmony);

            // solid circle formation
            _successPatch &= Patch_CircularFormation.Patch(_harmony);
            if (!_successPatch)
            {
                InformationManager.DisplayMessage(new InformationMessage("RTS Camera Command System: patch failed"));
            }
            return true;
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            CommandSystemSkillBehavior.CanIssueChargeToFormationOrder = true;

            game.GameTextManager.LoadGameTexts();
            if (gameStarterObject is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new CommandSystemSkillBehavior());
            }
        }
    }
}
