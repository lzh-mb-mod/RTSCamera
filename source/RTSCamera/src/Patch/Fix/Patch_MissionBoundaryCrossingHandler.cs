using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionBoundaryCrossingHandler
    {
        private static readonly MethodInfo HandleAgentStateChange =
            typeof(MissionBoundaryCrossingHandler).GetMethod("HandleAgentStateChange",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TickForMainAgent_Prefix(MissionBoundaryCrossingHandler __instance, MissionTimer ____mainAgentLeaveTimer)
        {
            HandleAgentStateChange?.Invoke(__instance, new object[]
            {
                Agent.Main,
                Agent.Main.Controller == Agent.ControllerType.Player &&
                !__instance.Mission.IsPositionInsideBoundaries(Agent.Main.Position.AsVec2),
                ____mainAgentLeaveTimer != null, ____mainAgentLeaveTimer
            });
            return false;
        }
    }
}
