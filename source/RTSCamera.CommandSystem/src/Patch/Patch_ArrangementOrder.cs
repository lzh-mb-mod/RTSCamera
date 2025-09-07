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
                harmony.Patch(
                    typeof(ArrangementOrder).GetMethod(nameof(ArrangementOrder.OnApply),
                    BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_ArrangementOrder).GetMethod(
                        nameof(Prefix_OnApply), BindingFlags.Static | BindingFlags.Public)));

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
                bool shouldEnableHollowSquareFor = Utilities.Utility.ShouldEnableHollowSquareFormationFor(formation);
                bool isSimuationFormation = formation.Team == null;
                bool isAIControlled = formation.IsAIControlled;
                if (shouldEnableHollowSquareFor || isSimuationFormation)
                {
                    __result = new SquareFormation(formation);
                    return false;
                }
            }
            return true;
        }

        public static bool Prefix_OnApply(ArrangementOrder __instance, Formation formation)
        {
            var previousUnitSpacing = formation.UnitSpacing;
            var newUnitSpacing = __instance.GetUnitSpacing();
            if (formation.Team != null && formation.Arrangement.GetType() != Utilities.Utility.GetTypeOfArrangement(__instance.OrderEnum, Utilities.Utility.ShouldEnableHollowSquareFormationFor(formation)))
            {
                AccessTools.Field(typeof(Formation), "_formOrder").SetValue(formation, FormOrder.FormOrderCustom(Patch_OrderController.GetNewWidthOfArrangementChange(formation, formation.Arrangement, __instance.OrderEnum)));
                AccessTools.Field(typeof(Formation), "_unitSpacing").SetValue(formation, newUnitSpacing);
            }
            else
            {
                formation.SetPositioning(unitSpacing: newUnitSpacing);
            }
            __instance.Rearrange(formation);
            if (__instance.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Scatter)
            {
                __instance.TickOccasionally(formation);
                formation.ResetArrangementOrderTickTimer();
            }
            ArrangementOrder.ArrangementOrderEnum orderEnum = __instance.OrderEnum;
            formation.ApplyActionOnEachUnit((Action<Agent>)(agent =>
            {
                if (agent.IsAIControlled)
                {
                    Agent.UsageDirection shieldDirectionOfUnit = ArrangementOrder.GetShieldDirectionOfUnit(formation, agent, orderEnum);
                    agent.EnforceShieldUsage(shieldDirectionOfUnit);
                }
                agent.UpdateAgentProperties();
                agent.RefreshBehaviorValues(formation.GetReadonlyMovementOrderReference().OrderEnum, orderEnum);
            }));
            return false;
        }
    }
}
