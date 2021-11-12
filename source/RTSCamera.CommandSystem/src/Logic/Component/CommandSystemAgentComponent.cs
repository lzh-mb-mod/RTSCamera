using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.QuerySystem;
using System;
using System.Runtime.ExceptionServices;
using System.Security;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic.Component
{
    public class CommandSystemAgentComponent : AgentComponent
    {

        public QueryData<WorldPosition> CurrentTargetPosition { get; }

        public QueryData<Vec2> PositionOfTargetAgent { get; private set; }

        public CommandSystemAgentComponent(Agent agent) : base(agent)
        {
            CurrentTargetPosition = new QueryData<WorldPosition>(() => GetTargetPosition(Agent), 0.3f);
            PositionOfTargetAgent = new QueryData<Vec2>(() => Vec2.Invalid, 2f);
        }


        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private WorldPosition GetTargetPosition(Agent agent)
        {
            try
            {
                var unit = agent;
                if (!unit.IsActive())
                    return WorldPosition.Invalid;
                var formation = unit.Formation;
                if (formation == null)
                    return WorldPosition.Invalid;
                var targetFormation = QueryDataStore.Get(formation.TargetFormation);

                Vec2 offset;
                if (QueryLibrary.IsCavalry(unit) || QueryLibrary.IsRangedCavalry(unit) &&
                    formation.FiringOrder.OrderType == OrderType.HoldFire)
                {
                    var averageOfTargetAgents = QueryDataStore.Get(formation).AverageOfTargetAgents.Value;
                    
                    offset = averageOfTargetAgents.IsValid ? formation.TargetFormation.QuerySystem.AveragePosition * 0.2f + averageOfTargetAgents * 0.8f - formation.QuerySystem.AveragePosition : Vec2.Zero;
                }
                else if (QueryLibrary.IsInfantry(unit) || QueryLibrary.IsRanged(unit) &&
                    formation.FiringOrder.OrderType == OrderType.HoldFire)
                {
                    var targetCenterAgent =
                        targetFormation.NearestAgent(formation.CurrentPosition);
                    if (targetCenterAgent == null)
                        return WorldPosition.Invalid;
                    offset = targetCenterAgent.Position.AsVec2 - formation.CurrentPosition;
                }
                else
                {
                    return WorldPosition.Invalid;
                }

                Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;

                var targetAgent = targetFormation.NearestAgent(targetPosition);
                var result = targetAgent?.GetWorldPosition() ?? WorldPosition.Invalid;
                PositionOfTargetAgent.SetValue(result.AsVec2, MBCommon.GetTotalMissionTime());
                if (targetAgent == null || !result.IsValid || result.GetNavMesh() == UIntPtr.Zero)
                {
                    result = unit.GetWorldPosition();
                    result.SetVec2(result.AsVec2 + unit.GetMovementDirection() * 0.1f);
                    return result;
                }

                if (QueryLibrary.IsInfantry(unit) || QueryLibrary.IsRanged(unit) &&
                    formation.FiringOrder.OrderType == OrderType.HoldFire)
                {
                    result.SetVec2((unit.Position.AsVec2 - result.AsVec2).Normalized() * 0.8f + result.AsVec2 +
                                   targetAgent.Velocity.AsVec2 * 1);
                }
                else if (QueryLibrary.IsCavalry(unit) || QueryLibrary.IsRangedCavalry(unit) &&
                    formation.FiringOrder.OrderType == OrderType.HoldFire)
                {
                    result.SetVec2((result.AsVec2 - unit.Position.AsVec2).Normalized() * 3f + result.AsVec2 +
                                   targetAgent.Velocity.AsVec2 * 1 + unit.Velocity.AsVec2 * 1);
                }
                else
                {
                    return WorldPosition.Invalid;
                }
                return result;
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }

            return WorldPosition.Invalid;
        }
    }
}
