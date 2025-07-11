using HarmonyLib;
using MissionLibrary;
using MissionLibrary.Controller;
using MissionLibrary.Extension;
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
using RTSCamera.Usage;
using SandBox.CampaignBehaviors;
using SandBox.Objects;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
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

        // random generated
        public const int MissionTimeSpeedRequestId = 936012602;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                Utility.ShouldDisplayMessage = true;
                Module.CurrentModule.GlobalTextManager.LoadGameTexts();
                Initialize();

                _successPatch = true;

                _harmony.Patch(
                    typeof(CommonVillagersCampaignBehavior).GetMethod("CheckIfConversationAgentIsEscortingTheMainAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(CheckIfConversationAgentIsEscortingTheMainAgent).GetMethod(
                        nameof(CheckIfConversationAgentIsEscortingTheMainAgent.Prefix_CheckIfConversationAgentIsEscortingTheMainAgent),
                        BindingFlags.Static | BindingFlags.Public)));
                _harmony.Patch(
                    typeof(GuardsCampaignBehavior).GetMethod("CheckIfConversationAgentIsEscortingTheMainAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(CheckIfConversationAgentIsEscortingTheMainAgent).GetMethod(
                        nameof(CheckIfConversationAgentIsEscortingTheMainAgent
                            .Prefix_CheckIfConversationAgentIsEscortingTheMainAgent),
                        BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    typeof(PassageUsePoint).GetMethod(nameof(PassageUsePoint.IsDisabledForAgent),
                        BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_PassageUsePoint).GetMethod(
                        nameof(Patch_PassageUsePoint.IsDisabledForAgent_Prefix),
                        BindingFlags.Static | BindingFlags.Public)));

                //_successPatch &= Patch_OrderOfBattleVM.Patch();
                // below checked
                _successPatch &= Patch_DeploymentMissionController.Patch();
                _successPatch &= Patch_LadderQueueManager.Patch(_harmony);
                _successPatch &= Patch_MissionFormationTargetSelectionHandler.Patch(_harmony);
                _successPatch &= Patch_OrderTroopPlacer.Patch(_harmony);
                _successPatch &= Patch_RangedSiegeWeaponView.Patch(_harmony);
                _successPatch &= Patch_ArenaPracticeFightMissionController.Patch(_harmony);
                _successPatch &= Patch_TeamAIComponent.Patch(_harmony);
                _successPatch &= Patch_MissionAgentLabelView.Patch(_harmony);
                _successPatch &= Patch_MissionBoundaryCrossingHandler.Patch(_harmony);
                _successPatch &= Patch_MissionFormationMarkerVM.Patch(_harmony);
                _successPatch &= Patch_MissionOrderVM.Patch(_harmony);
                _successPatch &= Patch_CrosshairVM.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletSpectatorControl.Patch(_harmony);
                _successPatch &= Patch_ScoreboardScreenWidget.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletMainAgentEquipDropView.Patch(_harmony);
                _successPatch &= Patch_MissionGauntletMainAgentEquipmentControllerView.Patch(_harmony);
                _successPatch &= Patch_AgentHumanAILogic.Patch(_harmony);
                _successPatch &= Patch_Mission.Patch(_harmony);
                _successPatch &= Patch_Formation.Patch(_harmony);
                // Use Patch to add game menu
                WatchBattleBehavior.Patch(_harmony);

                if (!UIConfig.DoNotUseGeneratedPrefabs && RTSCameraConfig.Get().OrderUIClickable)
                {
                    UIConfig.DoNotUseGeneratedPrefabs = true;
                }
            }
            catch (Exception e)
            {
                _successPatch = false;
                MBDebug.ConsolePrint(e.ToString());
            }
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

            if (!_successPatch)
            {
                InformationManager.DisplayMessage(new InformationMessage("RTS Camera: patch failed"));
            }

            Patch_MissionGauntletSingleplayerOrderUIHandler.Patch();
            Patch_MissionGauntletCrosshair.Patch(_harmony);
            Utility.ShouldDisplayMessage = RTSCameraConfig.Get().DisplayMessage;
            Utility.PrintUsageHint();
        }

        private bool SecondInitialize()
        {
            if (!Initializer.SecondInitialize())
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
                //campaignGameStarter.AddBehavior(new WatchBattleBehavior());
                campaignGameStarter.AddBehavior(new RTSCameraSkillBehavior());
            }
        }


        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            MissionExtensionCollection.Clear();
            _harmony.UnpatchAll(_harmony.Id);
            Initializer.Clear();
        }
    }
}
