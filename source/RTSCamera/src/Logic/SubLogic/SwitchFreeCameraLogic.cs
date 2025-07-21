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
using TaleWorlds.MountAndBlade.View;

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
        private List<FormationClass> _playerFormations;
        private float _updatePlayerFormationTime;
        private bool _hasShownOrderHint = false;

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
            bool showOrderHint = false;
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
                        showOrderHint = true;
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
                        showOrderHint = true;
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

            if (!_hasShownOrderHint && showOrderHint)
            {
                _hasShownOrderHint = true;
                Utilities.Utility.PrintOrderHint();
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
                if (WatchBattleBehavior.WatchMode || _config.DefaultToFreeCamera >= DefaultToFreeCamera.DeploymentStage)
                    // switch to free camera during deployment stage
                    _switchToFreeCameraNextTick = true;
                if (!WatchBattleBehavior.WatchMode && _config.AutoSetPlayerFormation >= AutoSetPlayerFormation.DeploymentStage)
                {
                    // Set player formation when team is deployed.
                    TrySetPlayerFormation();
                }
            }
        }   
        public void OnDeploymentFinished()
        {
            if (_config.DefaultToFreeCamera != DefaultToFreeCamera.Always && !WatchBattleBehavior.WatchMode)
            {
                _switchToAgentNextTick = true;
                // If not deployment is required, we need to set _switchToFreeCameraNextTick to false to prevent camera set to free mode.
                _switchToFreeCameraNextTick = false;
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
                ToggleOrderUIOnSwitchingCamera();
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
                }
                else
                {
                    _hasShownOrderHint = false;
                }

                if (_config.OrderOnSwitchingCamera)
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

        public void OnAgentControllerChanged(Agent agent)
        {
            if (agent.Controller == Agent.ControllerType.Player || agent.Controller == Agent.ControllerType.None)
            {
                agent.SetMaximumSpeedLimit(-1, false);
                agent.MountAgent?.SetMaximumSpeedLimit(-1, false);
                //agent.StopRetreating();
                TrySetPlayerFormation();

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
            // Current call chain during battle start up is:
            // OnMissionModeChange(StartUp) with Mission.Mode == Battle
            // OnMissionModeChange(Battle) with Mission.Mode == Deployment
            // OnMainAgentChanged triggered by `MainAgent.Controller = Controller.Player` in DeploymentMissionController
            // followed by OnAgentControllerChanged
            // OnAgentControllerChanged triggered by `MainAgent.Controller = Controller.AI` in DeploymentMissionController
            // OnTeamDeployed
            // Player deploy troops and click ready ...
            // Player may be set to general formation by GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished
            // OnDeploymentFinished
            // OnMainAgentChanged triggered by `MainAgent.Controller = Controller.Player` in DeploymentMissionController
            // followed by OnAgentControllerChanged
            // OnMissionModeChange(Deployment) with Mission.Mode == Deployment
            if (oldMissionMode == MissionMode.Deployment && Mission.Mode == MissionMode.Battle)
            {
                TrySetPlayerFormation(true);
            }
        }

        private void TrySetPlayerFormation(bool isDeploymentFinishing = false)
        {
            if (_config.AutoSetPlayerFormation == AutoSetPlayerFormation.Never)
                return;

            var formationToSet = CurrentPlayerFormation;
            // When deployment finishes, the player formation needs to be reset from General formation.
            if (isDeploymentFinishing || Mission.Mode == MissionMode.Deployment)
                formationToSet = _config.PlayerFormation;
            if (_config.AutoSetPlayerFormation == AutoSetPlayerFormation.Always ||
                (isDeploymentFinishing || Mission.Mode == MissionMode.Deployment))
                Utility.SetPlayerFormationClass(formationToSet);
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
            else if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || (_config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always && !Mission.IsFastForward))
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
                if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || _config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always)
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
                        if (IsSpectatorCamera && _config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera || (_config.TimingOfControlAllyAfterDeath == ControlAllyAfterDeathTiming.Always && !Mission.IsFastForward))
                        {
                            // will there be 2 agent with player controller in the same formation
                            // if we set new main agent here?
                            // yes so we need to resolve it in Patch_Formation.
                            _controlTroopLogic.SetMainAgent();
                        }
                        // Set smooth move again if controls another agent instantly.
                        // Otherwise MissionScreen will reset camera elevate and bearing.
                        if (Mission.MainAgent != null && Mission.MainAgent.Controller == Agent.ControllerType.Player)
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
            //else
            //{
            //    Utility.DisplayLocalizedText("str_rts_camera_player_dead");
            //    _controlTroopLogic.SetMainAgent();
            //}

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
            else if (_config.TimingOfControlAllyAfterDeath >= ControlAllyAfterDeathTiming.FreeCamera)
            {
                _controlTroopLogic.SetMainAgent();
            }

            MissionLibrary.Event.MissionEvent.OnToggleFreeCamera(true);
            Utility.DisplayLocalizedText("str_rts_camera_switch_to_free_camera");
        }
    }
}