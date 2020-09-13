using RTSCamera.QuerySystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera
{
    public class SwitchFreeCameraLogic : MissionLogic
    {
        private readonly RTSCameraConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        private ControlTroopLogic _controlTroopLogic;
        private FlyCameraMissionView _flyCameraMissionView;

        private bool _isFirstTimeMainAgentChanged = true;
        private bool _switchToFreeCameraAfter100ms = false;
        private float _timer;
        private List<FormationClass> _playerFormations;

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

        public bool isSpectatorCamera = false;

        public event Action<bool> ToggleFreeCamera;

        public SwitchFreeCameraLogic(RTSCameraConfig config)
        {
            _config = config;
        }

        public override void OnCreated()
        {
            base.OnCreated();

            QueryDataStore.EnsureInitialized();
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _controlTroopLogic = Mission.GetMissionBehaviour<ControlTroopLogic>();
            _flyCameraMissionView = Mission.GetMissionBehaviour<FlyCameraMissionView>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void AfterAddTeam(Team team)
        {
            base.AfterAddTeam(team);

            PlayerFormations.Add((FormationClass)_config.PlayerFormation);
            QueryDataStore.AddTeam(team);
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            this.Mission.OnMainAgentChanged -= OnMainAgentChanged;
            QueryDataStore.Clear();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_switchToFreeCameraAfter100ms)
            {
                _timer += dt;
                if (_timer > 0.1)
                {
                    _switchToFreeCameraAfter100ms = false;
                    _timer = 0;
                    SwitchToFreeCamera();
                }
            }

            if (this.Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.FreeCamera)))
            {
                this.SwitchCamera();
            }
        }

        public void SwitchCamera()
        {
            if (isSpectatorCamera)
            {
                SwitchToAgent();
            }
            else
            {
                SwitchToFreeCamera();
            }
        }

        public void ForceSwitchToAgent(Agent agent)
        {
            // temporarily mask code in this.OnMainAgentChanged that would set main agent's controller to AI.
            isSpectatorCamera = false;
            Mission.MainAgent = agent;
            SwitchToAgent();
        }

        protected override void OnAgentControllerChanged(Agent agent)
        {
            base.OnAgentControllerChanged(agent);

            if (agent.Controller == Agent.ControllerType.Player)
            {
                agent.SetMaximumSpeedLimit(-1, true);
                agent.DisableScriptedMovement();
                agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectMoving; //agent.AIMoveToGameObjectDisable();
                agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectUsing;  // agent.AIUseGameObjectEnable(false);
                if (_config.AlwaysSetPlayerFormation)
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
                if (agent.Formation == null)
                    return;
                CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
            else if (agent == Mission.MainAgent)
            {
                if (_config.AlwaysSetPlayerFormation)
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
                // the game may crash if team has ai, no formation has agents and there are agents controlled by AI.
                else if (agent.Team != null && agent.Team.HasTeamAi && agent.Formation == null)
                    Utility.SetPlayerFormation(CurrentPlayerFormation);
                if (agent.Formation == null)
                    return;
                CurrentPlayerFormation = agent.Formation.FormationIndex;
            }
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent != null)
            {
                if ((Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Deployment))
                {
                    if (_isFirstTimeMainAgentChanged)
                    {
                        // try to switch to free camera by default.
                        _isFirstTimeMainAgentChanged = false;
                        if (_config.UseFreeCameraByDefault)
                        {
                            _switchToFreeCameraAfter100ms = true;
                            _timer = 0;
                        }
                    }
                    if (Mission.MainAgent.Formation != null)
                        CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                    if (isSpectatorCamera)
                    {
                        EnsureMainAgentControlledByAI();
                    }
                    else
                    {
                        _controlTroopLogic.ControlMainAgent(false, false);
                    }
                }
            }
            else if (isSpectatorCamera)
            {
                DoNotDisturbRTS();
            }
            else if (_config.ControlAllyAfterDeath)
            {
                _controlTroopLogic.SetMainAgent();
            }
        }

        private void EnsureMainAgentControlledByAI()
        {
            Mission.MainAgent.Controller = Agent.ControllerType.AI;
            Mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
        }

        private void DoNotDisturbRTS()
        {
            Utility.DisplayLocalizedText("str_rts_camera_player_dead", null, new Color(1, 0, 0));
            _controlTroopLogic.SetMainAgent();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (Mission.MainAgent == affectedAgent && (_config.ControlAllyAfterDeath || isSpectatorCamera))
            {
                // mask code in Mission.OnAgentRemoved so that formations will not be delegated to AI after player dead.
                affectedAgent.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)null;
                Mission.MainAgent = null;
                // Set smooth move again if controls another agent instantly.
                // Otherwise MissionScreen will reset camera elevate and bearing.
                if (!isSpectatorCamera && Mission.MainAgent != null)
                {
                    if (ScreenManager.TopScreen is MissionScreen missionScreen)
                        Utility.SmoothMoveToAgent(missionScreen);
                }
            }
        }

        private void SwitchToAgent()
        {
            isSpectatorCamera = false;
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
            ToggleFreeCamera?.Invoke(false);
        }

        private void SwitchToFreeCamera()
        {
            isSpectatorCamera = true;
            if (Mission.MainAgent != null)
            {
                Utility.AIControlMainAgent(!_config.PreventPlayerFighting);
            }

            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_rts_camera_switch_to_free_camera");
        }
    }
}