using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace EnhancedMission
{
    class SwitchFreeCameraLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private EnhancedMissionOrderUIHandler _orderUIHandler;
        public bool isSpectatorCamera = false;

        public event Action<bool> ToggleFreeCamera;

        public SwitchFreeCameraLogic(EnhancedMissionConfig config)
        {
            _config = config;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (this.Mission.InputManager.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F10))
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
                //_orderUIHandler?.dataSource.RemoveTroops(Mission.Current.MainAgent);
                Mission.Current.MainAgent.Formation =
                    Mission.Current.PlayerTeam?.GetFormation((FormationClass)_config.playerFormation);
                //_orderUIHandler?.dataSource.AddTroops(Mission.Current.MainAgent);
            }
            ToggleFreeCamera?.Invoke(true);
            Utility.DisplayLocalizedText("str_switch_to_free_camera");
        }
    }
}