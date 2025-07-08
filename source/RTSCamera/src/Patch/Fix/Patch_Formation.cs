using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    internal class Patch_Formation
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
                    typeof(Formation).GetMethod("Arrangement_OnShapeChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Formation).GetMethod(nameof(Prefix_Arrangement_OnShapeChanged),
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
        public static bool Prefix_Arrangement_OnShapeChanged(Formation __instance, ref bool ____orderLocalAveragePositionIsDirty, ref bool ____isArrangementShapeChanged)
        {
            // When the player troop and player controlled troop are in formation,
            // or specificly, when there're 2 agents whose IsPlayerUnit is true in the same formation,
            // Arrangement_OnShapeChanged will recurse itself and cause stack overflow.
            if (__instance.HasPlayerControlledTroop && __instance.IsPlayerTroopInFormation)
            {
                ____orderLocalAveragePositionIsDirty = true;
                ____isArrangementShapeChanged = true;
                return false;
            }

            return true;
        }
    }
}
