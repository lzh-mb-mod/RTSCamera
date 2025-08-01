using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly QueryData<float> _averageDistanceSquareWithVelocityToOrderPositionExcludingFarAgents;
        private readonly QueryData<float> _ratioOfAgentsNearOrderPosition;

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

        public float AverageDistanceSquareWithVelocityToOrderPositionExcludingFarAgents => _averageDistanceSquareWithVelocityToOrderPositionExcludingFarAgents.Value;

        public float RatioOfAgentsNearOrderPosition => _ratioOfAgentsNearOrderPosition.Value;

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
                        foreach (Formation formation in enemyTeam.FormationsIncludingSpecialAndEmpty)
                        {
                            if (formation.CountOfUnits > 0)
                            {
                                float currentDistance = formation.QuerySystem.MedianPosition.GetNavMeshVec3().DistanceSquared(Patch_OrderController.GetFormationVirtualPosition(formation).GetNavMeshVec3());
                                if (currentDistance < minDistance)
                                {
                                    minDistance = currentDistance;
                                    closestFormation = formation;
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
                            float currentDistance = agent.Position.DistanceSquared(Patch_OrderController.GetFormationVirtualPosition(formation).GetNavMeshVec3());
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
            _averageDistanceSquareWithVelocityToOrderPositionExcludingFarAgents = new QueryData<float>(() =>
            {
                if (formation.CountOfUnitsWithoutDetachedOnes > 0)
                {
                    int count = 0;
                    float distanceSquaredAmount = 0;
                    formation.ApplyActionOnEachUnit((agent) =>
                    {
                        var worldPosition = agent.Formation.GetOrderPositionOfUnit(agent);
                        if (worldPosition.IsValid)
                        {
                            var distanceSquared = worldPosition.GetGroundVec3().DistanceSquared(agent.Position + agent.Velocity * 2f);
                            distanceSquaredAmount += distanceSquared;
                            ++count;
                        }
                    });
                    if (count > 0)
                    {
                        float threshold = distanceSquaredAmount / count * 1.1f;
                        float distanceSquaredAmount2 = 0;
                        int count2 = 0;
                        formation.ApplyActionOnEachUnit((agent) =>
                        {
                            var worldPosition = agent.Formation.GetOrderPositionOfUnit(agent);
                            if (worldPosition.IsValid)
                            {
                                var distanceSquared = worldPosition.GetGroundVec3().DistanceSquared(agent.Position + agent.Velocity * 2f);
                                if (distanceSquared < threshold)
                                {
                                    distanceSquaredAmount2 += distanceSquared;
                                    ++count2;
                                }
                            }
                        });
                        if (count2 > 0)
                        {
                            return distanceSquaredAmount2 / count2;
                        }
                    }
                }
                return 0f;
            }, 0.5f);
            _ratioOfAgentsNearOrderPosition = new QueryData<float>(() =>
            {
                if (formation.CountOfUnitsWithoutDetachedOnes > 0)
                {
                    int count = 0;
                    int amount = 0;
                    formation.ApplyActionOnEachUnit((agent) =>
                    {
                        var worldPosition = agent.Formation.GetOrderPositionOfUnit(agent);
                        if (worldPosition.IsValid)
                        {
                            var distanceSquared1 = worldPosition.GetGroundVec3().DistanceSquared(agent.Position + agent.Velocity * 2f);
                            var distanceSquared2 = worldPosition.GetGroundVec3().DistanceSquared(agent.Position);
                            var distanceSquared = MathF.Min(distanceSquared1, distanceSquared2);
                            if (distanceSquared < 25)
                            {
                                ++count;
                            }
                            ++amount;
                        }
                    });
                    if (amount > 0)
                    {
                        return (float)count / amount;
                    }
                }
                return 1f;
            }, 0.5f);
        }
        public void ExpireAllQueries()
        {
            _closestEnemyFormation?.Expire();
            _closestEnemyAgent?.Expire();
            _weightedAverageEnemyPosition?.Expire();
            _averageDistanceSquareWithVelocityToOrderPositionExcludingFarAgents?.Expire();
            _ratioOfAgentsNearOrderPosition.Expire();
        }
    }
}
