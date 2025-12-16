using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(MovementOrder), "Tick")]
    public class Patch_MovementOrder
    {
        private static readonly Harmony Harmony = new Harmony("RTSCommandPatchMovementOrder");

        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Have to be patched after Mission.Current is not null or call to Patch will throw null reference exception on Linux platform.
                // because that constructor of MovementOrder uses Mission.Current
                // patch behavior after charge to formation
                Harmony.Patch(
                    typeof(MovementOrder).GetMethod("GetSubstituteOrder",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod(nameof(Patch_MovementOrder.Prefix_GetSubstituteOrder),
                        BindingFlags.Static | BindingFlags.Public), Priority.First));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MBDebug.Print(e.ToString());
                Utility.DisplayMessage(e.ToString());
                return false;
            }

        }
        public static bool Prefix_GetSubstituteOrder(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget && formation.TargetFormation != null && CommandSystemConfig.Get().AttackSpecificFormation &&
                !CommandSystemSubModule.IsRealisticBattleModuleInstalled && !formation.IsAIControlled)
            {
                if (CommandSystemConfig.Get().BehaviorAfterCharge == BehaviorAfterCharge.Hold)
                {
                    var position = formation.QuerySystem.MedianPosition;
                    position.SetVec2(formation.CurrentPosition);
                    if (formation.Team == Mission.Current.PlayerTeam && formation.PlayerOwner == Agent.Main)
                    {
                        Utilities.Utility.DisplayFormationReadyMessage(formation);
                    }
                    __result = MovementOrder.MovementOrderMove(position);
                    return false;
                }
            }

            return true;
        }
    }
}
