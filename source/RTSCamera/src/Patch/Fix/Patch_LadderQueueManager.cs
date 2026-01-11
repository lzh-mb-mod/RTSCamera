using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_LadderQueueManager
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                harmony.Patch(
                    typeof(LadderQueueManager).GetMethod("OnTickParallelAux",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_LadderQueueManager).GetMethod(
                        nameof(Prefix_OnTickParallelAux), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static void Prefix_OnTickParallelAux(ref List<Agent> ____userAgents)
        {
            for (int index = ____userAgents.Count - 1; index >= 0; index--)
            {
                Agent agent = ____userAgents[index];
                if (agent.Controller == Agent.ControllerType.Player)
                {
                    ____userAgents.RemoveAt(index);
                }
            }
        }
    }
}
