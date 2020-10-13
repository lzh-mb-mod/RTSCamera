using System.ComponentModel;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic
{
    class CommanderLogic : MissionLogic
    {
        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            Mission.OnMainAgentChanged -= OnMainAgentChanged;
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Mission.MainAgent != null)
                Utility.SetPlayerAsCommander();
            else
                Utility.CancelPlayerAsCommander();
        }
    }
}
