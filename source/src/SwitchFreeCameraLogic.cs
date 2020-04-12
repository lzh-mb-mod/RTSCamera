using System;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    class SwitchFreeCameraLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        private ControlTroopAfterPlayerDeadLogic _controlTroopAfterPlayerDeadLogic;
        public bool isSpectatorCamera = false;

        private bool _isFirstTimeMainAgentChanged = true;
        private bool _switchToFreeCameraNextTurn = false;

        public event Action<bool> ToggleFreeCamera;

        public SwitchFreeCameraLogic(EnhancedMissionConfig config)
        {
            _config = config;
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _controlTroopAfterPlayerDeadLogic = Mission.GetMissionBehaviour<ControlTroopAfterPlayerDeadLogic>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void HandleOnCloseMission()
        {
            base.HandleOnCloseMission();

            this.Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_switchToFreeCameraNextTurn)
            {
                _switchToFreeCameraNextTurn = false;
                SwitchToFreeCamera();
            }
            else if (this.Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.FreeCamera)))
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

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isFirstTimeMainAgentChanged && Mission.MainAgent != null && (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Deployment))
            {
                _isFirstTimeMainAgentChanged = false;
                if (_config.UseFreeCameraByDefault)
                    _switchToFreeCameraNextTurn = true;
            }
            else if (isSpectatorCamera)
            {
                DoNotDisturbRTS();
            }
        }

        private void DoNotDisturbRTS()
        {
            if (Mission.MainAgent == null)
            {
                Utility.DisplayLocalizedText("str_player_dead", null, new Color(1, 0, 0));
                if (_controlTroopAfterPlayerDeadLogic.ControlTroopAfterDead() && Mission.MainAgent != null)
                {
                    Mission.MainAgent.Controller = Agent.ControllerType.AI;
                    Mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                    if (Mission.MainAgent.Formation == null || Mission.MainAgent.Formation.FormationIndex >= FormationClass.NumberOfRegularFormations)
                    {
                        Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
                    }
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            // mask code in Mission.OnAgentRemoved so that in spectator camera, formations will not be delegated to AI after player dead.
            if (Mission.MainAgent == affectedAgent && isSpectatorCamera)
            {
                affectedAgent.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)null;
                Mission.MainAgent = null;
            }
        }

        private void SwitchToAgent()
        {
            isSpectatorCamera = false;
            if (Mission.MainAgent != null)
            {
                Utility.DisplayLocalizedText("str_switch_to_player");
                Mission.MainAgent.Controller = Agent.ControllerType.Player;
                ToggleFreeCamera?.Invoke(false);
            }
            else
            {
                Utility.DisplayLocalizedText("str_player_dead");
                if (_controlTroopAfterPlayerDeadLogic.ControlTroopAfterDead() && Mission.MainAgent != null)
                    ToggleFreeCamera?.Invoke(false);
            }
        }

        private void SwitchToFreeCamera()
        {
            if (Mission.MainAgent != null && Mission.MainAgent.IsUsingGameObject)
                return;
            isSpectatorCamera = true;
            if (Mission.MainAgent != null)
            {
                Mission.MainAgent.Controller = Agent.ControllerType.AI;
                Mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                if (Mission.MainAgent.Formation == null || Mission.MainAgent.Formation.FormationIndex >=
                    FormationClass.NumberOfRegularFormations)
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
            }

            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_switch_to_free_camera");
        }
    }
}