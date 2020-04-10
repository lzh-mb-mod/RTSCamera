using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    class SwitchFreeCameraLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();
        public bool isSpectatorCamera = false;

        public event Action<bool> ToggleFreeCamera;

        public SwitchFreeCameraLogic(EnhancedMissionConfig config)
        {
            _config = config;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

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
                Mission.GetMissionBehaviour<ControlTroopAfterPlayerDeadLogic>()?.ControlTroopAfterDead();
                ToggleFreeCamera?.Invoke(false);
            }
        }

        private void SwitchToFreeCamera()
        {
            isSpectatorCamera = true;
            if (Mission.MainAgent != null)
            {
                Mission.MainAgent.Controller = Agent.ControllerType.AI;
                Mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                if (Mission.MainAgent.Formation?.FormationIndex != (FormationClass)_config.PlayerFormation)
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
            }
            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_switch_to_free_camera");
        }
    }
}