using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;

namespace RTSCamera.Logic.SubLogic
{
    public class SwitchFreeCameraLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        private ControlTroopLogic _controlTroopLogic;

        private bool _isDeploymentFinishing = false;
        private FormationClass _formationClassInDeployment;
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
        private List<FormationClass> _playerFormations;
        private float _updatePlayerFormationTime;
        private bool _hasShownFocusOnFormationHint = false;

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
            WatchBattleBehavior.WatchMode = false;
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }

        private void OnToggledOrderView(MissionPlayerToggledOrderViewEvent e)
        {
            bool showFocusOnFormationHint = false;
            if (e.IsOrderEnabled)
            {
                if (IsSpectatorCamera)
                {
                    if (_shouldIgnoreNextOrderViewOpenEvent)
                    {
                        // To keep order UI open in free camera,
                        // code is patched in a way that, if in free camera,
                        // UI will be opened instantly after closed
                        // This means that an event that UI is closed will be triggered
                        // and following an event that UI is opened.
                        // So we will wait for a tick if UI is closed,
                        // and if a false positive UI open event is triggered during this tick,
                        // we will not switch to agent camera, instead we will cancel the wait.
                        if (_config.SwitchCameraOnOrdering && !WatchBattleBehavior.WatchMode)
                        {
                            _switchToAgentNextTick = false;
                        }
                        _shouldIgnoreNextOrderViewOpenEvent = false;
                    }
                    else
                    {
                        showFocusOnFormationHint = true;
                        if (_config.SwitchCameraOnOrdering && !WatchBattleBehavior.WatchMode)
                        {
                            // The camera is already in free camera mode when ordering begins,
                            // so we skip switching camera to agent on ordering finished.
                            _skipSwitchingCameraOnOrderingFinished = true;
                        }
                    }
                }
                else
                {
                    if (_config.SwitchCameraOnOrdering && !WatchBattleBehavior.WatchMode)
                    {
                        _skipSwitchingCameraOnOrderingFinished = false;
                        SwitchToFreeCamera();
                        showFocusOnFormationHint = true;
                    }
                }
            }
            else
            {
                if (_config.SwitchCameraOnOrdering && !WatchBattleBehavior.WatchMode)
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

            if (!_hasShownFocusOnFormationHint && showFocusOnFormationHint)
            {
                _hasShownFocusOnFormationHint = true;
                var hint = GameTexts.FindText("str_rts_camera_focus_on_formation_hint");
                hint.SetTextVariable("KeyName", RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop).ToSequenceString());
                Utility.DisplayMessage(hint.ToString());
            }
        }

        public void OnTeamDeployed(Team team)
        {
            // TODO: Redundant with Patch_MissionOrderDeploymentControllerVM.Prefix_ExecuteDeployAll
            if (team == Mission.PlayerTeam)
            {
                if (WatchBattleBehavior.WatchMode && Mission.MainAgent == null)
                {
                    // Force control agent, setting controller to Player, to avoid the issue that,
                    // DeploymentMissionController.OnAgentControllerSetToPlayer may pause main agent ai, when 
                    // DeploymentMissionController.FinishDeployment set controller of main agent to Player.
                    //_controlTroopLogic.ForceControlAgent();
                    Utility.PlayerControlAgent(_controlTroopLogic.GetAgentToControl());
                    if (Mission.MainAgent != null)
                    {
                        Utility.SetIsPlayerAgentAdded(_controlTroopLogic.MissionScreen, true);
                        if (Mission.PlayerTeam.IsPlayerGeneral)
                            Utility.SetPlayerAsCommander(true);
                        team.PlayerOrderController?.SelectAllFormations();
                    }
                }
                if (_config.DefaultToFreeCamera >= DefaultToFreeCamera.DeploymentStage)
                // switch to free camera during deployment stage
                    _switchToFreeCameraNextTick = true;
            }
        }
        public void OnDeploymentFinished()
        {
            if (_config.DefaultToFreeCamera != DefaultToFreeCamera.Always && !WatchBattleBehavior.WatchMode)
            {
                _switchToAgentNextTick = true;
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

            _updatePlayerFormationTime += dt;
            if (_updatePlayerFormationTime > 0.1f && !Utility.IsPlayerDead() &&
                Mission.MainAgent.Formation != null)
            {
                _updatePlayerFormationTime = 0;
                CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
            }

            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera).IsKeyPressed(Mission.InputManager))
            {
                SwitchCamera();
                if (_config.OrderOnSwitchingCamera)
                {
                    var dataSource = Utility.GetMissionOrderVM(Mission);
                    if (dataSource != null)
                    {
                        if (IsSpectatorCamera)
                        {
                            // If order UI is already shown when switch to free camera,
                            // we will not close it when switching to agent camera.
                            if (dataSource.IsToggleOrderShown)
                                _skipClosingUIOnSwitchingCamera = true;
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
        }

        public void SwitchCamera()
        {
            if (IsSpectatorCamera)
            {
                SwitchToAgent();
            }
            else
            {
                SwitchToFreeCamera();
            }
        }

        public void OnAgentControllerChanged(Agent agent)
        {
            if (agent.Controller == Agent.ControllerType.Player || agent.Controller == Agent.ControllerType.None)
            {
                agent.SetMaximumSpeedLimit(-1, false);
                agent.MountAgent?.SetMaximumSpeedLimit(-1, false);
                //agent.StopRetreating();
                TrySetPlayerFormation();

                // If MainAgent.Controller = Controller.Player is called from DeploymentMissionController, then we will try set the player formation i then reset _isDeploymentFinishing.
                _isDeploymentFinishing = false;
                if (agent.Formation == null)
                    return;
                CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
            else if (agent == Mission.MainAgent)
            {
                //Utility.SetHasPlayerControlledTroop(agent.Formation, false);
                TrySetPlayerFormation();

                if (agent.Formation == null)
                    return;
                CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            if (oldMissionMode == MissionMode.Deployment && Mission.Mode == MissionMode.Battle)
            {
                // CurrentPlayerFormation will be changed when player agent is set to Player controller because OnMainAgentChanged will be called.
                // So we need to cache it here.
                _formationClassInDeployment = CurrentPlayerFormation;
                TrySetPlayerFormation(true);
                // OnMissionModeChange is called before the player is promoted to general formation in DeploymentMissionController.
                // So we need to still set player formation when player controller is set to Player in DeploymentMissionController later.
                _isDeploymentFinishing = true;
            }
        }

        private void TrySetPlayerFormation(bool isDeploymentFinishing = false)
        {
            // In watch mode, after deployment stage the player formation needs to be reset from General formation.
            if (WatchBattleBehavior.WatchMode)
            {
                if (_isDeploymentFinishing || isDeploymentFinishing)
                {
                    Utility.SetPlayerFormationClass(_formationClassInDeployment);
                }
            }
            else if (_config.AutoSetPlayerFormation == AutoSetPlayerFormation.Always ||
                     _config.AutoSetPlayerFormation == AutoSetPlayerFormation.DeploymentStage &&
                     (isDeploymentFinishing || _isDeploymentFinishing || Mission.Mode == MissionMode.Deployment))
                Utility.SetPlayerFormationClass((FormationClass)_config.PlayerFormation);
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent != null)
            {
                if (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Deployment)
                {
                    if (Mission.MainAgent.Formation != null)
                        CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                    if (IsSpectatorCamera || WatchBattleBehavior.WatchMode)
                    {
                        UpdateMainAgentControllerInFreeCamera();
                    }
                    else
                    {
                        if (Mission.MainAgent.Controller != Agent.ControllerType.Player)
                            _controlTroopLogic.ControlMainAgent(false);
                    }
                }
            }
            else if (IsSpectatorCamera || (_config.ControlAllyAfterDeath && !Mission.IsFastForward))
            {
                _controlTroopLogic.SetMainAgent();
            }
        }

        private void UpdateMainAgentControllerInFreeCamera()
        {
            Agent.ControllerType controllerType = _config.GetPlayerControllerInFreeCamera(Mission);
            Utilities.Utility.UpdateMainAgentControllerInFreeCamera(Mission.MainAgent, controllerType);
            Utilities.Utility.UpdateMainAgentControllerState(Mission.MainAgent, IsSpectatorCamera, controllerType);
        }

        public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == null)
            {
                return;
            }
            if (Mission.MainAgent == affectedAgent)
            {
                if (_config.ControlAllyAfterDeath || IsSpectatorCamera)
                {
                    if (Utilities.Utility.IsBattleCombat(Mission) &&
                        Mission.MainAgent.Character == CharacterObject.PlayerCharacter)
                        Utility.DisplayLocalizedText("str_rts_camera_player_dead", null, new Color(1, 0, 0));
                    // mask code in Mission.OnAgentRemoved so that formations will not be delegated to AI after player dead.
                    if (_controlTroopLogic.GetAgentToControl() != null)
                    {
                        affectedAgent.OnMainAgentWieldedItemChange = null;
                        // TODO: optimize this logic
                        bool shouldSmoothToAgent = Utility.BeforeSetMainAgent();
                        if (IsSpectatorCamera || (_config.ControlAllyAfterDeath && !Mission.IsFastForward))
                        {
                            // will there be 2 agent with player controller in the same formation
                            // if we set new main agent here?
                            _controlTroopLogic.SetMainAgent();
                        }
                        // Set smooth move again if controls another agent instantly.
                        // Otherwise MissionScreen will reset camera elevate and bearing.
                        if (Mission.MainAgent != null && Mission.MainAgent.Controller == Agent.ControllerType.Player)
                            Utility.AfterSetMainAgent(shouldSmoothToAgent, _controlTroopLogic.MissionScreen);
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
            if (WatchBattleBehavior.WatchMode)
            {
                Utility.DisplayLocalizedText("str_rts_camera_cannot_control_agent_in_command_mode");
                if (Mission.MainAgent == null)
                {
                    Utility.DisplayLocalizedText("str_rts_camera_player_dead");
                    _controlTroopLogic.SetMainAgent();
                }
                return;
            }
            IsSpectatorCamera = false;
            if (Mission.MainAgent != null)
            {
                Utility.DisplayLocalizedText("str_rts_camera_switch_to_player");
                _controlTroopLogic.ControlMainAgent();
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_player_dead");
                _controlTroopLogic.SetMainAgent();
            }

            if (Mission.MainAgent != null)
            {
                Utilities.Utility.UpdateMainAgentControllerState(Mission.MainAgent, IsSpectatorCamera,
                    _config.GetPlayerControllerInFreeCamera(Mission.Current));
            }

            MissionLibrary.Event.MissionEvent.OnToggleFreeCamera(false);
        }

        private void SwitchToFreeCamera()
        {
            if (IsSpectatorCamera)
                return;
            IsSpectatorCamera = true;
            if (!Utility.IsPlayerDead())
            {
                UpdateMainAgentControllerInFreeCamera();
            }

            MissionLibrary.Event.MissionEvent.OnToggleFreeCamera(true);
            Utility.DisplayLocalizedText("str_rts_camera_switch_to_free_camera");
        }
    }
}