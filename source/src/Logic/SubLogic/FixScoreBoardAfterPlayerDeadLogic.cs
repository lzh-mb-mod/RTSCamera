using System.ComponentModel;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace RTSCamera.Logic.SubLogic
{
    public class FixScoreBoardAfterPlayerDeadLogic
    {
        private readonly RTSCameraLogic _logic;
        private MissionGauntletBattleScoreUI _scoreUI;

        public Mission Mission => _logic.Mission;

        public FixScoreBoardAfterPlayerDeadLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            _scoreUI = Mission.GetMissionBehaviour<MissionGauntletBattleScoreUI>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public void OnRemoveBehaviour()
        {
            Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_scoreUI == null)
                return;

            if (Mission.MainAgent != null)
            {
                ResetPlayerDeathInScoreUI();
            }
        }

        public void ResetPlayerDeathInScoreUI()
        {
            if (_scoreUI == null)
                return;

            _scoreUI.DataSource.IsMainCharacterDead = false;
            _scoreUI.DataSource.RefreshValues();
            bool isOver = _scoreUI.DataSource.IsOver;
            if (isOver)
            {
                _scoreUI.DataSource.IsOver = false;
                _scoreUI.DataSource.IsOver = true;
            }
        }
    }
}
