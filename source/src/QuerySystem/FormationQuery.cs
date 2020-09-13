using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public QueryData<KdTree<float, PointInfo<AgentPointInfo>>> KdTree { get; }
        public QueryData<ClusterSet<AgentPointInfo>> Clusters { get; }
        public QueryData<Vec2> TargetPosition { get; }

        private readonly KdTree<float, PointInfo<AgentPointInfo>> _targetClusterKdTree = new KdTree<float, PointInfo<AgentPointInfo>>(2, new FloatMath(), AddDuplicateBehavior.Skip);

        public Agent NearestAgent(Vec2 position, bool refresh = false)
        {
            if (refresh)
            {
                KdTree.Expire();
            }
            var result = KdTree.Value.GetNearestNeighbours(new float[2] { position.x, position.y }, 1);
            return result.Length == 0 ? null : result[0].Value.Item.Agent;
        }

        public FormationQuery(Formation formation)
        {
            Formation = formation;

            KdTree = new QueryData<KdTree<float, PointInfo<AgentPointInfo>>>(() =>
            {
                var tree = new KdTree<float, PointInfo<AgentPointInfo>>(2, new FloatMath(), AddDuplicateBehavior.Skip);
                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent.IsActive())
                        AddAgent(tree, agent);
                });
                return tree;
            }, 0.23f);

            Clusters = new QueryData<ClusterSet<AgentPointInfo>>(
                () => DBSCAN.DBSCAN.CalculateClusters(new KdTreeSpatialIndex(KdTree.Value), 5, 3),
                0.29f);

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
                        var clusters = targetFormation.Clusters.Value;
                        float bestScore = 0;
                        Cluster<AgentPointInfo> bestCluster = null;
                        foreach (var cluster in clusters.Clusters)
                        {
                            var targetAveragePosition = Average(cluster.Objects);
                            var distance = targetAveragePosition.Distance(averagePosition);
                            distance = MathF.Clamp(distance, 1, 1000);
                            var score = cluster.Objects.Count / distance;
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestCluster = cluster;
                            }
                        }

                        KdTree<float, PointInfo<AgentPointInfo>> kdTree = null;
                        if (bestCluster != null)
                        {
                            _targetClusterKdTree.Clear();
                            foreach (var point in bestCluster.Objects)
                            {
                                if (point.Agent.IsActive())
                                    AddAgent(_targetClusterKdTree, point.Agent);
                            }

                            kdTree = _targetClusterKdTree;
                        }
                        else
                        {
                            Utility.DisplayMessage("no cluster");
                            kdTree = targetFormation.KdTree.Value;
                        }

                        if (kdTree.Count == 0)
                            return Vec2.Invalid;
                        var nearestNeighbors =
                            kdTree.GetNearestNeighbours(new float[2] {averagePosition.x, averagePosition.y}, 1);
                        if (nearestNeighbors.Length == 0)
                            return Vec2.Invalid;

                        var nearestPoint = nearestNeighbors[0].Point;
                        var pointsAround = targetFormation.KdTree.Value.RadialSearch(nearestPoint, 5);
                        
                        return Average(pointsAround);
                    }
                }

                return Vec2.Invalid;
            }, 0.29f);
        }

        private static void AddAgent(KdTree<float, PointInfo<AgentPointInfo>> tree, Agent agent)
        {
            tree.Add(new float[2] {agent.Position.x, agent.Position.y},
                new PointInfo<AgentPointInfo>(new AgentPointInfo(agent)));
        }

        private static Vec2 Average(KdTreeNode<float, PointInfo<AgentPointInfo>>[] points)
        {
            Vec2 result = Vec2.Zero;
            foreach (var point in points)
            {
                result.x += (float)point.Point[0];
                result.y += (float)point.Point[1];
            }

            if (points.Length != 0)
            {
                result *= 1.0f / points.Length;
            }

            return result;
        }

        private static Vec2 Average(ICollection<AgentPointInfo> points)
        {
            Vec2 result = Vec2.Zero;
            foreach (var point in points)
            {
                result.x += (float) point.Point.X;
                result.y += (float) point.Point.Y;
            }

            if (points.Count != 0)
            {
                result *= 1.0f / points.Count;
            }

            return result;
        }

        private static Vec2 Average(IEnumerable<Vec2> points)
        {
            Vec2 result = Vec2.Zero;
            int count = 0;
            foreach (var point in points)
            {
                ++count;
                result += point;
            }

            if (count != 0)
            {
                result *= 1.0f / count;
            }

            return result;
        }
    }
}
