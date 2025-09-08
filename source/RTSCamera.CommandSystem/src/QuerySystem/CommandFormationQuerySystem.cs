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
        private readonly QueryData<Vec2> _weightedAverageEnemyPosition;
        private readonly QueryData<Vec2> _weightedAverageFacingTargetEnemyPosition;
        private readonly QueryData<bool> _areAgentsNearTargetPositions;
        private readonly QueryData<bool> _coolDownToEvaluateAgentsDistanceToTarget;

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

        public Vec2 WeightedAverageEnemyPosition => this._weightedAverageEnemyPosition.Value;

        public Vec2 WeightedAverageFacingTargetEnemyPosition => this._weightedAverageFacingTargetEnemyPosition.Value;

        public bool AreAgentsNearTargetPositions => _areAgentsNearTargetPositions.Value;

        public bool CoolDownToEvaluateAgentsDistanceToTarget => _coolDownToEvaluateAgentsDistanceToTarget.Value;

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
                                float currentDistance = enemyFormation.QuerySystem.MedianPosition.GetNavMeshVec3().DistanceSquared((medianPosition ?? formation.QuerySystem.MedianPosition).GetNavMeshVec3());
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
                            float currentDistance = agent.Position.DistanceSquared((medianPosition ?? formation.QuerySystem.MedianPosition).GetNavMeshVec3());
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
            _weightedAverageEnemyPosition = new QueryData<Vec2>(() => Formation.Team.GetWeightedAverageOfEnemies(Patch_OrderController.GetFormationVirtualPositionVec2(formation)), 0.5f);
            _weightedAverageFacingTargetEnemyPosition = new QueryData<Vec2>(() =>
            {
                var targetFormation = Patch_OrderController.GetFacingEnemyTargetFormation(formation);
                if (targetFormation == null)
                    return formation.QuerySystem.WeightedAverageEnemyPosition;
                var basePoint = formation.CurrentPosition;

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
        }
        public void ExpireAllQueries()
        {
            _closestEnemyFormation?.Expire();
            _closestEnemyAgent?.Expire();
            _weightedAverageEnemyPosition?.Expire();
            _weightedAverageFacingTargetEnemyPosition?.Expire();
            _areAgentsNearTargetPositions.Expire();
            _coolDownToEvaluateAgentsDistanceToTarget.SetValue(true, Mission.Current.CurrentTime);
            NeedToUpdateTargetPositionDistance = true;
        }
    }
}
