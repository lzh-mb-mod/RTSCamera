using System.ComponentModel;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace EnhancedMission
{
    class FixScoreBoardAfterPlayerDeadLogic : MissionLogic
    {
        private MissionGauntletBattleScoreUI _scoreUI;

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _scoreUI = Mission.GetMissionBehaviour<MissionGauntletBattleScoreUI>();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

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
