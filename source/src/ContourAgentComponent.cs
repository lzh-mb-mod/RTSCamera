using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.src
{
    class ContourAgentComponent : AgentComponent
    {
        public ContourAgentComponent(Agent agent) : base(agent)
        {
        }

        protected override void OnDismount(Agent mount)
        {
            base.OnDismount(mount);

            mount.AgentVisuals?.SetContourColor(new uint?());
        }
    }
}
