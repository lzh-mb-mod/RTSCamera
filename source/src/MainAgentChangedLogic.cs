using System.ComponentModel;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace EnhancedMission
{
    class MainAgentChangedLogic : MissionLogic
    {
        private MissionGauntletBattleScoreUI _scoreUI;

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _scoreUI = Mission.GetMissionBehaviour<MissionGauntletBattleScoreUI>();
        }

        public override void EarlyStart()
        {
            base.EarlyStart();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void HandleOnCloseMission()
        {
            base.HandleOnCloseMission();

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
        }
    }
}
