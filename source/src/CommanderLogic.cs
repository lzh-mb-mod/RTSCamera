using System.ComponentModel;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    class CommanderLogic : MissionLogic
    {
        public override void EarlyStart()
        {
            base.EarlyStart();

            this.Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void HandleOnCloseMission()
        {
            base.HandleOnCloseMission();

            this.Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.Mission.MainAgent != null)
                Utility.SetPlayerAsCommander();
            else
                Utility.CancelPlayerAsCommander();
        }
    }
}
