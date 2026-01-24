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
using MathF = TaleWorlds.Library.MathF;

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
                // patch advance order for throwing formation
                Harmony.Patch(
                    typeof(MovementOrder).GetMethod("GetPositionAux",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod(nameof(Prefix_GetPositionAux),
                        BindingFlags.Static | BindingFlags.Public)));
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
            if (Mission.Current.IsNavalBattle)
                return true;
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
            if (!CommandSystemConfig.Get().FixAdvaneOrderForThrowing || Mission.Current.IsNavalBattle)
                return true;

            FormationQuerySystem querySystem = f.QuerySystem;
            bool isRanged = querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation;
            if (f.IsAIControlled && !CommandSystemConfig.Get().ApplyAdvanceOrderFixForAI && !isRanged)
                return true;

            if (__instance.OrderEnum != MovementOrder.MovementOrderEnum.Advance)
                return true;

            if (Mission.Current.Mode == TaleWorlds.Core.MissionMode.Deployment)
                return true;

            FormationQuerySystem enemyQuerySystem = f.TargetFormation?.QuerySystem ?? f.CachedClosestEnemyFormation;

            if (!isRanged && (querySystem.HasThrowingUnitRatio <= CommandSystemConfig.Get().ThrowerRatioThreshold || f.FiringOrder.OrderType == OrderType.HoldFire))
            {
                return true;
            }

            var commandQuerySystem = CommandQuerySystem.GetQueryForFormation(f);
            if (commandQuerySystem.RatioOfRemainingAmmo < CommandSystemConfig.Get().RemainingAmmoRatioThreshold)
            {
                return true;
            }

            var missileRange = commandQuerySystem.AverageMissileRangeAdjusted;
            if (missileRange < 1f)
                return true;

            WorldPosition enemyPosition;
            WorldPosition positionAux;
            if (enemyQuerySystem == null)
            {
                return true;
            }
            else
            {
                enemyPosition = enemyQuerySystem.Formation.CachedMedianPosition;
            }
            positionAux = enemyPosition;
            var distanceSquared = f.CurrentPosition.DistanceSquared(positionAux.AsVec2);
            var ammoFactor = MathF.Pow(MathF.Max(commandQuerySystem.RatioOfRemainingAmmo - CommandSystemConfig.Get().RemainingAmmoRatioThreshold, 0f), 0.2f);
            if (!CommandSystemConfig.Get().ShortenRangeBasedOnRemainingAmmo || isRanged)
            {
                ammoFactor = 1f;
            }
            var distanceFactor = isRanged ? 1 : MathF.Pow(MathF.Clamp(distanceSquared / MathF.Max(missileRange * missileRange, 1f) * 1.5f, 0f, 1f), 0.1f);
            var vec2 = GetDirectionAux(__instance, f);
            positionAux.SetVec2(positionAux.AsVec2 - vec2 * missileRange * ammoFactor * distanceFactor);
            var direction = f.Direction;
            var leftVec = new Vec2(-direction.y, direction.x);
            var width = f.Width;
            var leftFront = positionAux.AsVec2 + leftVec * width / 2;
            var rightFront = positionAux.AsVec2 - leftVec * width / 2;
            var leftBack = leftFront - direction * f.Depth;
            var righBack = rightFront - direction * f.Depth;
            positionAux = AdjustOutOfBoundaryPositions(positionAux, leftFront);
            positionAux = AdjustOutOfBoundaryPositions(positionAux, rightFront);
            positionAux = AdjustOutOfBoundaryPositions(positionAux, leftBack);
            positionAux = AdjustOutOfBoundaryPositions(positionAux, righBack);

            if (!____engageTargetPositionCache.IsValid)
                ____engageTargetPositionCache = positionAux;
            float num1 = (float)((double)f.QuerySystem.MovementSpeedMaximum * (double)f.QuerySystem.MovementSpeedMaximum * 9.0) * f.Depth;
            bool b1 = (double)(____engageTargetPositionCache.AsVec2 + vec2 * ____engageTargetPositionOffset).DistanceSquared(positionAux.AsVec2) > (double)f.CurrentPosition.DistanceSquared(____engageTargetPositionCache.AsVec2) * 0.10000000149011612;
            bool b2 = (double)positionAux.AsVec2.DistanceSquared(f.CurrentPosition) <= (double)num1;
            if ((b1 || b2))
            {
                ____engageTargetPositionCache = positionAux;
                ____engageTargetPositionOffset = 0.0f;
            }
            var newCachedPosition = ____engageTargetPositionCache;
            bool b3 = (double)newCachedPosition.AsVec2.DistanceSquared(f.CurrentPosition) > (double)num1 && f.Arrangement is LineFormation arrangement && (double)arrangement.GetUnavailableUnitPositions().Count<Vec2>() > (double)arrangement.UnitCount * 0.03;
            if (b3 || newCachedPosition.GetNavMesh() == UIntPtr.Zero)
            {
                //newCachedPosition.SetVec2(newCachedPosition.AsVec2 - vec2 * 10f);
                //____engageTargetPositionOffset += 10f;
                var backwardPosition = newCachedPosition;
                backwardPosition.SetVec2(backwardPosition.AsVec2 - vec2 * 10f);
                if (backwardPosition.GetNavMesh() == UIntPtr.Zero)
                {
                    backwardPosition = Mission.Current.GetStraightPathToTarget(backwardPosition.AsVec2, enemyPosition);
                }
                var offset = (newCachedPosition.AsVec2 - backwardPosition.AsVec2).DotProduct(vec2);
                newCachedPosition = backwardPosition;
                ____engageTargetPositionOffset += offset;
            }
            ____engageTargetPositionCache = newCachedPosition;
            __result = newCachedPosition;
            return false;
        }

        private static WorldPosition AdjustOutOfBoundaryPositions(WorldPosition orderPosition, Vec2 position)
        {
            if (!Mission.Current.IsPositionInsideBoundaries(position))
            {
                var boundaryPosition = Mission.Current.GetClosestBoundaryPosition(position);
                var diffVec = boundaryPosition - position;
                orderPosition.SetVec2(orderPosition.AsVec2 + diffVec);
            }
            return orderPosition;
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
