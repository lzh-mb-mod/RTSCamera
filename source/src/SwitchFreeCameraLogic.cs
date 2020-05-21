using System;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    public class SwitchFreeCameraLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        private ControlTroopLogic _controlTroopLogic;
        public bool isSpectatorCamera = false;

        private bool _isFirstTimeMainAgentChanged = true;
        private bool _isFirstTimeSetToFreeCamera = true;
        private bool _switchToFreeCameraNextTick = false;

        public event Action<bool> ToggleFreeCamera;

        public SwitchFreeCameraLogic(EnhancedMissionConfig config)
        {
            _config = config;
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _controlTroopLogic = Mission.GetMissionBehaviour<ControlTroopLogic>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
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
                else if (isSpectatorCamera)
                {
                    EnsureMainAgentControlledByAI();
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

            // the game may crash if no formation has agents and there are agents controlled by AI.
            if (Mission.MainAgent.Formation == null || Mission.MainAgent.Formation.FormationIndex >= FormationClass.NumberOfRegularFormations)
            {
                Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
            }
        }

        private void DoNotDisturbRTS()
        {
            Utility.DisplayLocalizedText("str_em_player_dead", null, new Color(1, 0, 0));
            _controlTroopLogic.ControlTroopAfterDead();
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
                _controlTroopLogic.ControlTroopAfterDead();
            }
            ToggleFreeCamera?.Invoke(false);
        }

        private void SwitchToFreeCamera()
        {
            if (Mission.MainAgent != null && Mission.MainAgent.IsUsingGameObject)
                return;
            isSpectatorCamera = true;
            if (Mission.MainAgent != null)
            {
                Utility.AIControlMainAgent((FormationClass)_config.PlayerFormation);
                if (_isFirstTimeSetToFreeCamera)
                {
                    _isFirstTimeSetToFreeCamera = false;
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
                }
            }
            else if (_isFirstTimeSetToFreeCamera)
                _isFirstTimeSetToFreeCamera = false;

            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_em_switch_to_free_camera");
        }
    }
}