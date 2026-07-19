using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static HarmonyLib.Code;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.Patch
{
    public class Patch_MissionState
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // override fast forward time speed
                if (RTSCameraConfig.Get().OverrideFastForwardSpeed)
                {
                    harmony.Patch(
                        typeof(MissionState).GetMethod("TickMission", BindingFlags.Instance | BindingFlags.NonPublic),
                        transpiler: new HarmonyMethod(
                            typeof(Patch_MissionState).GetMethod(nameof(Transpile_TickMission),
                            BindingFlags.Static | BindingFlags.Public)));
                }
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
        public static IEnumerable<CodeInstruction> Transpile_TickMission(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            OverrideFastForwardSpeed(codes);
            return codes.AsEnumerable();
        }

        public static void OverrideFastForwardSpeed(List<CodeInstruction> codes)
        {
            //// [151 7 - 151 45]
            //IL_0152: ldarg.0      // this
            //IL_0153: call instance class TaleWorlds.MountAndBlade.Mission TaleWorlds.MountAndBlade.MissionState::get_CurrentMission()
            //IL_0158: callvirt instance bool TaleWorlds.MountAndBlade.Mission::get_IsFastForward()
            //IL_015d: brfalse.s IL_01d5

            //// [153 9 - 153 28]
            //IL_015f: ldloc.0      // dt
            //IL_0160: ldc.r4       9
            //IL_0165: mul
            //IL_0166: stloc.3      // num

            bool found_get_IsFastForward = false;
            int get_IsFastForward_index = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!found_get_IsFastForward)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == "get_IsFastForward")
                        {
                            found_get_IsFastForward = true;
                            get_IsFastForward_index = i;
                            break;
                        }
                    }
                }
            }

            if (!found_get_IsFastForward)
            {
                throw new Exception("Failed to find get_IsFastForward");
            }

            bool verified = true;
            verified &= codes[get_IsFastForward_index + 1].opcode == OpCodes.Brfalse_S;
            verified &= codes[get_IsFastForward_index + 2].opcode == OpCodes.Ldloc_0;
            verified &= codes[get_IsFastForward_index + 3].opcode == OpCodes.Ldc_R4;
            verified &= codes[get_IsFastForward_index + 3].operand is float value && value == 9f;
            verified &= codes[get_IsFastForward_index + 4].opcode == OpCodes.Mul;
            verified &= codes[get_IsFastForward_index + 5].opcode == OpCodes.Stloc_3;

            if (!verified)
            {
                throw new Exception("Failed to verify patched code in Patch_Mission.OverrideFastForwardSpeed");
            }

            codes[get_IsFastForward_index + 3].opcode = OpCodes.Call; 
            codes[get_IsFastForward_index + 3].operand = typeof(Patch_MissionState).GetMethod(nameof(GetOverridenFastForwardSpeed), BindingFlags.Static | BindingFlags.Public);
        }

        public static float GetOverridenFastForwardSpeed()
        {
            var config = RTSCameraConfig.Get();
            if (!config.OverrideFastForwardSpeed)
                return 9f;

            return MathF.Clamp(config.FastForwardSpeed - 1f, 1f, 9f);
        }
    }
}
