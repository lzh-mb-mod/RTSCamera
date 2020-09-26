using RTSCamera.Config;
using RTSCamera.QuerySystem;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera
{
    public class ControlTroopLogic : MissionLogic
    {

        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private FlyCameraMissionView _flyCameraMissionView;
        private SelectCharacterView _selectCharacterView;

        public event Action MainAgentWillBeChangedToAnotherOne;

        public MissionScreen MissionScreen => _flyCameraMissionView?.MissionScreen;

        public bool SetMainAgent()
        {
            return SetToMainAgent(GetAgentToControl());
        }

        public bool SetToMainAgent(Agent agent)
        {
            if (agent != null)
            {
                if (Mission.MainAgent == agent || agent.Team != Mission.PlayerTeam)
                    return false;
                if (!Utility.IsPlayerDead())
                {
                    MainAgentWillBeChangedToAnotherOne?.Invoke();
                    Utility.AIControlMainAgent(true);
                }
                GameTexts.SetVariable("ControlledTroopName", agent.Name);
                Utility.DisplayLocalizedText("str_rts_camera_control_troop");
                bool shouldSmoothMoveToAgent = Utility.BeforeSetMainAgent();
                if (_switchFreeCameraLogic.isSpectatorCamera)
                {
                    Mission.MainAgent = agent;
                }
                else
                {
                    agent.Controller = Agent.ControllerType.Player;
                }
                Utility.AfterSetMainAgent(shouldSmoothMoveToAgent, _flyCameraMissionView.MissionScreen);

                return true;
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
                return false;
            }
        }

        public bool ForceControlAgent()
        {
            return ForceControlAgent(GetAgentToControl());
        }

        public bool ForceControlAgent(Agent agent)
        {
            if (agent != null)
            {
                if (agent.Controller == Agent.ControllerType.Player || agent.Team != Mission.PlayerTeam)
                    return false;
                if (!Utility.IsPlayerDead())
                {
                    MainAgentWillBeChangedToAnotherOne?.Invoke();
                    Utility.AIControlMainAgent(true);
                }
                bool shouldSmoothMoveToAgent = Utility.BeforeSetMainAgent();
                if (_switchFreeCameraLogic.isSpectatorCamera)
                {
                    Mission.MainAgent = agent;
                    _switchFreeCameraLogic.SwitchCamera();
                }
                else
                {
                    agent.Controller = Agent.ControllerType.Player;
                }

                Utility.AfterSetMainAgent(shouldSmoothMoveToAgent, _flyCameraMissionView.MissionScreen);
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
                return false;
            }

            return false;
        }

        public bool ControlMainAgent(bool displayMessage = true)
        {
            if (Mission.MainAgent != null)
            {
                if (Mission.MainAgent.Controller != Agent.ControllerType.Player)
                {
                    if (displayMessage)
                    {
                        GameTexts.SetVariable("ControlledTroopName", Mission.MainAgent.Name);
                        Utility.DisplayLocalizedText("str_rts_camera_control_troop");
                    }
                    bool shouldSmoothMoveToAgent = Utility.BeforeSetMainAgent();
                    Mission.MainAgent.Controller = Agent.ControllerType.Player;
                    Utility.AfterSetMainAgent(shouldSmoothMoveToAgent, _flyCameraMissionView.MissionScreen);
                }
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
                return false;
            }

            return false;
        }

        public Agent GetAgentToControl()
        {
            if (_flyCameraMissionView.MissionScreen?.LastFollowedAgent?.IsActive() ?? false)
            {
                if ((!_switchFreeCameraLogic.isSpectatorCamera || _flyCameraMissionView.LockToAgent) &&
                    _flyCameraMissionView.MissionScreen.LastFollowedAgent.Team == Mission.PlayerTeam) return _flyCameraMissionView.MissionScreen?.LastFollowedAgent;
            }
            else if (Mission.MainAgent?.IsActive() ?? false)
            {
                return Mission.MainAgent;
            }

            if (Mission.PlayerTeam == null)
                return null;

            var nearestAgent =
                QueryDataStore.Get(Mission.PlayerTeam.GetFormation(_switchFreeCameraLogic.CurrentPlayerFormation))
                    .NearestAgent(Mission.Scene.LastFinalRenderCameraPosition.AsVec2, true);
            if (nearestAgent != null && nearestAgent.IsActive() && CanControl(nearestAgent))
                return nearestAgent;
            var firstPreferredAgents = Mission.GetNearbyAllyAgents(
                new WorldPosition(this.Mission.Scene, this.Mission.Scene.LastFinalRenderCameraPosition).AsVec2, 20f,
                Mission.PlayerTeam);
            var secondPreferredAgents = Mission.GetNearbyAllyAgents(
                new WorldPosition(this.Mission.Scene, this.Mission.Scene.LastFinalRenderCameraPosition).AsVec2, 1E+7f,
                Mission.PlayerTeam);
            var preferCompanions = _config.PreferToControlCompanions;
            Agent firstPreferredFallback = null;
            var firstPreferredAgent = firstPreferredAgents.FirstOrDefault(agent =>
            {
                if (agent.IsRunningAway)
                    return false;
                if (!CanControl(agent))
                    return false;
                if (firstPreferredFallback == null)
                    firstPreferredFallback = agent;
                return !preferCompanions || agent.IsHero && Utility.IsInPlayerParty(agent);
            });
            if (firstPreferredAgent != null || firstPreferredFallback != null)
                return firstPreferredAgent ?? firstPreferredFallback;
            Agent secondPreferredFallback = null;
            var secondPreferredAgent = secondPreferredAgents.FirstOrDefault(agent =>
            {
                if (agent.IsRunningAway)
                    return false;
                if (!CanControl(agent))
                    return false;
                if (secondPreferredFallback == null)
                    secondPreferredFallback = agent;
                return !preferCompanions || agent.IsHero && Utility.IsInPlayerParty(agent);
            });
            return secondPreferredAgent ?? secondPreferredFallback ?? Mission.PlayerTeam.Leader;
        }

        private bool CanControl(Agent agent)
        {
            return agent.IsHuman && agent.IsActive() && (!_config.ControlTroopsInPlayerPartyOnly || Utility.IsInPlayerParty(agent));
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _switchFreeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            _flyCameraMissionView = Mission.GetMissionBehaviour<FlyCameraMissionView>();
            _selectCharacterView = Mission.GetMissionBehaviour<SelectCharacterView>();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (MissionScreen.SceneLayer.Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.ControlTroop)))
            {
                if (_selectCharacterView.LockOnAgent())
                    return;

                if (Mission.MainAgent?.Controller == Agent.ControllerType.Player)
                    return;
                
                ForceControlAgent();
            }
        }
    }
}