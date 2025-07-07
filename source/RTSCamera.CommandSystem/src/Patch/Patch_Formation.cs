using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.src.Patch
{
    public class Patch_Formation
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
                    typeof(Formation).GetMethod("SetMovementOrder",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Formation).GetMethod(nameof(Postfix_SetMovementOrder),
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
        public static void Postfix_SetMovementOrder(Formation __instance, MovementOrder input)
        {
            if (input.OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget)
            {
                input.OnApply(__instance);
            }
        }
    }
}
