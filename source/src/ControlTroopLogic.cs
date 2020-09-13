using RTSCamera.QuerySystem;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
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

        //public bool ControlTroop(bool forceControl, bool smoothMovement)
        //{
        //    if (this.Mission.PlayerTeam != null)
        //    {
        //        Agent closestAllyAgent = GetAgentToControl();
        //        return ControlAgent(closestAllyAgent, forceControl, smoothMovement);
        //    }

        //    return false;
        //}

        public bool SetMainAgent()
        {
            return SetToMainAgent(GetAgentToControl());
        }

        public bool SetToMainAgent(Agent agent)
        {
            if (agent != null)
            {
                if (!(ScreenManager.TopScreen is MissionScreen missionScreen))
                    return false;
                if (Mission.MainAgent == agent || agent.Team != Mission.PlayerTeam)
                    return false;
                if (!Utility.IsPlayerDead())
                {
                    MainAgentWillBeChangedToAnotherOne?.Invoke();
                    Utility.AIControlMainAgent(true);
                }
                GameTexts.SetVariable("ControlledTroopName", agent.Name);
                Utility.DisplayLocalizedText("str_rts_camera_control_troop");
                Mission.MainAgent = agent;
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
                if (Mission.MainAgent != agent)
                {
                    Mission.MainAgent = agent;
                }
                if (_switchFreeCameraLogic != null && _switchFreeCameraLogic.isSpectatorCamera)
                {
                    _switchFreeCameraLogic.SwitchCamera();
                }
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
                return false;
            }

            return false;
        }

        public bool ControlMainAgent(bool displayMessage = true, bool smoothMoveToAgent = true)
        {
            if (Mission.MainAgent != null)
            {
                if (Mission.MainAgent.Controller != Agent.ControllerType.Player)
                {
                    if (!(ScreenManager.TopScreen is MissionScreen missionScreen))
                        return false;
                    if (displayMessage)
                    {
                        GameTexts.SetVariable("ControlledTroopName", Mission.MainAgent.Name);
                        Utility.DisplayLocalizedText("str_rts_camera_control_troop");
                    }
                    Mission.MainAgent.Controller = Agent.ControllerType.Player;
                    if (smoothMoveToAgent)
                    {
                        Utility.SmoothMoveToAgent(missionScreen);
                    }
                }
            }
            else
            {
                Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
                return false;
            }

            return false;
        }

        //public bool ControlAgent(Agent agent)
        //{
        //    if (agent != null)
        //    {
        //        if (!(ScreenManager.TopScreen is MissionScreen missionScreen))
        //            return false;
        //        if (agent.Controller == Agent.ControllerType.Player || agent.Team != Mission.PlayerTeam)
        //            return false;
        //        if (!Utility.IsPlayerDead())
        //        {
        //            MainAgentWillBeChangedToAnotherOne?.Invoke();
        //            Utility.AIControlMainAgent(true);
        //        }
        //        GameTexts.SetVariable("ControlledTroopName", agent.Name);
        //        Utility.DisplayLocalizedText("str_rts_camera_control_troop");
        //        if (_switchFreeCameraLogic != null && _switchFreeCameraLogic.isSpectatorCamera)
        //        {
        //            _switchFreeCameraLogic.ForceSwitchToAgent(agent);
        //        }
        //        else
        //        {
        //            agent.Controller = Agent.ControllerType.Player;
        //            Utility.SmoothMoveToAgent(missionScreen);
        //        }

        //        return true;
        //    }
        //    else
        //    {
        //        Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
        //        return false;
        //    }
        //}

        //public bool ControlAgent(Agent agent, bool forceControl, bool smoothMovement)
        //{
        //    if (agent != null)
        //    {
        //        if (!(ScreenManager.TopScreen is MissionScreen missionScreen))
        //            return false;
        //        if (agent.Controller == Agent.ControllerType.Player || agent.Team != Mission.PlayerTeam)
        //            return false;
        //        if (!Utility.IsPlayerDead())
        //        {
        //            MainAgentWillBeChangedToAnotherOne?.Invoke();
        //            Utility.AIControlMainAgent(true);
        //        }
        //        GameTexts.SetVariable("ControlledTroopName", agent.Name);
        //        Utility.DisplayLocalizedText("str_rts_camera_control_troop");
        //        if (forceControl && _switchFreeCameraLogic != null && _switchFreeCameraLogic.isSpectatorCamera)
        //        {
        //            _switchFreeCameraLogic.ForceSwitchToAgent(agent);
        //        }
        //        else
        //        {
        //            agent.Controller = Agent.ControllerType.Player;

        //            if (smoothMovement && _switchFreeCameraLogic != null && !_switchFreeCameraLogic.isSpectatorCamera &&
        //                agent != missionScreen.LastFollowedAgent)
        //            {
        //                Utility.SmoothMoveToAgent(missionScreen);
        //            }
        //        }

        //        return true;
        //    }
        //    else
        //    {
        //        Utility.DisplayLocalizedText("str_rts_camera_no_troop_to_control");
        //        return false;
        //    }
        //}

        public Agent GetAgentToControl()
        {
            var missionScreen = ScreenManager.TopScreen as MissionScreen;

            if (missionScreen?.LastFollowedAgent?.IsActive() ?? false)
            {
                if ((!_switchFreeCameraLogic.isSpectatorCamera || _flyCameraMissionView.LockToAgent) &&
                    missionScreen.LastFollowedAgent.Team == Mission.PlayerTeam) return missionScreen?.LastFollowedAgent;
            }
            else if (Mission.MainAgent?.IsActive() ?? false)
            {
                return Mission.MainAgent;
            }

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

            if (this.Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.ControlTroop)))
            {
                if (_selectCharacterView.SelectedAgent != null)
                {
                    if (_flyCameraMissionView.FocusOnAgent(_selectCharacterView.SelectedAgent))
                        _selectCharacterView.IsSelectingCharacter = false;
                }
                else
                {
                    ForceControlAgent();
                }
            }
        }
    }
}