using MissionLibrary.Controller;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCameraAgentComponent
{
    public class MissionStartingHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {
            AddMissionBehaviour(entranceView, new ComponentAdder());
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
        }
        public static void AddMissionBehaviour(MissionView entranceView, MissionBehavior behaviour)
        {
            behaviour.OnAfterMissionCreated();
            entranceView.Mission.AddMissionBehavior(behaviour);
        }
    }
}
