using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_ColumnFormation
    {
        private static bool _patched;

        private static MethodInfo GetLastUnit = typeof(ColumnFormation).GetMethod("GetLastUnit",
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
                var map = typeof(ColumnFormation).GetInterfaceMap(interfaceMethod.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                var targetMethod = map.TargetMethods[index];
                harmony.Patch(
                    targetMethod,
                    prefix: new HarmonyMethod(
                        typeof(Patch_ColumnFormation).GetMethod(nameof(Prefix_SwitchUnitLocationsWithBackMostUnit),
                            BindingFlags.Static | BindingFlags.Public)));
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
        public static bool Prefix_SwitchUnitLocationsWithBackMostUnit(ColumnFormation __instance, IFormationUnit unit)
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
    }
}
