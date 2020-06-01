using System;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class SwitchFreeCameraLogic : MissionLogic
    {
        private readonly RTSCameraConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        private ControlTroopLogic _controlTroopLogic;

        private bool _isFirstTimeMainAgentChanged = true;
        private bool _switchToFreeCameraNextTick = false;
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

        public override void EarlyStart()
        {
            base.EarlyStart();

            _controlTroopLogic = Mission.GetMissionBehaviour<ControlTroopLogic>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void AfterAddTeam(Team team)
        {
            base.AfterAddTeam(team);

            PlayerFormations.Add((FormationClass)_config.PlayerFormation);
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            this.Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_switchToFreeCameraNextTick)
            {
                _switchToFreeCameraNextTick = false;
                SwitchToFreeCamera();
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

        protected override void OnAgentControllerChanged(Agent agent)
        {
            base.OnAgentControllerChanged(agent);

            if (agent.Controller == Agent.ControllerType.Player)
            {
                agent.SetMaximumSpeedLimit(-1, true);
                agent.DisableScriptedMovement();
                agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectMoving; //agent.AIMoveToGameObjectDisable();
                agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectUsing;  // agent.AIUseGameObjectEnable(false);
                if (agent.Formation == null)
                    return;
                CurrentPlayerFormation = agent.Formation.FormationIndex;
                agent.Formation = null;
            }
            else if (agent == Mission.MainAgent)
            {
                // the game may crash if no formation has agents and there are agents controlled by AI.
                Utility.SetPlayerFormation(CurrentPlayerFormation);
            }
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent != null)
            {
                if (_isFirstTimeMainAgentChanged && (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Deployment))
                {
                    // try to switch to free camera by default.
                    _isFirstTimeMainAgentChanged = false;
                    if (_config.UseFreeCameraByDefault)
                    {
                        _switchToFreeCameraNextTick = true;
                    }
                }
                else
                {
                    if (Mission.MainAgent.Formation != null)
                        CurrentPlayerFormation = Mission.MainAgent.Formation.FormationIndex;
                    if (isSpectatorCamera)
                    {
                        EnsureMainAgentControlledByAI();
                    }
                }
            }
            else if (isSpectatorCamera)
            {
                DoNotDisturbRTS();
            }
        }

        private void EnsureMainAgentControlledByAI()
        {
            Mission.MainAgent.Controller = Agent.ControllerType.AI;
            Mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
        }

        private void DoNotDisturbRTS()
        {
            Utility.DisplayLocalizedText("str_em_player_dead", null, new Color(1, 0, 0));
            _controlTroopLogic.ControlTroop();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (Mission.MainAgent == affectedAgent && (_config.ControlAlliesAfterDeath || isSpectatorCamera))
            {
                // mask code in Mission.OnAgentRemoved so that formations will not be delegated to AI after player dead.
                affectedAgent.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)null;
                Mission.MainAgent = null;
            }
        }

        private void SwitchToAgent()
        {
            isSpectatorCamera = false;
            if (Mission.MainAgent != null)
            {
                Utility.DisplayLocalizedText("str_em_switch_to_player");
                Mission.MainAgent.Controller = Agent.ControllerType.Player;
            }
            else
            {
                Utility.DisplayLocalizedText("str_em_player_dead");
                _controlTroopLogic.ControlTroop();
            }
            ToggleFreeCamera?.Invoke(false);
        }

        private void SwitchToFreeCamera()
        {
            isSpectatorCamera = true;
            if (Mission.MainAgent != null)
            {
                Utility.AIControlMainAgent();
            }

            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_em_switch_to_free_camera");
        }
    }
}