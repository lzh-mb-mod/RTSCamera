using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBSCAN;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class AgentPointInfo : IPointData
    {
        public Agent Agent { get; }
        private readonly Point _point;

        public AgentPointInfo(Agent agent)
        {
            Agent = agent;
            _point = new Point(agent.Position.x, agent.Position.y);
        }

        public ref readonly Point Point => ref _point;
    }
}
