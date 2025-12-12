using HarmonyLib;
using MissionLibrary;
using MissionLibrary.Controller;
using MissionLibrary.View;
using MissionSharedLibrary;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.CampaignGame.Skills;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Patch;
using RTSCamera.Patch.Fix;
using RTSCamera.Patch.Naval;
using RTSCamera.Patch.TOR_fix;
using RTSCamera.Usage;
using SandBox.Objects;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera
{
    public class RTSCameraSubModule : MBSubModuleBase
    {
        public const string ModuleId = "RTSCamera";
        public const string OldModuleId = "EnhancedMission";

        private readonly Harmony _harmony = new Harmony("RTSCameraPatch");
        private bool _successPatch;
        public static bool IsCommandSystemInstalled = false;
        public static bool IsNavalInstalled = false;
        public static bool IsHelmsmanInstalled = false;

        // random generated
        public const int MissionTimeSpeedRequestId = 936012602;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            IsCommandSystemInstalled = Utility.IsModuleInstalled("RTSCamera.CommandSystem");
            IsNavalInstalled = Utility.IsModuleInstalled("NavalDLC");
            IsHelmsmanInstalled = Utility.IsModuleInstalled("Helmsman");
            Utility.ShouldDisplayMessage = true;
            Initialize();

            try
            {
                _successPatch = true;

                //_harmony.Patch(
                //    typeof(CommonVillagersCampaignBehavior).GetMethod("CheckIfConversationAgentIsEscortingTheMainAgent",
                //        BindingFlags.Instance | BindingFlags.NonPublic),
                //    prefix: new HarmonyMethod(typeof(CheckIfConversationAgentIsEscortingTheMainAgent).GetMethod(
                //        nameof(CheckIfConversationAgentIsEscortingTheMainAgent.Prefix_CheckIfConversationAgentIsEscortingTheMainAgent),
                //        BindingFlags.Static | BindingFlags.Public)));
                //_harmony.Patch(
                //    typeof(GuardsCampaignBehavior).GetMethod("CheckIfConversationAgentIsEscortingTheMainAgent",
                //        BindingFlags.Instance | BindingFlags.NonPublic),
                //    prefix: new HarmonyMethod(typeof(CheckIfConversationAgentIsEscortingTheMainAgent).GetMethod(
                //        nameof(CheckIfConversationAgentIsEscortingTheMainAgent
                //            .Prefix_CheckIfConversationAgentIsEscortingTheMainAgent),
                //        BindingFlags.Static | BindingFlags.Public)));

                // below checked
                _successPatch &= Patch_PassageUsePoint.Patch(_harmony);
                _successPatch &= Patch_OrderOfBattleVM.Patch(_harmony);
                _successPatch &= Patch_DeploymentMissionController.Patch(_harmony);
                _successPatch &= Patch_LadderQueueManager.Patch(_harmony);
                _successPatch &= Patch_MissionFormationTargetSelectionHandler.Patch(_harmony);
                _successPatch &= Patch_FormationMarkerListPanel.Patch(_harmony);
                _successPatch &= Patch_OrderTroopPlacer.Patch(_harmony);
                _successPatch &= Patch_RangedSiegeWeaponView.Patch(_harmony);
                _successPatch &= Patch_ArenaPracticeFightMissionController.Patch(_harmony);
                _successPatch &= Patch_MissionAgentLabelView.Patch(_harmony);
                _successPatch &= Patch_MissionBoundaryCrossingHandler.Patch(_harmony);
                //_successPatch &= Patch_MissionFormationMarkerVM.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletFormationMarker.Patch(_harmony);
                _successPatch &= Patch_MissionOrderVM.Patch(_harmony);
                _successPatch &= Patch_MissionOrderTroopControllerVM.Patch(_harmony);
                _successPatch &= Patch_CrosshairVM.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletSpectatorControl.Patch(_harmony);
                _successPatch &= Patch_ScoreboardScreenWidget.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletMainAgentEquipDropView.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletMainAgentEquipmentControllerView.Patch(_harmony);
                _successPatch &= Patch_AgentHumanAILogic.Patch(_harmony);
                _successPatch &= Patch_Mission.Patch(_harmony);
                _successPatch &= Patch_LineFormation.Patch(_harmony);
                _successPatch &= Patch_ColumnFormation.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletSingleplayerOrderUIHandler.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletCrosshair.Patch(_harmony);
                _successPatch &= Patch_HideoutMissionController.Patch(_harmony);
                _successPatch &= Patch_OrderFlag.Patch(_harmony);
                _successPatch &= Patch_SandboxBattleBannerBearsModel.Patch(_harmony);
                _successPatch &= Patch_OrderItemBaseVM.Patch(_harmony);
                _successPatch &= Patch_BattleEndLogic.Patch(_harmony);
                // naval dlc
                if (IsNavalInstalled)
                {
                    _successPatch &= Patch_MissionShipControlView.Patch(_harmony);
                    _successPatch &= Patch_MissionShip.Patch(_harmony);
                    _successPatch &= Patch_NavalDLCHelpers.Patch(_harmony);
                    _successPatch &= Patch_MissionGauntletNavalOrderUIHandler.Patch(_harmony);
                    _successPatch &= Patch_NarvalShipTargetSelectionHandler.Patch(_harmony);
                    _successPatch &= Patch_ShipAgentSpawnLogicTeamSide.Patch(_harmony);
                    _successPatch &= Patch_NavalShipVisualOrderProvider.Patch(_harmony);
                    _successPatch &= Patch_NavalTroopVisualOrderProvider.Patch(_harmony);
                    _successPatch &= Patch_ShipOrder.Patch(_harmony);
                    _successPatch &= Patch_ShipControllerMachine.Patch(_harmony);
                    _successPatch &= Patch_AgentNavalComponent.Patch(_harmony);
                    _successPatch &= Patch_NavalMovementOrder.Patch(_harmony);
                }

                // Use Patch to add game menu
                CommandBattleBehavior.Patch(_harmony);
            }
            catch (Exception e)
            {
                _successPatch = false;
                MBDebug.Print(e.ToString());
            }
        }

        private void Initialize()
        {
            if (!Initializer.Initialize(ModuleId))
                return;
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            Initializer.OnApplicationTick(dt);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            Patch_CareerHelper.Patch(_harmony);

            if (!ThirdInitialize())
                return;

            if (!_successPatch)
            {
                InformationManager.DisplayMessage(new InformationMessage("RTS Camera: patch failed"));
            }
            if (IsHelmsmanInstalled)
            {
                Utilities.Utility.PrintHelmsmanWarning();
            }
            try
            {
                Module.CurrentModule.GlobalTextManager.LoadGameTexts();
            }
            catch (Exception e)
            {
                MBDebug.Print(e.ToString());
                InformationManager.DisplayMessage(new InformationMessage($"RTS Camera: failed to load game texts: {e}"));
            }

            Utility.ShouldDisplayMessage = RTSCameraConfig.Get().DisplayMessage;
            Utility.PrintUsageHint();
        }

        private bool ThirdInitialize()
        {
            if (!Initializer.ThirdInitialize())
                return false;

            RTSCameraGameKeyCategory.RegisterGameKeyCategory();
            RTSCameraUsageCategory.RegisterUsageCategory();
            var missionStartingManager = Global.GetInstance<AMissionStartingManager>();
            missionStartingManager.AddSingletonHandler("RTSCameraAgentComponent.MissionStartingHandler",
                new RTSCameraAgentComponent.MissionStartingHandler(), new Version(1, 0, 0));
            missionStartingManager.AddHandler(new MissionStartingHandler.MissionStartingHandler());
            var menuClassCollection = AMenuManager.Get().MenuClassCollection;
            AMenuManager.Get().OnMenuClosedEvent += RTSCameraConfig.OnMenuClosed;
            menuClassCollection.RegisterItem(RTSCameraOptionClassFactory.CreateOptionClassProvider(menuClassCollection));
            return true;
        }


        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            game.GameTextManager.LoadGameTexts();
            AddCampaignBehavior(gameStarterObject);
        }

        public override void RegisterSubModuleObjects(bool isSavedCampaign)
        {
            base.RegisterSubModuleObjects(isSavedCampaign);
            RTSCameraSkillEffects.Initialize();

        }

        private void AddCampaignBehavior(object gameStarter)
        {
            if (gameStarter is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new CommandBattleBehavior());
                campaignGameStarter.AddBehavior(new RTSCameraSkillBehavior());
            }
        }
    }
}
