using HarmonyLib;
using MissionLibrary.Extension;
using MissionSharedLibrary;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Patch;
using RTSCamera.Patch.Fix;
using SandBox;
using SandBox.Source.Towns;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Missions.SiegeWeapon;
using TaleWorlds.MountAndBlade.View.Screen;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera
{
    public class RTSCameraSubModule : MBSubModuleBase
    {
        public const string ModuleId = "RTSCamera";
        public const string OldModuleId = "EnhancedMission";

        private readonly Harmony _harmony = new Harmony("RTSCameraPatch");
        private bool _successPatch;
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                Initializer.Initialize();
                RTSCameraGameKeyCategory.Initialize();
                RTSCameraExtension.Clear();
                Module.CurrentModule.GlobalTextManager.LoadGameTexts(
                    BasePath.Name + "Modules/RTSCamera/ModuleData/module_strings.xml");
                Module.CurrentModule.GlobalTextManager.LoadGameTexts(
                    BasePath.Name + "Modules/RTSCamera/ModuleData/MissionLibrary.xml");

                _successPatch = true;

                _harmony.Patch(
                    typeof(Formation).GetMethod("LeaveDetachment", BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Formation).GetMethod("LeaveDetachment_Prefix",
                            BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    typeof(RangedSiegeWeaponView).GetMethod("HandleUserInput",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_RangedSiegeWeaponView).GetMethod("HandleUserInput_Prefix",
                            BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    typeof(CommonVillagersCampaignBehavior).GetMethod("CheckIfConversationAgentIsEscortingThePlayer",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_CommonVillagersCampaignBehavior).GetMethod(
                        "CheckIfConversationAgentIsEscortingThePlayer_Prefix",
                        BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    typeof(ArenaPracticeFightMissionController).GetMethod("StartPractice",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_ArenaPracticeFightMissionController).GetMethod("StartPractice_Prefix",
                            BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    typeof(MissionAgentLabelView).GetMethod("IsAllyInAllyTeam",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionAgentLabelView).GetMethod("IsAllyInAllyTeam_Prefix",
                        BindingFlags.Static | BindingFlags.Public)));
                _harmony.Patch(
                    typeof(MissionBoundaryCrossingHandler).GetMethod("TickForMainAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionBoundaryCrossingHandler).GetMethod("TickForMainAgent_Prefix",
                            BindingFlags.Static | BindingFlags.Public)));

                var missionListenerOnMissionModeChange = typeof(IMissionListener).GetMethod("OnMissionModeChange", BindingFlags.Instance | BindingFlags.Public);

                var mapping = typeof(MissionScreen).GetInterfaceMap(missionListenerOnMissionModeChange.DeclaringType);
                var index = Array.IndexOf(mapping.InterfaceMethods, missionListenerOnMissionModeChange);
                _harmony.Patch(
                     mapping.TargetMethods[index],
                    prefix: new HarmonyMethod(typeof(Patch_MissionScreen).GetMethod("OnMissionModeChange_Prefix",
                        BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                _successPatch = false;
                MBDebug.ConsolePrint(e.ToString());
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!_successPatch)
            {
                InformationManager.DisplayMessage(new InformationMessage("RTS Camera: patch failed"));
            }

            MissionSharedLibrary.Utility.ShouldDisplayMessage = RTSCameraConfig.Get().DisplayMessage; 
            Utility.PrintUsageHint();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            game.GameTextManager.LoadGameTexts(BasePath.Name + "Modules/RTSCamera/ModuleData/module_strings.xml");
            game.GameTextManager.LoadGameTexts(BasePath.Name + "Modules/RTSCamera/ModuleData/MissionLibrary.xml");
            AddCampaignBehavior(gameStarterObject);
        }

        private void AddCampaignBehavior(object gameStarter)
        {
            if (gameStarter is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new WatchBattleBehavior());
            }
        }
        

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            RTSCameraExtension.Clear();
            MissionExtensionCollection.Clear();
            _harmony.UnpatchAll(_harmony.Id);
            Initializer.Clear();
        }
    }
}
