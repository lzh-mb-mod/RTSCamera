using TaleWorlds.MountAndBlade;

namespace RTSCameraAgentComponent
{
    public class ComponentAdder : MissionLogic
    {
        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            agent.AddComponent(new RTSCameraComponent(agent));
        }
    }
}
