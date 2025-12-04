using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_AgentNavalComponent
    {
        private static MethodInfo _checkAgentOffShip = AccessTools.TypeByName("AgentNavalComponent").GetMethod("CheckAgentOffShip",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("AgentNavalComponent").Method("OnTick"),
                    prefix: new HarmonyMethod(typeof(Patch_AgentNavalComponent).GetMethod(nameof(Prefix_OnTick), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_OnTick(AgentComponent __instance, float dt, Agent ___Agent, ref float ____lastOffShipCheckTime)
        {
            if ((double)dt <= 0.0)
                return false;
            if (___Agent.IsAIControlled)
                return true;
            if (!(____lastOffShipCheckTime + 5.0 <= ___Agent.Mission.CurrentTime && ___Agent.Mission.IsDeploymentFinished))
            {
                return true;
            }

             ____lastOffShipCheckTime += 5f;
            _checkAgentOffShip.Invoke(__instance, new object[] { false });
            return true;
        }
    }
}
