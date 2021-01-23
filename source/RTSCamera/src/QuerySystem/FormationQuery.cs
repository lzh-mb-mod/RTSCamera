using KdTree;
using KdTree.Math;
using System.Collections.Generic;
using System.Linq;
using RTSCamera.Logic.SubLogic.Component;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class FormationQuery
    {
        public Formation Formation { get; }

        public QueryData<KdTree<float, AgentPointInfo>> CurrentPositionKdTree { get; }

        public QueryData<KdTree<float, AgentTargetPointInfo>> TargetPositionKdTree { get; }

        public QueryData<bool> IsEngaged { get; }

        public Vec2 PositionOffset { get; set; }

        public Agent NearestAgent(Vec2 position, bool refresh = false)
        {
            if (refresh)
            {
                CurrentPositionKdTree.Expire();
            }
            var result = CurrentPositionKdTree.Value.GetNearestNeighbours(new float[2] { position.x, position.y }, 1);
            return result.Length == 0 ? null : result[0].Value.Agent;
        }
        public List<AgentPointInfo> NearestAgents(Vec2 position, int count, bool refresh = false)
        {
            if (refresh)
            {
                CurrentPositionKdTree.Expire();
            }
            var result = CurrentPositionKdTree.Value.GetNearestNeighbours(new float[2] { position.x, position.y }, count);
            return result.Select(node => node.Value).ToList();
        }

        public Agent NearestOfAverageOfNearestPosition(Vec2 position, int count)
        {
            var agents = CurrentPositionKdTree.Value.GetNearestNeighbours(new float[2] {position.x, position.y}, count);
            var averagePosition = Average(agents);
            var nearest = CurrentPositionKdTree.Value.GetNearestNeighbours(new float[2] {averagePosition.x, averagePosition.y}, 1);
            return nearest.Length == 0 ? null : nearest[0].Value.Agent;
        }

        public List<AgentTargetPointInfo> NearestTargetPositions(Vec2 position, int count)
        {
            var nodes = TargetPositionKdTree.Value.GetNearestNeighbours(new float[2] {position.x, position.y}, count);
            return nodes.Select(node => node.Value).ToList();
        }

        public void RemoveTargetPosition(Vec2 position)
        {
            TargetPositionKdTree.Value.RemoveAt(new float[2] {position.x, position.y});
        }

        public void AddTargetPosition(Vec2 position, Agent agent, Vec2 resistanceDirection)
        {
            TargetPositionKdTree.GetCachedValue()
                .Add(new float[2] {position.x, position.y}, new AgentTargetPointInfo(agent, position, resistanceDirection));
        }

        public FormationQuery(Formation formation)
        {
            Formation = formation;

            CurrentPositionKdTree = new QueryData<KdTree<float, AgentPointInfo>>(() =>
            {
                var tree = new KdTree<float, AgentPointInfo>(2, new FloatMath(), AddDuplicateBehavior.Skip);
                Formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent.IsActive())
                        AddAgent(tree, agent);
                });
                return tree;
            }, 0.2f);

            TargetPositionKdTree = new QueryData<KdTree<float, AgentTargetPointInfo>>(() =>
            {
                var tree = new KdTree<float, AgentTargetPointInfo>(2, new FloatMath(), AddDuplicateBehavior.Skip);
                Formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent.IsActive())
                        AddAgentTargetPosition(tree, agent);
                });
                return tree;
            }, 1f);

            IsEngaged = new QueryData<bool>(() =>
            {
                bool isEngaged = false;
                float nearestDistance = float.MaxValue;
                Vec2 nearestDistanceDiff = Vec2.Zero;
                Formation.ApplyActionOnEachUnit(agent =>
                {
                    if (isEngaged)
                        return;
                    var targetFormationQuery = QueryDataStore.Get(Formation.TargetFormation);
                    var nearestAgent = targetFormationQuery.NearestAgent(agent.Position.AsVec2);
                    if (nearestAgent == null)
                        return;
                    var diff = nearestAgent.Position - agent.Position;
                    var distance = diff.Length;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestDistanceDiff = diff.AsVec2;
                    }
                    if (distance < 2)
                    {
                        isEngaged = true;
                    }
                });

                PositionOffset = isEngaged ? Vec2.Zero : nearestDistanceDiff;
                return isEngaged;
            }, 0.21f);

            PositionOffset = Vec2.Zero;

            //PositionOffset = new QueryData<Vec2>(() =>
            //{
            //    if (Formation.TargetFormation == null)
            //    {
            //        return Vec2.Zero;
            //    }

            //    var targetFormation = QueryDataStore.Get(Formation.TargetFormation);
            //    return targetFormation.NearestOfAverageOfNearestPosition(formation.CurrentPosition, 7).Position
            //        .AsVec2 - formation.CurrentPosition;
            //}, 0.2f);
        }

        private static void AddAgent(KdTree<float, AgentPointInfo> tree, Agent agent)
        {
            tree.Add(new float[2] {agent.Position.x, agent.Position.y},
                new AgentPointInfo(agent));
        }

        private static void AddAgentTargetPosition(KdTree<float, AgentTargetPointInfo> tree, Agent agent)
        {
            var component = agent.GetComponent<RTSCameraAgentComponent>();
            if (component != null)
            {
                var oldTargetPosition = component.OldTargetPosition;
                if (oldTargetPosition.IsValid)
                {
                    tree.Add(new float[2] {oldTargetPosition.x, oldTargetPosition.y},
                        new AgentTargetPointInfo(agent, oldTargetPosition, component.ResistanceDirection));
                    return;
                }

                component.OldTargetPosition = agent.Position.AsVec2;
                component.ResistanceDirection = Vec2.Zero; 
            }

            tree.Add(new float[2] { agent.Position.x, agent.Position.y },
                new AgentTargetPointInfo(agent, Vec2.Zero));
        }

        private static Vec2 Average(KdTreeNode<float, AgentPointInfo>[] points)
        {
            Vec2 result = Vec2.Zero;
            foreach (var point in points)
            {
                result.x += point.Point[0];
                result.y += point.Point[1];
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
                result.x += point.Point.X;
                result.y += point.Point.Y;
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
