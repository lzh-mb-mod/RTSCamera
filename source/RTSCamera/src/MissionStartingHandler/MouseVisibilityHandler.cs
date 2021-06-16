using MissionLibrary.Controller;
using MissionSharedLibrary.Controller;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.MissionStartingHandler
{
    public class MouseVisibilityHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {
            MissionStartingManager.AddMissionBehaviour(entranceView, new );
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
        }
    }
}
