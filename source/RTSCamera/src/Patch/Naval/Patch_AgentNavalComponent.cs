using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_AgentNavalComponent
    {
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
                    transpiler: new HarmonyMethod(typeof(Patch_AgentNavalComponent).GetMethod(
                        nameof(Transpiler_OnTick), BindingFlags.Static | BindingFlags.Public)));

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

        public static IEnumerable<CodeInstruction> Transpiler_OnTick(IEnumerable<CodeInstruction> instructions)
        {
            var isAIControlled = AccessTools.PropertyGetter(typeof(Agent), nameof(Agent.IsAIControlled));
            var shouldCheckAgentOffShip = AccessTools.Method(
                typeof(Patch_AgentNavalComponent), nameof(ShouldCheckAgentOffShip));
            var replaced = false;

            foreach (var instruction in instructions)
            {
                if (!replaced && instruction.Calls(isAIControlled))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = shouldCheckAgentOffShip;
                    replaced = true;
                }

                yield return instruction;
            }

            if (!replaced)
            {
                throw new InvalidOperationException("Failed to find Agent.IsAIControlled in AgentNavalComponent.OnTick.");
            }
        }

        public static bool ShouldCheckAgentOffShip(Agent agent)
        {
            return agent.IsAIControlled || agent.IsMainAgent && !agent.IsPlayerControlled;
        }
    }
}
