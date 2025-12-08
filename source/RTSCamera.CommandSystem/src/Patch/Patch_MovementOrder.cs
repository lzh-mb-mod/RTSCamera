using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.QuerySystem;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(MovementOrder), "Tick")]
    public class Patch_MovementOrder
    {

        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // patch advance order for throwing formation
                harmony.Patch(
                    typeof(MovementOrder).GetMethod("GetPositionAux",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod(nameof(Prefix_GetPositionAux),
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

        public static bool Prefix_GetSubstituteOrder(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (__instance.OrderType == OrderType.ChargeWithTarget && formation.TargetFormation != null && CommandSystemConfig.Get().AttackSpecificFormation &&
                !CommandSystemSubModule.IsRealisticBattleModuleInstalled && !formation.IsAIControlled)
            {
                if (CommandSystemConfig.Get().BehaviorAfterCharge == BehaviorAfterCharge.Hold)
                {
                    var position = formation.CachedMedianPosition;
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

        public static bool Prefix_GetPositionAux(MovementOrder __instance, Formation f, WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache, ref WorldPosition __result, ref WorldPosition ____engageTargetPositionCache, ref float ____engageTargetPositionOffset)
        {
            if (!CommandSystemConfig.Get().FixAdvaneOrderForThrowing)
                return true;

            if (__instance.OrderEnum != MovementOrder.MovementOrderEnum.Advance)
                return true;

            if (Mission.Current.Mode == TaleWorlds.Core.MissionMode.Deployment)
                return true;


            FormationQuerySystem querySystem = f.QuerySystem;
            FormationQuerySystem enemyQuerySystem = f.TargetFormation?.QuerySystem ?? f.CachedClosestEnemyFormation;
            WorldPosition positionAux;


            if (querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation ||  querySystem.HasThrowingUnitRatio <= CommandSystemConfig.Get().JavelinThrowerRatioThreshold || f.FiringOrder.OrderType == OrderType.HoldFire)
            {
                return true;
            }

            var commandQuerySystem = CommandQuerySystem.GetQueryForFormation(f);
            if (commandQuerySystem.RatioOfRemainingAmmo < CommandSystemConfig.Get().RemainingAmmoRatioThreshold)
            {
                return true;
            }
            if (enemyQuerySystem == null)
            {
                return true;
            }
            else
            {
                positionAux = enemyQuerySystem.Formation.CachedMedianPosition;
            }

            var vec2 = GetDirectionAux(__instance, f);
            positionAux.SetVec2(positionAux.AsVec2 - vec2 * commandQuerySystem.AverageMissileRangeAdjusted);

            if (!____engageTargetPositionCache.IsValid)
                ____engageTargetPositionCache = positionAux;
            float num1 = (float)((double)f.QuerySystem.MovementSpeedMaximum * (double)f.QuerySystem.MovementSpeedMaximum * 9.0) * f.Depth;
            if ((double)(____engageTargetPositionCache.AsVec2 + vec2 * ____engageTargetPositionOffset).DistanceSquared(positionAux.AsVec2) > (double)f.CurrentPosition.DistanceSquared(____engageTargetPositionCache.AsVec2) * 0.10000000149011612 || (double)positionAux.AsVec2.DistanceSquared(f.CurrentPosition) <= (double)num1)
            {
                ____engageTargetPositionCache = positionAux;
                ____engageTargetPositionOffset = 0.0f;
            }
            positionAux = ____engageTargetPositionCache;
            if ((double)positionAux.AsVec2.DistanceSquared(f.CurrentPosition) > (double)num1 && f.Arrangement is LineFormation arrangement && (double)arrangement.GetUnavailableUnitPositions().Count<Vec2>() > (double)arrangement.UnitCount * 0.03)
            {
                positionAux.SetVec2(positionAux.AsVec2 - vec2 * 10f);
                ____engageTargetPositionOffset += 10f;
            }
            ____engageTargetPositionCache = positionAux;
            __result = positionAux;
            return false;
        }

        public static Vec2 GetDirectionAux(MovementOrder __instance, Formation f)
        {
            switch (__instance.OrderEnum)
            {
                case MovementOrder.MovementOrderEnum.Advance:
                case MovementOrder.MovementOrderEnum.FallBack:
                    FormationQuerySystem formationQuerySystem = f.TargetFormation?.QuerySystem ?? f.CachedClosestEnemyFormation;
                    return formationQuerySystem != null ? (formationQuerySystem.Formation.CachedMedianPosition.AsVec2 - f.CachedAveragePosition).Normalized() : Vec2.One;
                default:
                    Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\MovementOrder.cs", nameof(GetDirectionAux), 1789);
                    return Vec2.One;
            }
        }
    }
}
