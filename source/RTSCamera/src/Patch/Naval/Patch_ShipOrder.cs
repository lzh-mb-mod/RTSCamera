using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_ShipOrder
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
                harmony.Patch(AccessTools.TypeByName("ShipOrder").Method("ManageShipDetachments"),
                    transpiler: new HarmonyMethod(typeof(Patch_ShipOrder).GetMethod(nameof(Transpile_ManageShipDetachments), BindingFlags.Static | BindingFlags.Public)));

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

        public static IEnumerable<CodeInstruction> Transpile_ManageShipDetachments(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            // Enable AI to pilot player ship in player mode.
            EnableAIPilotPlayerShip(codes, false);
            // patch twice
            EnableAIPilotPlayerShip(codes, true);
            return codes.AsEnumerable();
        }

        public static void EnableAIPilotPlayerShip(List<CodeInstruction> codes, bool isSecondPlace)
        {
            bool found_get_IsPlayerShip = false;
            int get_IsPlayerShip_Index = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                 if (!found_get_IsPlayerShip)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == "get_IsPlayerShip")
                        {
                            found_get_IsPlayerShip = true;
                            get_IsPlayerShip_Index = i;
                        }
                    }
                }
            }

            if (!found_get_IsPlayerShip)
                throw new Exception("Failed to find get_IsPlayerShip");

            bool verified = true;
            verified &= codes[get_IsPlayerShip_Index - 2].opcode == OpCodes.Ldarg_0;
            verified &= codes[get_IsPlayerShip_Index - 1].opcode == OpCodes.Ldfld && (codes[get_IsPlayerShip_Index - 1].operand as FieldInfo).Name == "_ownerShip";
            verified &= codes[get_IsPlayerShip_Index + 1].opcode == OpCodes.Brfalse_S;
            verified &= codes[get_IsPlayerShip_Index + 2].opcode == OpCodes.Call && (codes[get_IsPlayerShip_Index + 2].operand as MethodInfo).Name == "get_Current";
            verified &= codes[get_IsPlayerShip_Index + 3].opcode == OpCodes.Callvirt && (codes[get_IsPlayerShip_Index + 3].operand as MethodInfo).Name == "get_MainAgent";
            verified &= codes[get_IsPlayerShip_Index + 4].opcode == (isSecondPlace ? OpCodes.Brtrue_S : OpCodes.Brtrue);

            if (!verified)
                throw new Exception("Failed to verify patched code in Patch_ShipOrder.EnableAIPilotPlayerShip");

            codes[get_IsPlayerShip_Index].opcode = OpCodes.Call;
            codes[get_IsPlayerShip_Index].operand = typeof(Patch_ShipOrder).GetMethod(nameof(ShouldAIPilotPlayerShip), BindingFlags.Static | BindingFlags.Public);
            codes[get_IsPlayerShip_Index + 1].opcode = OpCodes.Brtrue_S;
            codes[get_IsPlayerShip_Index + 2].opcode = OpCodes.Nop;
            codes[get_IsPlayerShip_Index + 3].opcode = OpCodes.Nop;
            codes[get_IsPlayerShip_Index + 4].opcode = (isSecondPlace ? OpCodes.Br_S : OpCodes.Br);
        }

        public static bool ShouldAIPilotPlayerShip(MissionObject ownerShip)
        {
            var isShipAIControlled = Utilities.Utility.IsShipAIControlled(ownerShip);
            var isPlayerShip = Utilities.Utility.IsPlayerShip(ownerShip);
            return isPlayerShip && isShipAIControlled;
        }
    }
}
