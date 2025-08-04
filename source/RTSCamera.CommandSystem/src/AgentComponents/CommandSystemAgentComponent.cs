using RTSCamera.CommandSystem.QuerySystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.AgentComponents
{
    public class CommandSystemAgentComponent : AgentComponent
    {
        public float DistanceSquaredToTargetPosition = 0;
        private Timer _cachedDistanceUpdateTimer;
        public CommandSystemAgentComponent(Agent agent) : base(agent)
        {
            _cachedDistanceUpdateTimer = new Timer(agent.Mission.CurrentTime, 0.2f + MBRandom.RandomFloat * 0.1f);
        }

        public override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);

            if (Agent.Formation == null)
            {
                return;
            }

            if (!_cachedDistanceUpdateTimer.Check(Agent.Mission.CurrentTime))
            {
                return;
            }

            var query = CommandQuerySystem.GetQueryForFormation(Agent.Formation);
            if ((query?.NeedToUpdateTargetPositionDistance ?? false) == false)
            {
                return;
            }

            var worldPosition = Agent.Formation.GetOrderPositionOfUnit(Agent);
            if (worldPosition.IsValid)
            {
                var orderPosition = worldPosition.GetGroundVec3();
                var agentPosition = Agent.Position;
                var pos2SecsLater = agentPosition + Agent.Velocity * 2f;
                var vec1 = pos2SecsLater - agentPosition;
                var vec2 = orderPosition - agentPosition;
                if (vec1.LengthSquared < 0.1f)
                {
                    DistanceSquaredToTargetPosition = vec2.LengthSquared;
                    return;
                }
                var t = Vec3.DotProduct(vec1, vec2) / vec1.LengthSquared;
                if (t < 0)
                {
                    DistanceSquaredToTargetPosition = vec2.LengthSquared;
                }
                else if (t > 1)
                {
                    DistanceSquaredToTargetPosition = orderPosition.DistanceSquared(pos2SecsLater);
                }
                else
                {
                    DistanceSquaredToTargetPosition = orderPosition.DistanceSquared(agentPosition + t * vec1);
                }
            }
            else
            {
                DistanceSquaredToTargetPosition = 0;
            }
        }
    }
}
