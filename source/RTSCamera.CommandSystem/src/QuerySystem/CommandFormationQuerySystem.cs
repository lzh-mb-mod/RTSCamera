using Microsoft.VisualBasic;
using RTSCamera.CommandSystem.AgentComponents;
using RTSCamera.CommandSystem.Patch;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.QuerySystem
{
    public class CommandFormationQuerySystem
    {
        public readonly Formation Formation;
        public readonly QueryData<Formation> _closestEnemyFormation;
        private readonly QueryData<Agent> _closestEnemyAgent;
        private readonly QueryData<Vec2> _virtualWeightedAverageEnemyPosition;
        private readonly QueryData<Vec2> _weightedAverageFacingTargetEnemyPosition;
        private readonly QueryData<Vec2> _virtualWeightedAverageFacingTargetEnemyPosition;
        private readonly QueryData<bool> _areAgentsNearTargetPositions;
        private readonly QueryData<bool> _coolDownToEvaluateAgentsDistanceToTarget;
        private readonly QueryData<float> _averageMissileRangeAdjusted;
        private readonly QueryData<float> _ratioOfAgentsHavingAmmo;
        private readonly QueryData<float> _ratioOfRemainingAmmoQuery;
        private float _ratioOfRemainingAmmo = 0;

        public Formation ClosestEnemyFormation
        {
            get
            {
                if (_closestEnemyFormation.Value == null || _closestEnemyFormation.Value.CountOfUnits == 0)
                {
                    _closestEnemyFormation.Expire();
                }

                return _closestEnemyFormation.Value;
            }
        }
        public Agent ClosestEnemyAgent => this._closestEnemyAgent.Value;

        public Vec2 VirtualWeightedAverageEnemyPosition => this._virtualWeightedAverageEnemyPosition.Value;

        public Vec2 WeightedAverageFacingTargetEnemyPosition => this._weightedAverageFacingTargetEnemyPosition.Value;
        public Vec2 VirtualWeightedAverageFacingTargetEnemyPosition => this._virtualWeightedAverageFacingTargetEnemyPosition.Value;

        public bool AreAgentsNearTargetPositions => _areAgentsNearTargetPositions.Value;

        public bool CoolDownToEvaluateAgentsDistanceToTarget => _coolDownToEvaluateAgentsDistanceToTarget.Value;

        public float AverageMissileRangeAdjusted => _averageMissileRangeAdjusted.Value;

        public float RatioOfAgentsHavingAmmo => _ratioOfAgentsHavingAmmo.Value;

        public float RatioOfRemainingAmmo => _ratioOfRemainingAmmoQuery.Value;

        public bool HasCurrentMovementOrderCompleted
        {
            get
            {
                if (!NeedToUpdateTargetPositionDistance)
                    return true;
                if (CoolDownToEvaluateAgentsDistanceToTarget)
                    return false;
                if (AreAgentsNearTargetPositions)
                {
                    NeedToUpdateTargetPositionDistance = false;
                    return true;
                }
                return false;
            }
        }

        public bool NeedToUpdateTargetPositionDistance;

        public CommandFormationQuerySystem(Formation formation)
        {
            Formation = formation;

            Mission mission = Mission.Current;
            _closestEnemyFormation = new QueryData<Formation>(delegate
            {
                float minDistance = float.MaxValue;
                Formation closestFormation = null;
                foreach (Team enemyTeam in mission.Teams)
                {
                    if (enemyTeam.IsEnemyOf(formation.Team))
                    {
                        foreach (Formation enemyFormation in enemyTeam.FormationsIncludingSpecialAndEmpty)
                        {
                            if (enemyFormation.CountOfUnits > 0)
                            {
                                Patch_OrderController.GetFormationMovingTargetForPreview(formation, out var medianPosition);
                                float currentDistance = enemyFormation.CachedMedianPosition.GetNavMeshVec3().DistanceSquared((medianPosition ?? formation.CachedMedianPosition).GetNavMeshVec3());
                                if (currentDistance < minDistance)
                                {
                                    minDistance = currentDistance;
                                    closestFormation = enemyFormation;
                                }
                            }
                        }
                    }
                }

                return closestFormation;
            }, 1.5f);
            _closestEnemyAgent = new QueryData<Agent>(() =>
            {
                float minDistance = float.MaxValue;
                Agent closestAgent = (Agent)null;
                foreach (Team team in mission.Teams)
                {
                    if (team.IsEnemyOf(formation.Team))
                    {
                        foreach (Agent agent in (List<Agent>)team.ActiveAgents)
                        {
                            Patch_OrderController.GetFormationMovingTargetForPreview(formation, out var medianPosition);
                            float currentDistance = agent.Position.DistanceSquared((medianPosition ?? formation.CachedMedianPosition).GetNavMeshVec3());
                            if ((double)currentDistance < (double)minDistance)
                            {
                                minDistance = currentDistance;
                                closestAgent = agent;
                            }
                        }
                    }
                }
                return closestAgent;
            }, 1.5f);
            _virtualWeightedAverageEnemyPosition = new QueryData<Vec2>(() => Formation.Team.GetWeightedAverageOfEnemies(Patch_OrderController.GetFormationVirtualPositionVec2(formation)), 0.5f);
            _weightedAverageFacingTargetEnemyPosition = new QueryData<Vec2>(() =>
            {
                var targetFormation = Patch_OrderController.GetFacingEnemyTargetFormation(formation);
                if (targetFormation == null)
                    return formation.QuerySystem.WeightedAverageEnemyPosition;
                var basePoint = formation.CurrentPosition;
                return WeightedAverageFormationPosition(targetFormation, basePoint);
            }, 0.5f);
            _virtualWeightedAverageFacingTargetEnemyPosition = new QueryData<Vec2>(() =>
            {
                var targetFormation = Patch_OrderController.GetVirtualFacingEnemyTargetFormation(formation);
                if (targetFormation == null)
                    return formation.QuerySystem.WeightedAverageEnemyPosition;
                var basePoint = Patch_OrderController.GetFormationVirtualPositionVec2(formation);
                return WeightedAverageFormationPosition(targetFormation, basePoint);
            }, 0.5f);
            _areAgentsNearTargetPositions = new QueryData<bool>(() =>
            {
                if (formation.CountOfUnitsWithoutDetachedOnes > 0)
                {
                    float scoreSum = 0f;
                    float threshold = (float)formation.CountOfUnitsWithoutDetachedOnes / 2;
                    formation.ApplyActionOnEachAttachedUnit((agent) =>
                    {
                        var distanceSquared = agent.GetComponent<CommandSystemAgentComponent>()?.DistanceSquaredToTargetPosition ?? 0;
                        var score = MathF.Pow(MathF.E, -distanceSquared/7f);
                        scoreSum += score;
                    });
                    if (scoreSum > threshold)
                    {
                        return true;
                    }
                    return false;
                }
                return true;
            }, 0.5f);
            _coolDownToEvaluateAgentsDistanceToTarget = new QueryData<bool>(() => false, 0.31f + MBRandom.RandomFloat * 0.1f);
            _averageMissileRangeAdjusted = new QueryData<float>(() =>
            {
                if (formation.CountOfUnits == 0)
                    return 0f;
                float sum = 0f;
                int count = 0;
                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent.MissileRangeAdjusted > 0)
                    {
                        sum += agent.MissileRangeAdjusted;
                        count++;
                    }
                });
                if (count == 0)
                    return 0f;
                return sum / (float)count;
            }, 5f);
            _ratioOfAgentsHavingAmmo = new QueryData<float>(() =>
            {
                if (formation.CountOfUnits == 0)
                    return 0f;
                int countHavingAmmo = 0;
                int totalCurrentAmmo = 0;
                int totalMaxAmmo = 0;
                formation.ApplyActionOnEachUnit(agent =>
                {
                    Utilities.Utility.GetMaxAndCurrentAmmoOfAgent(agent, out var currentAmmo, out var maxAmmo);
                    totalCurrentAmmo += currentAmmo;
                    totalMaxAmmo += maxAmmo;
                    if (maxAmmo > 0 && currentAmmo > 0)
                    {
                        countHavingAmmo++;
                    }
                });
                _ratioOfRemainingAmmo = totalCurrentAmmo / (float)totalMaxAmmo;
                return (float)countHavingAmmo / (float)formation.CountOfUnits;
            }, 5f);
            _ratioOfRemainingAmmoQuery = new QueryData<float>(() => _ratioOfRemainingAmmo, 5f);
            _ratioOfRemainingAmmoQuery.SetSyncGroup(new IQueryData[] { _ratioOfAgentsHavingAmmo });
        }



        public void ExpireAllQueries()
        {
            _closestEnemyFormation?.Expire();
            _closestEnemyAgent?.Expire();
            _virtualWeightedAverageEnemyPosition?.Expire();
            _weightedAverageFacingTargetEnemyPosition?.Expire();
            _areAgentsNearTargetPositions.Expire();
            _coolDownToEvaluateAgentsDistanceToTarget.SetValue(true, Mission.Current.CurrentTime);
            _averageMissileRangeAdjusted.Expire();
            _ratioOfAgentsHavingAmmo.Expire();
            _ratioOfRemainingAmmoQuery.Expire();
            NeedToUpdateTargetPositionDistance = true;
            _ratioOfRemainingAmmo = 0;
        }

        private static Vec2 WeightedAverageFormationPosition(Formation targetFormation, Vec2 basePoint)
        {
            Vec2 zero = Vec2.Zero;
            float num1 = 0.0f;
            targetFormation.ApplyActionOnEachUnit((agent) =>
            {
                Vec2 asVec2 = agent.Position.AsVec2;
                float num2 = 1f / (basePoint - asVec2).LengthSquared;
                zero += asVec2 * num2;
                num1 += num2;
            });
            return (double)num1 > 0.0 ? zero * (1f / num1) : Vec2.Invalid;
        }
    }
}
