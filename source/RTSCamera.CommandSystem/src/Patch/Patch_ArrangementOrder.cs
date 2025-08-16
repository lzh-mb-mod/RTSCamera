using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_ArrangementOrder
    {

        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // for resizable square formation
                harmony.Patch(
                    typeof(ArrangementOrder).GetMethod(nameof(ArrangementOrder.GetArrangement),
                    BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_ArrangementOrder).GetMethod(
                        nameof(Prefix_GetArrangement), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
            return true;
        }

        public static bool Prefix_GetArrangement(Formation formation, ArrangementOrder __instance, ref IFormationArrangement __result)
        {
            if (__instance.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Square && CommandSystemConfig.Get().HollowSquare)
            {
                bool isFormationUnderPlayerCommand = Utilities.Utility.ShouldEnablePlayerOrderControllerPatchForFormation(formation);
                bool isSimuationFormation = formation.Team == null;
                bool isAIControlled = formation.IsAIControlled;
                if (isFormationUnderPlayerCommand || isSimuationFormation)
                {
                    __result = new SquareFormation(formation);
                    return false;
                }
            }
            return true;
        }
    }
}
