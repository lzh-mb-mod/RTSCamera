using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_LineFormation
    {
        private static bool _patched;

        private static MethodInfo GetLastUnit = typeof(LineFormation).GetMethod("GetLastUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                var interfaceMethod = typeof(IFormationArrangement).GetMethod("SwitchUnitLocationsWithBackMostUnit",
                    BindingFlags.Instance | BindingFlags.Public);
                var map = typeof(LineFormation).GetInterfaceMap(interfaceMethod.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                var targetMethod = map.TargetMethods[index];
                harmony.Patch(
                    targetMethod,
                    prefix: new HarmonyMethod(
                        typeof(Patch_LineFormation).GetMethod(nameof(Prefix_SwitchUnitLocationsWithBackMostUnit),
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
        public static bool Prefix_SwitchUnitLocationsWithBackMostUnit(LineFormation __instance, IFormationUnit unit)
        {
            // When the player troop and player controlled troop are in formation,
            // or specificly, when there're 2 agents whose IsPlayerUnit is true in the same formation,
            // Arrangement_OnShapeChanged will recurse itself and cause stack overflow.

            IFormationUnit lastUnit = (IFormationUnit)GetLastUnit?.Invoke(__instance, new object[] {});
            if (lastUnit == null || unit == null || lastUnit == unit || lastUnit.IsPlayerUnit && unit.IsPlayerUnit)
            {
                // If the last unit and the current unit are both player controlled,
                // we don't want to switch their locations.
                return false;
            }

            return true;
        }

        // need to patch OnFormationFrameChanged to avoid OrderController.actualWidths being removed when formation is positioned in narrow place.
    }
}
