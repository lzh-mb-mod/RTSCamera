﻿using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class CampaignSkillLogic
    {
        private readonly RTSCameraLogic _logic;
        private bool _isFreeCamera;
        private float _beginTime;
        public CampaignSkillLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
        }

        private void OnToggleFreeCamera(bool isFreeCamera)
        {
            if (Campaign.Current == null || (_logic.Mission.Mode != MissionMode.Battle && _logic.Mission.Mode != MissionMode.Deployment) || _logic.Mission.IsMissionEnding)
                return;
            _isFreeCamera = isFreeCamera;
            if (isFreeCamera)
            {
                _beginTime = _logic.Mission.CurrentTime;
            }
            else
            {
                UpdateSkillXp(false);
            }
        }

        public void AfterAddTeam(Team team)
        {
            team.PlayerOrderController.OnOrderIssued += OnOnOrderIssued;
        }

        private void OnOnOrderIssued(OrderType orderType, IEnumerable<Formation> appliedFormations, object[] delegateParams)
        {
            if (Campaign.Current == null || _logic.Mission.IsMissionEnding)
                return;
            if (Utility.IsTeamValid(_logic.Mission.PlayerTeam) &&
                (MapEvent.PlayerMapEvent == null ||
                 MapEvent.PlayerMapEvent.PlayerSide == _logic.Mission.PlayerTeam.Side) &&
                _logic.Mission.PlayerTeam.PlayerOrderController.Owner == _logic.Mission.MainAgent && _isFreeCamera)
            {
                UpdateSkillXp(true);
            }
        }

        private void UpdateSkillXp(bool hasOrderIssued)
        {
            var duration = _logic.Mission.CurrentTime - _beginTime;
            _beginTime = _logic.Mission.CurrentTime;
            GiveXpForScouting(duration);
            
            if (hasOrderIssued)
            {
                RTSCameraSkillBehavior.GetHeroForTacticLevel()
                    ?.AddSkillXp(DefaultSkills.Tactics, duration * MathF.Log10(_logic.Mission.AllAgents.Count) * 10);
            }
        }

        private void GiveXpForScouting(float duration)
        {
            float factor = 1;
            if (!Utility.IsAgentDead(_logic.Mission.MainAgent))
            {
                var distance = _logic.Mission.MainAgent.Position.Distance(_logic.Mission.GetCameraFrame().origin);
                factor = MathF.Max(1f, MathF.Log10(MathF.Max(distance, RTSCameraSkillBehavior.CameraDistanceLimit)));
            }
            RTSCameraSkillBehavior.GetHeroForScoutingLevel()
                ?.AddSkillXp(DefaultSkills.Scouting, duration * factor * 10);
        }
    }
}