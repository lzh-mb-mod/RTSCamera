using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_HumanAIComponent
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
                    typeof(HumanAIComponent).GetMethod(nameof(HumanAIComponent.GetDesiredSpeedInFormation),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_HumanAIComponent).GetMethod(nameof(Prefix_GetDesiredSpeedInFormation),
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

        public static bool Prefix_GetDesiredSpeedInFormation(HumanAIComponent __instance, Agent ___Agent, ref float __result, bool isCharging)
        {
            if (isCharging || CommandSystemConfig.Get().FormationSpeedSyncMode == FormationSpeedSyncMode.Disabled)
                return true;
            if (___Agent.Formation == null || ___Agent.Team == null || !___Agent.Team.IsPlayerTeam)
                return true;
            if (___Agent.Formation.Arrangement is ColumnFormation || !__instance.ShouldCatchUpWithFormation || isCharging || Mission.Current.IsMissionEnding)
                return true;
            if (!CommandQueueLogic.PendingOrders.TryGetValue(___Agent.Formation, out var pendingOrder))
                return true;

            if (!pendingOrder.ShouldAdjustFormationSpeed || pendingOrder.FormationExpectedPositions.Count <= 1 || !pendingOrder.FormationExpectedPositions.ContainsKey(___Agent.Formation))
                return true;
            if (isCharging || ___Agent.IsDetachedFromFormation)
                return true;
            Agent mountAgent = ___Agent.MountAgent;
            float maxSpeed = mountAgent != null ? mountAgent.MaximumForwardUnlimitedSpeed : ___Agent.MaximumForwardUnlimitedSpeed;
            bool flag = !isCharging;
            Vec3 agentPosition;
            //if (isCharging)
            //{
            //    FormationQuerySystem closestEnemyFormation = ___Agent.Formation.CachedClosestEnemyFormation;
            //    float num2 = float.MaxValue;
            //    float num3 = 4f * num1 * num1;
            //    if (closestEnemyFormation != null)
            //    {
            //        num2 = ___Agent.Formation.CachedMedianPosition.AsVec2.DistanceSquared(closestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
            //        if ((double)num2 <= (double)num3)
            //        {
            //            WorldPosition cachedMedianPosition = ___Agent.Formation.CachedMedianPosition;
            //            vec3 = cachedMedianPosition.GetNavMeshVec3MT();
            //            ref Vec3 local = ref vec3;
            //            cachedMedianPosition = closestEnemyFormation.Formation.CachedMedianPosition;
            //            Vec3 navMeshVec3Mt = cachedMedianPosition.GetNavMeshVec3MT();
            //            num2 = local.DistanceSquared(navMeshVec3Mt);
            //        }
            //    }
            //    flag = (double)num2 > (double)num3;
            //}
            if (flag)
            {
                Vec2 globalPositionOfUnit = ___Agent.Formation.GetCurrentGlobalPositionOfUnit(___Agent, true);
                agentPosition = ___Agent.Position;
                Vec2 agentPositionVec2 = agentPosition.AsVec2;
                var formationMovementSpeed = MathF.Max(0.1f, ___Agent.Formation.QuerySystem.MovementSpeed);
                var finalMovementSpeed = formationMovementSpeed;
                var distanceError = 1f;
                var targetPosition = ___Agent.Formation.GetOrderPositionOfUnit(___Agent);
                if (targetPosition.IsValid)
                {
                    var agentDistance = targetPosition.AsVec2.Distance(agentPositionVec2);
                    var formationDistance = pendingOrder.FormationTargetDistances[___Agent.Formation];
                    switch (CommandSystemConfig.Get().FormationSpeedSyncMode)
                    {
                        case FormationSpeedSyncMode.Linear:
                            {
                                finalMovementSpeed = MathF.Clamp((agentDistance + distanceError) / pendingOrder.MaxDuration, 0.1f, formationMovementSpeed);
                                break;
                            }
                        case FormationSpeedSyncMode.CatchUp:
                            {
                                var linearSpeedLimit = MathF.Clamp((agentDistance + distanceError) / pendingOrder.MaxDuration, 0.1f, formationMovementSpeed);
                                //catch up and do not wait for slower formation
                                finalMovementSpeed = MathF.Clamp(MathF.Lerp(linearSpeedLimit, formationMovementSpeed, (formationDistance - pendingOrder.DistanceWithMaxDuration + distanceError) / (formationMovementSpeed * 2f)), linearSpeedLimit, formationMovementSpeed);
                                break;
                            }
                        case FormationSpeedSyncMode.WaitForLastFormation:
                            {
                                var formationExpectedPosition = pendingOrder.FormationExpectedPositions[___Agent.Formation];
                                globalPositionOfUnit = globalPositionOfUnit - ___Agent.Formation.CurrentPosition + formationExpectedPosition;
                                var linearSpeedLimit = MathF.Clamp((agentDistance + distanceError) / pendingOrder.MaxDuration, 0.1f, formationMovementSpeed);
                                //catch up and do not wait for slower formation
                                finalMovementSpeed = MathF.Clamp(MathF.Lerp(linearSpeedLimit, formationMovementSpeed, (formationDistance - pendingOrder.DistanceWithMaxDuration + distanceError) / (formationMovementSpeed * 2f)), linearSpeedLimit, formationMovementSpeed);
                                break;
                            }
                    }
                }
                Vec2 currentDiffVec = globalPositionOfUnit - agentPositionVec2;
                float slowDownFactor = MathF.Clamp(-___Agent.GetMovementDirection().DotProduct(currentDiffVec), 0.0f, 100f);
                float mountFactor = ___Agent.MountAgent != null ? 4f : 2f;
                //float formationSpeedLimitFactor = (isCharging ? ___Agent.Formation.CachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents : ___Agent.Formation.CachedMovementSpeed) / num1;
                float formationSpeedLimitFactor = finalMovementSpeed / maxSpeed;
                float maxSpeedRatio = formationMovementSpeed / finalMovementSpeed;
                var progressFactor = MathF.Clamp((float)(0.7 + 0.4 * (((double)maxSpeed - (double)slowDownFactor * (double)mountFactor) / MathF.Max(1f, (double)maxSpeed + (double)slowDownFactor * (double)mountFactor))), 0, maxSpeedRatio);
                __result = MathF.Clamp(progressFactor * formationSpeedLimitFactor, 0.1f, 1f);
                //__result = MathF.Clamp((float)(0.7 + 0.4 * (((double)maxSpeed - (double)slowDownFactor * (double)mountFactor) / ((double)maxSpeed + (double)slowDownFactor * (double)mountFactor))) * formationSpeedLimitFactor, 0.1f, 1f);
                return false;
            }
            return true;
        }
    }
}
