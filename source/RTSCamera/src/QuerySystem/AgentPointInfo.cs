using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class AgentPointInfo
    {
        public Agent Agent { get; }
        public readonly Vec2 Point;

        public AgentPointInfo(Agent agent)
        {
            Agent = agent;
            Point = agent.Position.AsVec2;
        }
    }
}
