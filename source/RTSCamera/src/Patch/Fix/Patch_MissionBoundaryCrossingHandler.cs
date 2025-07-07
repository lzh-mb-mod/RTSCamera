using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionBoundaryCrossingHandler
    {
        private static readonly MethodInfo HandleAgentStateChange =
            typeof(MissionBoundaryCrossingHandler).GetMethod("HandleAgentStateChange",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionBoundaryCrossingHandler).GetMethod("TickForMainAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionBoundaryCrossingHandler).GetMethod(nameof(Prefix_TickForMainAgent_Prefix),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_TickForMainAgent_Prefix(MissionBoundaryCrossingHandler __instance, MissionTimer ____mainAgentLeaveTimer)
        {
            // Ignore boundary crossing event if in free camera.
            HandleAgentStateChange?.Invoke(__instance, new object[]
            {
                Agent.Main,
                Agent.Main.Controller == Agent.ControllerType.Player &&
                !__instance.Mission.IsPositionInsideBoundaries(Agent.Main.Position.AsVec2) && __instance.Mission.GetMissionBehavior<RTSCameraLogic>()?.SwitchFreeCameraLogic.IsSpectatorCamera != true,
                ____mainAgentLeaveTimer != null, ____mainAgentLeaveTimer
            });
            return false;
        }
    }
}
