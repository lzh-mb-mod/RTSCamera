using RTSCameraAgentComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RTSCamera.View
{
    public static class FocusOnFormationMode
    {
        public static bool GetPositionToLookAt(Formation formation, out Vec3 result)
        {
            result = Vec3.Zero;
            var averagePosition = formation.GetAveragePositionOfUnits(false, false);
            if (!averagePosition.IsValid)
                return false;
            var agent = formation.GetMedianAgent(false, false, formation.GetAveragePositionOfUnits(false, false));
            if (agent == null)
                return false;
            var position = agent.GetWorldPosition();
            result = position.GetGroundVec3() + Vec3.Up * agent.GetEyeGlobalHeight();
            return true;
        }
    }
}
