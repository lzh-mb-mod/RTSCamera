using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
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
        private bool _battleResultComesOut = false;
        private float _freeCameraBeginTime;
        private float _orderIssueBeginTime;

        public CampaignSkillLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
            RTSCameraSkillBehavior.UpdateCameraMaxDistance();
        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            if (oldMissionMode == MissionMode.Deployment && _logic.Mission.Mode == MissionMode.Battle)
            {
                if (_isFreeCamera)
                {
                    _freeCameraBeginTime = _logic.Mission.CurrentTime;
                    _orderIssueBeginTime = _logic.Mission.CurrentTime;
                }
            }
        }

        internal void ShowBattleResults()
        {
            if (!ShouldGainSkillXp())
                return;
            UpdateScoutingSkillXp();
            _battleResultComesOut = true;
        }

        private bool ShouldGainSkillXp()
        {
            return !_battleResultComesOut && Campaign.Current != null && RTSCameraSkillBehavior.ShouldLimitCameraDistance(_logic.Mission) &&
                _logic.Mission.Mode == MissionMode.Battle && !_logic.Mission.IsMissionEnding;
        }

        private void OnToggleFreeCamera(bool isFreeCamera)
        {
            if (!ShouldGainSkillXp())
                return;
            if (isFreeCamera)
            {
                _orderIssueBeginTime = _freeCameraBeginTime = _logic.Mission.CurrentTime;
            }
            else
            {
                UpdateScoutingSkillXp();
            }
            _isFreeCamera = isFreeCamera;
        }

        public void AfterAddTeam(Team team)
        {
            team.PlayerOrderController.OnOrderIssued += OnOnOrderIssued;
        }

        private void OnOnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, object[] delegateParams)
        {
            if (!_isFreeCamera || !ShouldGainSkillXp())
                return;
            if (Utility.IsTeamValid(_logic.Mission.PlayerTeam) && Campaign.Current != null &&
                (MapEvent.PlayerMapEvent == null ||
                 MapEvent.PlayerMapEvent.PlayerSide == _logic.Mission.PlayerTeam.Side) &&
                _logic.Mission.PlayerTeam.PlayerOrderController.Owner == _logic.Mission.MainAgent)
            {
                UpdateTacticsSkillXp();
            }
        }

        private void UpdateScoutingSkillXp()
        {
            var scoutingDuration = MathF.Min(_logic.Mission.CurrentTime - _freeCameraBeginTime, RTSCameraSkillBehavior.ScoutingSkillGainMaxDuration);
            _freeCameraBeginTime = _logic.Mission.CurrentTime;
            GiveXpForScouting(scoutingDuration);
        }

        private void UpdateTacticsSkillXp()
        {
            var tacticsDuration = MathF.Min(_logic.Mission.CurrentTime - _orderIssueBeginTime, RTSCameraSkillBehavior.TacticsSkillGainMaxDuration);
            _orderIssueBeginTime = _logic.Mission.CurrentTime;
            GiveXpForTactics(tacticsDuration);
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
                ?.AddSkillXp(DefaultSkills.Scouting, duration * factor * RTSCameraSkillBehavior.ScoutingSkillGainFactor);
        }

        private void GiveXpForTactics(float duration)
        {
            RTSCameraSkillBehavior.GetHeroForTacticLevel()
                ?.AddSkillXp(DefaultSkills.Tactics, duration * MathF.Log10(_logic.Mission.AllAgents.Count) * RTSCameraSkillBehavior.TacticsSkillGainFactor);
        }
    }
}
