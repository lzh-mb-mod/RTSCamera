﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
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
        public bool ControlTroop()
        {
            if (this.Mission.PlayerTeam != null)
            {
                var missionScreen = ScreenManager.TopScreen as MissionScreen;
                Agent closestAllyAgent =
                    (missionScreen?.LastFollowedAgent?.IsActive() ?? false)
                        ? missionScreen?.LastFollowedAgent
                        : GetAgentToControl() ?? this.Mission.PlayerTeam.Leader;
                if (closestAllyAgent == Mission.MainAgent)
                    return false;
                return ControlAgent(closestAllyAgent);
            }

            return false;
        }

        public bool ControlAgent(Agent agent)
        {
            if (agent != null)
            {
                if (agent.Team != Mission.PlayerTeam)
                    return false;
                if (!Utility.IsPlayerDead())
                {
                    MainAgentWillBeChangedToAnotherOne?.Invoke();
                    Utility.AIControlMainAgent(true);
                }
                GameTexts.SetVariable("ControlledTroopName", agent.Name);
                Utility.DisplayLocalizedText("str_em_control_troop");
                agent.Controller = Agent.ControllerType.Player;
                return true;
            }
            else
            {
                Utility.DisplayLocalizedText("str_em_no_troop_to_control");
                return false;
            }
        }

        public bool FocusOnAgent(Agent agent)
        {
            var missionScreen = ScreenManager.TopScreen as MissionScreen;
            if (missionScreen == null)
                return false;
            if (!_switchFreeCameraLogic.isSpectatorCamera)
                _switchFreeCameraLogic.SwitchCamera();
            if (!_flyCameraMissionView.LockToAgent)
                _flyCameraMissionView.LockToAgent = true;
            
            typeof(MissionScreen).GetProperty("LastFollowedAgent")?.GetSetMethod(true)
                .Invoke(missionScreen, new[] { agent });
            return true;
        }

        private Agent GetAgentToControl()
        {
            var agents = Mission.GetNearbyAllyAgents(
                new WorldPosition(this.Mission.Scene, this.Mission.Scene.LastFinalRenderCameraPosition).AsVec2, 1E+7f,
                Mission.PlayerTeam);
            var inPlayerPartyOnly = _config.ControlTroopsInPlayerPartyOnly;
            var preferCompanions = _config.PreferToControlCompanions;
            Agent firstAgent = null;
            var preferredAgent = agents.FirstOrDefault(agent =>
            {
                bool isInPlayerParty = !Utility.IsInPlayerParty(agent);
                if (inPlayerPartyOnly && !isInPlayerParty) return false;
                if (firstAgent != null)
                    firstAgent = agent;
                return !preferCompanions || agent.IsHero && isInPlayerParty;
            });
            return preferredAgent ?? firstAgent;
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
            _switchFreeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            _flyCameraMissionView = Mission.GetMissionBehaviour<FlyCameraMissionView>();
            _selectCharacterView = Mission.GetMissionBehaviour<SelectCharacterView>();
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            this.Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (this.Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.ControlTroop)))
            {
                if (_selectCharacterView.SelectedAgent != null)
                {
                    if (FocusOnAgent(_selectCharacterView.SelectedAgent))
                        _selectCharacterView.IsSelectingCharacter = false;
                }
                else if (ControlTroop())
                {
                    if (_switchFreeCameraLogic != null && _switchFreeCameraLogic.isSpectatorCamera)
                        _switchFreeCameraLogic.SwitchCamera();
                }
            }
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent == null && _config.ControlAlliesAfterDeath)
            {
                ControlTroop();
            }
        }
    }
}