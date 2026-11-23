using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;

namespace RTSCameraAgentComponent
{
    public class ComponentAdder : MissionLogic
    {
        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            agent.AddComponent(new RTSCameraComponent(agent));
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            affectedAgent.GetComponent<RTSCameraComponent>()?.MarkAgentLeavingMission();
            base.OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            affectedAgent.GetComponent<RTSCameraComponent>()?.MarkAgentLeavingMission();
            base.OnAgentDeleted(affectedAgent);
        }
    }
}
