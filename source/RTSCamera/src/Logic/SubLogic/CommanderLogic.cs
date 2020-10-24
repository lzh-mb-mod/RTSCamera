using System.ComponentModel;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class CommanderLogic
    {
        private readonly RTSCameraLogic _logic;

        public Mission Mission => _logic.Mission;

        public CommanderLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            Mission.OnMainAgentChanged += OnMainAgentChanged;
        }

        public void OnRemoveBehaviour()
        {
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
