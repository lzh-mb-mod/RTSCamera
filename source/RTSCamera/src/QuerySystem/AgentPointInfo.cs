using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class AgentPointInfo
    {
        public readonly Agent Agent;
        public readonly Vec2 Point;

        public AgentPointInfo(Agent agent)
        {
            Agent = agent;
            Point = agent.Position.AsVec2;
        }

        public AgentPointInfo(Agent agent, Vec2 pos)
        {
            Agent = agent;
            Point = pos;
        }
    }

    public class AgentTargetPointInfo
    {
        public readonly Agent Agent;
        public readonly Vec2 Point;
        public readonly Vec2 ResistanceDirection;

        public AgentTargetPointInfo(Agent agent, Vec2 pos, Vec2 resistanceDirection)
        {
            Agent = agent;
            Point = pos;
            ResistanceDirection = resistanceDirection;
        }

        public AgentTargetPointInfo(Agent agent, Vec2 resistanceDirection)
        {
            Agent = agent;
            Point = agent.Position.AsVec2;
            ResistanceDirection = resistanceDirection;
        }
    }
}
