using System.Collections;
using System.Collections.Generic;
using DBSCAN;
using DBSCAN.RBush;
using KdTree;
using KdTree.Math;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class FormationQuery
    {
        public Formation Formation { get; }

        public QueryData<KdTree<float, Agent>> KdTree { get; }
        public QueryData<Vec2> TargetPosition { get; }

        public Agent NearestAgent(Vec2 position, bool refresh = false)
        {
            if (refresh)
            {
                KdTree.Expire();
            }
            var result = KdTree.Value.GetNearestNeighbours(new float[2] { position.x, position.y }, 1);
            return result.Length == 0 ? null : result[0].Value;
        }

        public FormationQuery(Formation formation)
        {
            Formation = formation;

            KdTree = new QueryData<KdTree<float, Agent>>(() =>
            {
                var tree = new KdTree<float, Agent>(2, new GeoMath(), AddDuplicateBehavior.Skip);
                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent.IsActive())
                        tree.Add(new float[2] {agent.Position.x, agent.Position.y}, agent);
                });
                return tree;
            }, 0.23f);

            TargetPosition = new QueryData<Vec2>(() =>
            {
                if (formation.MovementOrder.OrderType == OrderType.ChargeWithTarget)
                {
                    if (formation.TargetFormation == null)
                    {
                        Utility.DisplayMessage("Error: Unexpected null target formation.");
                        return Vec2.Invalid;
                    }
                    else if (formation.TargetFormation.CountOfUnits > 0)
                    {
                        var averagePosition = formation.QuerySystem.AveragePosition;
                        var targetFormation = QueryDataStore.Get(formation.TargetFormation);
                        var nearestNeighbors = targetFormation.KdTree.Value
                            .GetNearestNeighbours(new float[2] { averagePosition.x, averagePosition.y }, 1);
                        if (nearestNeighbors.Length == 0)
                            return Vec2.Invalid;

                        var nearestPoint = nearestNeighbors[0].Point;
                        var pointsAround = targetFormation.KdTree.Value.RadialSearch(nearestPoint, 5);
                        var result = Vec2.Zero;
                        foreach (var point in pointsAround)
                        {
                            result.x += point.Point[0];
                            result.y += point.Point[1];
                        }

                        result.x /= pointsAround.Length;
                        result.y /= pointsAround.Length;
                        return result;
                    }
                }

                return Vec2.Invalid;
            }, 0.29f);
        }
    }
}
