using CinematicCamera.Config.HotKey;
using MissionLibrary.Event;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CinematicCamera
{
    public class SetPlayerHealthLogic : MissionLogic
    {
        private readonly CinematicCameraConfig _config = CinematicCameraConfig.Get();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            Mission.OnMainAgentChanged += Mission_OnMainAgentChanged;
            MissionEvent.MainAgentWillBeChangedToAnotherOne += MainAgentWillBeChangedToAnotherOne;
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();

            Mission.OnMainAgentChanged -= Mission_OnMainAgentChanged;
            MissionEvent.MainAgentWillBeChangedToAnotherOne -= MainAgentWillBeChangedToAnotherOne;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.TogglePlayerInvulnerable)
                .IsKeyPressed(Mission.InputManager))
            {
                _config.PlayerInvulnerable = !_config.PlayerInvulnerable;
                UpdateInvulnerable(_config.PlayerInvulnerable);
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseDepthOfFieldDistance)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldDistance = MathF.Clamp(_config.DepthOfFieldDistance + 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldDistance();
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseDepthOfFieldDistance)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldDistance = MathF.Clamp(_config.DepthOfFieldDistance - 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldDistance();
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseDepthOfFieldStart)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldStart = MathF.Clamp(_config.DepthOfFieldStart + 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldDistance();
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseDepthOfFieldStart)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldStart = MathF.Clamp(_config.DepthOfFieldStart - 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldDistance();
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseDepthOfFieldEnd)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldEnd = MathF.Clamp(_config.DepthOfFieldEnd + 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
            if (CinematicCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseDepthOfFieldEnd)
                .IsKeyDown(Mission.InputManager))
            {
                _config.DepthOfFieldEnd = MathF.Clamp(_config.DepthOfFieldEnd - 0.05f, 0, 1000);
                ModifyCameraHelper.UpdateDepthOfFieldParameters();
            }
        }

        private void MainAgentWillBeChangedToAnotherOne(Agent newAgent)
        {
            if (_config.PlayerInvulnerable)
                UpdateInvulnerable(false);
        }

        private void Mission_OnMainAgentChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent != null)
            {
                if (_config.PlayerInvulnerable)
                    UpdateInvulnerable(true);
            }
        }

        public void UpdateInvulnerable(bool invulnerable)
        {
            if (Mission.MainAgent == null)
                return;
            var agent = Mission.MainAgent;
            //agent.SetInvulnerable(invulnerable);
            if (invulnerable)
            {
                agent.SetMortalityState(Agent.MortalityState.Invulnerable);
                if (agent.HasMount)
                {
                    agent.MountAgent.SetMortalityState(Agent.MortalityState.Invulnerable);
                }
            }
            else
            {
                agent.SetMortalityState(Agent.MortalityState.Mortal);
                if (agent.HasMount)
                {
                    agent.MountAgent.SetMortalityState(Agent.MortalityState.Mortal);
                }
            }            
        }
    }
}