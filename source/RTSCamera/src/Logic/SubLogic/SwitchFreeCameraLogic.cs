using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Patch.Fix;
using RTSCamera.Patch.TOR_fix;
using RTSCamera.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using static TaleWorlds.MountAndBlade.Agent;

namespace RTSCamera.Logic.SubLogic
{
    public class SwitchFreeCameraLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        private ControlTroopLogic _controlTroopLogic;

        // To keep order UI open in free camera,
        // code is patched in a way that, if in free camera,
        // UI will be opened instantly after closed
        // This means that after an order is issued,
        // an event that UI is closed will be triggered
        // and following an event that UI is opened.
        // the following open event is a false positive,
        // so we need to ignore it
        private bool _shouldIgnoreNextOrderViewOpenEvent = false;
        private bool _switchToFreeCameraNextTick;
        private bool _switchToAgentNextTick;
        private bool _skipSwitchingCameraOnOrderingFinished;
        private bool _skipClosingUIOnSwitchingCamera;
        public bool ShouldKeepUIOpen = false;
        private List<FormationClass> _playerFormations;
        private float _updatePlayerFormationTime;
        private bool _hasShownOrderHint = false;
        private bool _isSwitchCameraKeyPressedLastTick = false;
        private bool _shouldShowFastForwardInHideoutPromptInThisMission = false;
        public bool FastForwardHideoutNextTick = false;

        public Mission Mission => _logic.Mission;

        public List<FormationClass> PlayerFormations => _playerFormations ??= new List<FormationClass>();

        public FormationClass CurrentPlayerFormation
        {
            get => Mission.PlayerTeam?.TeamIndex < PlayerFormations.Count
                ? PlayerFormations[Mission.PlayerTeam.TeamIndex]
                : (FormationClass)_config.PlayerFormation;
            set
            {
                if (Mission.PlayerTeam?.TeamIndex < PlayerFormations.Count)
                    PlayerFormations[Mission.PlayerTeam.TeamIndex] = value;
            }
        }

        public bool IsSpectatorCamera;

        public SwitchFreeCameraLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            _controlTroopLogic = _logic.ControlTroopLogic;

            Mission.OnMainAgentChanged += OnMainAgentChanged;
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }

        public void AfterAddTeam(Team team)
        {
            PlayerFormations.Add((FormationClass)_config.PlayerFormation);
        }

        public void OnRemoveBehaviour()
        {
            Mission.OnMainAgentChanged -= OnMainAgentChanged;
            CommandBattleBehavior.CommandMode = false;
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }

        private void OnToggledOrderView(MissionPlayerToggledOrderViewEvent e)
        {
            bool showOrderHint = false;
            if (e.IsOrderEnabled)
            {
                if (_shouldIgnoreNextOrderViewOpenEvent)
                {
                    // To refresh orders during switching free camera,
                    // order ui may be closed and opened again.
                    // This means that an event that UI is closed will be triggered
                    // and following an event that UI is opened.
                    // So we will wait for a tick if UI is closed,
                    // and if a false positive UI open event is triggered during this tick,
                    // we will not switch to agent camera, instead we will cancel the wait.
                    if (_config.SwitchCameraOnOrdering && !CommandBattleBehavior.CommandMode)
                    {
                        _switchToAgentNextTick = false;
                    }
                    _shouldIgnoreNextOrderViewOpenEvent = false;
                }
                else
                {
                    if (IsSpectatorCamera)
                    {
                        showOrderHint = true;
                        if (_config.SwitchCameraOnOrdering && !CommandBattleBehavior.CommandMode)
                        {
                            // The camera is already in free camera mode when ordering begins,
                            // so we skip switching camera to agent on ordering finished.
                            _skipSwitchingCameraOnOrderingFinished = true;
                        }
                    }
                    else
                    {
                        if (_config.SwitchCameraOnOrdering && !CommandBattleBehavior.CommandMode)
                        {
                            _skipSwitchingCameraOnOrderingFinished = false;
                            SwitchToFreeCamera();
                            showOrderHint = true;
                        }
                    }
                }
            }
            else
            {
                if (_config.SwitchCameraOnOrdering && !CommandBattleBehavior.CommandMode)
                {
                    if (!_skipSwitchingCameraOnOrderingFinished)
                    {
                        _shouldIgnoreNextOrderViewOpenEvent = true;
                        _switchToAgentNextTick = true;
                    }
                    else
                    {
                        _skipSwitchingCameraOnOrderingFinished = false;
                    }
                }
            }

            if (!_hasShownOrderHint && showOrderHint)
            {
                _hasShownOrderHint = true;
                Utilities.Utility.PrintOrderHint();
            }

            if (_shouldShowFastForwardInHideoutPromptInThisMission)
            {
                _shouldShowFastForwardInHideoutPromptInThisMission = false;
                InquiryData data = new InquiryData("RTS Camera", GameTexts.FindText("str_rts_camera_fast_forward_hideout_prompt").ToString(), true, true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(),
                    () =>
                    {
                        _config.FastForwardHideout = FastForwardHideout.Always;
                        FastForwardHideoutNextTick = true;
                        _config.Serialize();
                    }, () =>
                    {
                        _config.Serialize();
                    });
                InformationManager.ShowInquiry(data, false);
            }
        }

        public void OnEarlyTeamDeployed(Team team)
        {
            if (team == Mission.PlayerTeam)
            {
                if (CommandBattleBehavior.CommandMode && Mission.MainAgent == null)
                {
                    // Force control agent, setting controller to Player, to avoid the issue that,
                    // DeploymentMissionController.OnAgentControllerSetToPlayer may pause main agent ai, when 
                    // DeploymentMissionController.FinishDeployment set controller of main agent to Player.
                    Agent agentToControl = null;
                    if (Mission.IsNavalBattle)
                    {
                        // in naval battle the first formation is player formation by default.
                        var infantryFormation = Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
                        if (infantryFormation.Captain != null)
                        {
                            agentToControl = infantryFormation.Captain;
                        }
                    }
                    if (agentToControl == null)
                    {
                        agentToControl = _controlTroopLogic.GetAgentToControl();
                    }
                    Utility.PlayerControlAgent(agentToControl);
                    if (Mission.MainAgent != null)
                    {
                        Utility.SetIsPlayerAgentAdded(_controlTroopLogic.MissionScreen, true);
                        if (Mission.PlayerTeam.IsPlayerGeneral)
                        {
                            Utility.SetPlayerAsCommander(true);
                            Mission.MainAgent?.SetCanLeadFormationsRemotely(true);
                            Mission.PlayerTeam.GeneralAgent = Mission.MainAgent;
                        }
                        team.PlayerOrderController?.SelectAllFormations();
                    }
                }
                if (CommandBattleBehavior.CommandMode || _config.AssignPlayerFormation < AssignPlayerFormation.Overwrite)
                {
                    if (Mission.MainAgent?.Formation != null)
                        CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                }
            }
        }

        public void OnTeamDeployed(Team team)
        {
            try
            {
                // TODO: Redundant with Patch_MissionOrderDeploymentControllerVM.Prefix_ExecuteDeployAll
                if (team == Mission.PlayerTeam)
                {
                    //if (CommandBattleBehavior.CommandMode && Mission.MainAgent == null)
                    //{
                    //    // Force control agent, setting controller to Player, to avoid the issue that,
                    //    // DeploymentMissionController.OnAgentControllerSetToPlayer may pause main agent ai, when 
                    //    // DeploymentMissionController.FinishDeployment set controller of main agent to Player.
                    //    Utility.PlayerControlAgent(_controlTroopLogic.GetAgentToControl());
                    //    if (Mission.MainAgent != null)
                    //    {
                    //        Utility.SetIsPlayerAgentAdded(_controlTroopLogic.MissionScreen, true);
                    //        if (Mission.PlayerTeam.IsPlayerGeneral)
                    //        {
                    //            Utility.SetPlayerAsCommander(true);
                    //            Mission.MainAgent?.SetCanLeadFormationsRemotely(true);
                    //            Mission.PlayerTeam.GeneralAgent = Mission.MainAgent;
                    //        }
                    //        team.PlayerOrderController?.SelectAllFormations();
                    //    }
                    //}
                    if (CommandBattleBehavior.CommandMode || _config.DefaultToFreeCamera >= DefaultToFreeCamera.DeploymentStage)
                    {
                        // switch to free camera during deployment stage
                        _switchToFreeCameraNextTick = true;
                    }
                    if ((CommandBattleBehavior.CommandMode || _config.AssignPlayerFormation < AssignPlayerFormation.Overwrite) && MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
                    {
                        if (Mission.MainAgent?.Formation != null)
                            CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                    }
                    if (_config.AssignPlayerFormation == AssignPlayerFormation.Overwrite)
                    {
                        // Set player formation when team is deployed.
                        TrySetPlayerFormation();
                    }
                }
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }
        }

        public void OnEarlyDeploymentFinished()
        {
            try
            {
                // When player joins as reinforcement, at this point the player is already added to general formation
                if ((CommandBattleBehavior.CommandMode || _config.AssignPlayerFormation < AssignPlayerFormation.Overwrite) && MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
                {
                    if (Mission.MainAgent?.Formation != null)
                        CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                }
            }
            catch(Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }
        }


        public void OnDeploymentFinished()
        {
            if (_config.DefaultToFreeCamera != DefaultToFreeCamera.Always && !CommandBattleBehavior.CommandMode)
            {
                _switchToAgentNextTick = true;
                // If not deployment is required, we need to set _switchToFreeCameraNextTick to false to prevent camera set to free mode.
                _switchToFreeCameraNextTick = false;
            }
            else
            {
                ShouldKeepUIOpen = true;
            }
        }

        public void OnMissionTick(float dt)
        {
            if (Mission.IsInPhotoMode)
                return;
            if (_shouldIgnoreNextOrderViewOpenEvent)
            {
                _shouldIgnoreNextOrderViewOpenEvent = false;
            }
            if (_switchToFreeCameraNextTick)
            {
                _switchToFreeCameraNextTick = false;
                SwitchToFreeCamera();
            }
            else if (_switchToAgentNextTick)
            {
                _switchToAgentNextTick = false;
                SwitchToAgent();
            }

            if (FastForwardHideoutNextTick)
            {
                FastForwardHideoutNextTick = false;

                SwitchToFreeCamera();
                Utilities.Utility.FastForwardInHideout(Mission);
            }

            _updatePlayerFormationTime += dt;
            if (_updatePlayerFormationTime > 0.1f && !Utility.IsPlayerDead() &&
                Mission.MainAgent.Formation != null)
            {
                _updatePlayerFormationTime = 0;
                CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
            }

            // In fastforward mode, the key may be triggered in 2 ticks
            if (_isSwitchCameraKeyPressedLastTick)
            {
                _isSwitchCameraKeyPressedLastTick = false;
            }
            // some keys are not supported in Mission.InputManager. Pass null to use InputSystem.Input directly.
            else if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera).IsKeyPressed())
            {
                _isSwitchCameraKeyPressedLastTick = true;
                SwitchCamera(true);
            }
        }

        public void SwitchCamera(bool toggleOrderUI = false)
        {
            if (IsSpectatorCamera)
            {
                if (CommandBattleBehavior.CommandMode)
                {
                    Utility.DisplayLocalizedText("str_rts_camera_cannot_control_agent_in_command_mode");
                    if (Mission.MainAgent == null)
                    {
                        Utility.DisplayLocalizedText("str_rts_camera_player_dead");
                        _controlTroopLogic.SetMainAgent();
                    }
                    return;
                }
                SwitchToAgent();
            }
            else
            {
                SwitchToFreeCamera();
            }
            if (toggleOrderUI)
            {
                ToggleOrderUIOnSwitchingCamera();
            }
        }

        private void ToggleOrderUIOnSwitchingCamera()
        {
            if (Mission.Mode == MissionMode.Deployment)
                return;
            var dataSource = Utility.GetMissionOrderVM(Mission);
            if (dataSource != null)
            {
                if (IsSpectatorCamera)
                {
                    if (dataSource.IsToggleOrderShown)
                    {
                        Utilities.Utility.PrintOrderHint();
                        _hasShownOrderHint = true;
                    }
                    // Should keep UI open if we actively switch to free camera mode by pressing hotkey.
                    ShouldKeepUIOpen = true;
                }
                else
                {
                    _hasShownOrderHint = false;
                    ShouldKeepUIOpen = false;
                }

                if (_config.OrderOnSwitchingCamera)
                {

                    if (IsSpectatorCamera)
                    {
                        // If order UI is already shown when switch to free camera,
                        // we will not close it when switching to agent camera.
                        if (dataSource.IsToggleOrderShown)
                        {
                            _skipClosingUIOnSwitchingCamera = true;
                        }
                        else
                        {
                            _skipClosingUIOnSwitchingCamera = false;
                            dataSource.OpenToggleOrder(false);
                        }
                    }
                    else
                    {
                        if (!_skipClosingUIOnSwitchingCamera)
                        {
                            dataSource.TryCloseToggleOrder(true);
                        }
                        else
                        {
                            _skipClosingUIOnSwitchingCamera = false;
                        }
                    }
                }
            }
        }

        public void OnAgentControllerChanged(Agent agent)
        {
            if (agent.Controller == AgentControllerType.Player || agent.Controller == AgentControllerType.None)
            {
                agent.SetMaximumSpeedLimit(-1, false);
                agent.MountAgent?.SetMaximumSpeedLimit(-1, false);
                if (agent.WalkMode)
                {
                    agent.EventControlFlags |= EventControlFlag.Run;
                    // required to fix the issue that the agent may still walk after switching to player controller, after deployment.
                    agent.EventControlFlags &= ~EventControlFlag.Walk;
                }
                //agent.StopRetreating();
                TrySetPlayerFormation();

                if (agent.Formation == null)
                    return;
                //CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
            else if (agent == Mission.MainAgent)
            {
                //Utility.SetHasPlayerControlledTroop(agent.Formation, false);
                //TrySetPlayerFormation();

                //if (agent.Formation == null)
                //    return;
                //CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            // Current call chain during battle start up is:
            // OnMissionModeChange(StartUp) with Mission.Mode == Battle
            // OnMissionModeChange(Battle) with Mission.Mode == Deployment
            // OnMainAgentChanged triggered by `MainAgent.Controller = Controller.Player` in DeploymentMissionController
            // followed by OnAgentControllerChanged
            // OnAgentControllerChanged triggered by `MainAgent.Controller = Controller.AI` in DeploymentMissionController
            // 
            // If has deployment stage:
            // OnTeamDeployed is called
            // Player deploy troops and click ready ...
            // Player is assigned to general formation in GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished
            // OnDeploymentFinished is called
            // else:
            // Player is assigned to general formation if player is general in GeneralsAndCaptainsAssignmentLogic.OnTeamDeployed
            // OnTeamDeployed is called
            // In the same tick, Player is added to general formation if player is not general in GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished
            // In the same tick, OnDeploymentFinished is called.
            // 
            // OnMainAgentChanged triggered by `MainAgent.Controller = Controller.Player` in DeploymentMissionController
            // followed by OnAgentControllerChanged
            // OnMissionModeChange(Deployment) with Mission.Mode == Deployment
            if (oldMissionMode == MissionMode.Deployment && Mission.Mode == MissionMode.Battle)
            {
                TrySetPlayerFormation(true);
            }
            //if (MissionState.Current?.MissionName == "HideoutBattle")
            //{
            //    if (oldMissionMode == MissionMode.Battle && Mission.Mode == MissionMode.Stealth)
            //    {
            //        if (!_config.FastForwardHideoutPrompted)
            //        {
            //            _config.FastForwardHideoutPrompted = true;
            //            if (_config.FastForwardHideout == FastForwardHideout.Never)
            //            {
            //                _shouldShowFastForwardInHideoutPromptInThisMission = true;
            //            }
            //        }
            //        if (_config.FastForwardHideout >= FastForwardHideout.UntilBossFight)
            //            FastForwardHideoutNextTick = true;
            //    }
            //    if (oldMissionMode == MissionMode.Stealth && (Mission.Mode == MissionMode.CutScene || Mission.Mode == MissionMode.Conversation))
            //    {
            //        // do not fast forward in conversation.
            //        Mission.SetFastForwardingFromUI(false);
            //        _logic.MissionSpeedLogic.SetSlowMotionMode(false);
            //        if (IsSpectatorCamera)
            //        {
            //            SwitchToAgent();
            //        }
            //    }
            //}
        }

        private void TrySetPlayerFormation(bool isDeploymentFinishing = false)
        {
            bool isDeployment = isDeploymentFinishing || Mission.Mode == MissionMode.Deployment;
            if (!isDeployment)
                return;
            // skip setting formation in naval battle.
            if (Mission.IsNavalBattle)
                return;
            if (Mission.MainAgent?.Formation?.FormationIndex == null)
                return;

            var formationToSet = Mission.MainAgent.Formation.FormationIndex;
            // When deployment finishes, the player formation needs to be reset from General formation if AssignPlayerFormation is set to Default.
            // In watch mode, recover to previous formation instead of configured formation
            if ((CommandBattleBehavior.CommandMode || _config.AssignPlayerFormation == AssignPlayerFormation.Default))
            {
                formationToSet = CurrentPlayerFormation;
            }
            if (!CommandBattleBehavior.CommandMode && _config.AssignPlayerFormation == AssignPlayerFormation.Overwrite)
            {
                formationToSet = _config.PlayerFormation;
            }

            // If has bodyguard formation and the general formation only contains player, we do not remove player from General formation to avoid the bodyguard formation being charging alone.
            if (Mission.MainAgent?.Formation?.FormationIndex == FormationClass.General && Mission.PlayerTeam?.BodyGuardFormation != null
                && Mission.MainAgent?.Formation?.CountOfUnits == 1)
                return;
            Utility.SetPlayerFormationClass(formationToSet);
        }

        private void OnMainAgentChanged(Agent oldAgent)
        {
            if (Mission.MainAgent != null)
            {
                if (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Deployment || Mission.Mode == MissionMode.Stealth || Mission.Mode == MissionMode.Tournament)
                {
                    //if (Mission.MainAgent.Formation != null)
                    //    CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                    if (IsSpectatorCamera || CommandBattleBehavior.CommandMode)
                    {
                        UpdateMainAgentControllerInFreeCamera();
                    }
                    else
                    {
                        if (Mission.MainAgent.Controller != AgentControllerType.Player)
                            _controlTroopLogic.ControlMainAgent(false);
                    }
                    // Fix crash in The Old Realms.
                    Patch_CareerHelper.OnMainAgentChanged();
                }
            }
            else if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || (_config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always && !Mission.IsFastForward))
            {
                _controlTroopLogic.SetMainAgent();
            }
        }

        private void UpdateMainAgentControllerInFreeCamera()
        {
            // Avoid update if switch to agent next tick.
            // For example, when DeploymentMissionController.FinishDeployment is called and MainAgent.Controller is set to Player.
            if (_switchToAgentNextTick)
                return;
            AgentControllerType controllerType = GetPlayerControllerInFreeCamera(Mission);
            Utilities.Utility.UpdateMainAgentControllerInFreeCamera(Mission.MainAgent, controllerType);
            Utilities.Utility.UpdateMainAgentControllerState(Mission.MainAgent, IsSpectatorCamera, controllerType);
        }

        private AgentControllerType GetPlayerControllerInFreeCamera(Mission mission)
        {
            if (CommandBattleBehavior.CommandMode || mission?.Mode == MissionMode.Deployment)
                return AgentControllerType.AI;
            return (AgentControllerType)_config.PlayerControllerInFreeCamera;
        }

        public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == null)
            {
                return;
            }
            if (Mission.MainAgent == affectedAgent)
            {
                if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || _config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always)
                {
                    if (Utilities.Utility.IsBattleCombat(Mission) &&
                        Mission.MainAgent.Character == CharacterObject.PlayerCharacter)
                        Utility.DisplayLocalizedText("str_rts_camera_player_dead", null, new Color(1, 0, 0));
                    // mask code in Mission.OnAgentRemoved so that formations will not be delegated to AI after player dead.
                    var agent = _controlTroopLogic.GetAgentToControl();
                    if (agent != null)
                    {
                        affectedAgent.OnMainAgentWieldedItemChange = null;
                        // TODO: optimize this logic
                        bool shouldSmoothToAgent = Utility.BeforeSetMainAgent(agent);
                        if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || (_config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always && !Mission.IsFastForward))
                        {
                            // will there be 2 agent with player controller in the same formation
                            // if we set new main agent here?
                            // yes so we need to resolve it in Patch_Formation.
                            _controlTroopLogic.SetToMainAgent(agent);
                        }
                        // Set smooth move again if controls another agent instantly.
                        // Otherwise MissionScreen will reset camera elevate and bearing.
                        if (Mission.MainAgent != null && Mission.MainAgent.Controller == AgentControllerType.Player)
                            Utility.AfterSetMainAgent(shouldSmoothToAgent, _controlTroopLogic.MissionScreen, _config.FollowFaceDirection >= FollowFaceDirection.ControlNewTroopOnly);
                        // Restore the variables to initial state
                        else if (shouldSmoothToAgent)
                        {
                            Utility.ShouldSmoothMoveToAgent = true;
                            Utility.SetIsPlayerAgentAdded(_controlTroopLogic.MissionScreen, false);
                        }
                    }
                }
                else if (!Utility.IsTeamValid(Mission.PlayerTeam) || Mission.PlayerTeam.ActiveAgents.Count > 0)
                {
                    GameTexts.SetVariable("KeyName", RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop).ToSequenceString());
                    Utility.DisplayLocalizedText("str_rts_camera_control_troop_hint");
                }
            }

            if (Utility.IsTeamValid(affectedAgent.Team))
            {
                if (affectedAgent.Team.PlayerOrderController.Owner == affectedAgent)
                {
                    affectedAgent.Team.PlayerOrderController.Owner = null;
                }
            }
        }

        private void SwitchToAgent()
        {
            if (!IsSpectatorCamera)
                return;
            IsSpectatorCamera = false;
            if (Mission.MainAgent != null)
            {
                Utility.DisplayLocalizedText("str_rts_camera_switch_to_player");
                _controlTroopLogic.ControlMainAgent();
            }
            //else
            //{
            //    Utility.DisplayLocalizedText("str_rts_camera_player_dead");
            //    _controlTroopLogic.SetMainAgent();
            //}

            if (Mission.MainAgent != null && Mission.Mode != MissionMode.Deployment)
            {
                Utilities.Utility.UpdateMainAgentControllerState(Mission.MainAgent, IsSpectatorCamera,
                    GetPlayerControllerInFreeCamera(Mission.Current));
            }

            MissionLibrary.Event.MissionEvent.OnToggleFreeCamera(false);
            if (Mission.IsOrderMenuOpen && Mission.IsNavalBattle)
            {
                RefreshOrders();
            }
        }

        private void SwitchToFreeCamera()
        {
            if (IsSpectatorCamera)
                return;
            IsSpectatorCamera = true;
            if (!Utility.IsPlayerDead() && Mission.Mode != MissionMode.Deployment)
            {
                UpdateMainAgentControllerInFreeCamera();
            }
            // When main agent is null and player press E to lock to agent, we should not set the main agent to allow pressing E again to show inquiry and control the locked agent.
            else if (_config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always || _config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.FreeCamera && Mission?.GetMissionBehavior<FlyCameraMissionView>()?.LockToAgent != true)
            {
                _controlTroopLogic.SetMainAgent();
            }

            MissionLibrary.Event.MissionEvent.OnToggleFreeCamera(true);
            Utility.DisplayLocalizedText("str_rts_camera_switch_to_free_camera");
            if (Mission.IsOrderMenuOpen && Mission.IsNavalBattle)
            {
                RefreshOrders();
            }
        }

        private void RefreshOrders()
        {
            var missionOrderVM = Utility.GetMissionOrderVM(Mission);
            if (missionOrderVM != null)
            {
                // allow it to be actually closed.
                Patch_MissionOrderVM.AllowClosingOrderUI = true;
                missionOrderVM.TryCloseToggleOrder();
                // avoid switching to free camera automatically when opening order UI.
                _shouldIgnoreNextOrderViewOpenEvent = true;
                missionOrderVM.OpenToggleOrder(false, false);
            }
        }
    }
}