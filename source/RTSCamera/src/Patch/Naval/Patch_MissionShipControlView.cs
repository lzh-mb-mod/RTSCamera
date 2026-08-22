using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionShipControlView
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
                var targetType = AccessTools.TypeByName("NavalDLC.View.MissionViews.MissionShipControlView");
                var target = AccessTools.Method(targetType, "OnObjectUsed",
                    new[] { typeof(Agent), typeof(UsableMissionObject) });
                harmony.Patch(target,
                    transpiler: new HarmonyMethod(typeof(Patch_MissionShipControlView).GetMethod(
                        nameof(Transpiler_OnObjectUsed), BindingFlags.Static | BindingFlags.Public)));
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

        public static IEnumerable<CodeInstruction> Transpiler_OnObjectUsed(IEnumerable<CodeInstruction> instructions)
        {
            var isMainAgent = AccessTools.PropertyGetter(typeof(Agent), nameof(Agent.IsMainAgent));
            var canUseShipControl = AccessTools.Method(
                typeof(Patch_MissionShipControlView), nameof(CanUseShipControl));
            var replaced = false;

            foreach (var instruction in instructions)
            {
                if (!replaced && instruction.Calls(isMainAgent))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = canUseShipControl;
                    replaced = true;
                }

                yield return instruction;
            }

            if (!replaced)
            {
                throw new InvalidOperationException(
                    "Failed to find Agent.IsMainAgent in MissionShipControlView.OnObjectUsed.");
            }
        }

        public static bool CanUseShipControl(Agent agent)
        {
            return agent.IsPlayerControlled ||
                   agent.IsMainAgent && Mission.Current?.Mode == MissionMode.Deployment;
        }
    }
}
