using RTSCamera.QuerySystem;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic.Component
{
    public struct Contour
    {
        public Contour(uint? color, bool alwaysVisible)
        {
            Color = color;
            AlwaysVisible = alwaysVisible;
        }
        public uint? Color;
        public bool AlwaysVisible;
    }

    public enum ColorLevel
    {
        TargetFormation,
        SelectedFormation,
        MouseOverFormation,
        TargetAgent,
        SelectedAgent,
        MouseOverAgent,
        NumberOfLevel
    }

    public class RTSCameraAgentComponent : AgentComponent
    {
        private readonly MethodInfo GetNeighbourUnit = typeof(Formation).GetMethod("GetNeighbourUnit", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Contour[] _colors = new Contour[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        public Vec2 OldTargetPosition { get; set; } = Vec2.Invalid;

        public Vec2 ResistanceDirection { get; set; } = Vec2.Zero;

        public void SetOldTargetPosition(Vec2 pos, Vec2 resistanceDirection)
        {
            var selfFormationQuery = QueryDataStore.Get(Agent.Formation);
            selfFormationQuery.RemoveTargetPosition(OldTargetPosition);
            OldTargetPosition = pos;
            ResistanceDirection = resistanceDirection;
            selfFormationQuery.AddTargetPosition(OldTargetPosition, Agent, resistanceDirection);
        }

        public QueryData<WorldPosition> CurrentTargetPosition { get; }

        //public static float offset = 2.5f;
        //public static float radiusFactor = 0.88f;
        //public static float offset = 2.5f;
        //public static float radiusFactor = 1.33f;
        //public static float offsetFactor = 1.79f;
        //public static float radiusFactor = 0.8f;
        //public static int countThreshold = 8;

        public RTSCameraAgentComponent(Agent agent) : base(agent)
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i] = new Contour(null, false);
            }

            CurrentTargetPosition = new QueryData<WorldPosition>(() =>
            {
                try
                {
                    var unit = Agent;
                    var formation = unit.Formation;
                    if (formation == null)
                        return WorldPosition.Invalid;
                    var targetFormation = QueryDataStore.Get(formation.TargetFormation);
                    var selfFormationQuery = QueryDataStore.Get(formation);

                    if (QueryLibrary.IsRangedCavalry(unit) && formation.FiringOrder.OrderType == OrderType.FireAtWill)
                    {
                        var targetAgent = unit.GetTargetAgent();
                        if (targetAgent == null || targetAgent.Formation != formation.TargetFormation)
                        {
                            Vec2 unitPosition = unit.Position.AsVec2;
                            targetAgent = targetFormation.NearestAgent(unitPosition);
                        }

                        return targetAgent?.GetWorldPosition() ?? new WorldPosition();
                    }

                    Vec2 offset;
                    if (QueryLibrary.IsCavalry(unit) || QueryLibrary.IsRangedCavalry(unit) && formation.FiringOrder.OrderType == OrderType.HoldFire)
                    {
                        var offset = targetFormation.Formation.CurrentPosition - formation.CurrentPosition;
                        Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                        return targetFormation.NearestAgent(targetPosition)?.GetWorldPosition() ?? WorldPosition.Invalid;
                    }
                    else
                    {
                        _ = selfFormationQuery.TargetPositionKdTree.Value;
                        var targetAgent = targetFormation.NearestAgent(OldTargetPosition);
                        var targetAgentPos = targetAgent.Position.AsVec2;
                        Vec2 diffOldTargetPosToTargetEnemy = targetAgentPos - OldTargetPosition;
                        var distanceOldTargetPosToTargetEnemy = diffOldTargetPosToTargetEnemy.Normalize();
                        Vec2 offset = Vec2.Zero;
                        //float averageOffsetAlongFowardFromFront = 0;
                        //int numberOfFront = 0;
                        var targetPositions = selfFormationQuery.NearestTargetPositions(OldTargetPosition, 6);
                        var resistanceDirection = distanceOldTargetPosToTargetEnemy < 1.5f
                            ? diffOldTargetPosToTargetEnemy
                            : Vec2.Zero;
                        if (targetPositions.Count > 1)
                        {
                            var self = targetPositions.Find(info => info.Agent == unit);
                            if (self == null)
                            {
                                var selfPos = unit.GetWorldPosition();
                                SetOldTargetPosition(selfPos.AsVec2, Vec2.Zero);
                                throw new Exception("Warn: Unexpected case happened.");
                                //return result;
                            }

                            foreach (var targetPosition in targetPositions)
                            {
                                if (targetPosition != self)
                                {
                                    var diff = targetPosition.Point - self.Point;
                                    var length = diff.Normalize();
                                    if (length > 2.0f)
                                        break;
                                    //var cos = diff.DotProduct(diffOldTargetPosToTargetEnemy);
                                    //if (cos > 0.5f)
                                    //{
                                    //    averageOffsetAlongFowardFromFront += cos * (1.2f - length);
                                    //    ++numberOfFront;
                                    //}


                                    if (distanceOldTargetPosToTargetEnemy >= 1.5f)
                                    {
                                        resistanceDirection += Math.Max(diff.DotProduct(targetPosition.ResistanceDirection), 0f) * diff;
                                    }
                                    //if (MathF.Abs(cos) < 0.5f)
                                    //{
                                    //    offset += diff * (length - 1.2f);
                                    //}
                                    //else
                                    {
                                        offset += diff * MathF.Clamp(length - 1.2f, -1.2f, 0);/* * MathF.Clamp(diff.DotProduct(diffOldTargetPosToTargetEnemy) + 1, 0.5f, 1f)*/;
                                    }
                                }
                            }
                        }

                        if (resistanceDirection.Length > 0)
                        {
                            resistanceDirection.Normalize();
                        }

                        //if (numberOfFront > 0)
                        //    averageOffsetAlongFowardFromFront *= 1.0f / (numberOfFront);
                        offset += diffOldTargetPosToTargetEnemy *
                                  MathF.Clamp((distanceOldTargetPosToTargetEnemy - 1.2f), -1.2f, 1.2f) *
                                  (distanceOldTargetPosToTargetEnemy < 1.2f
                                      ? (targetPositions.Count + 1)
                                      : resistanceDirection.Length > 0
                                          ? 1 - resistanceDirection.DotProduct(diffOldTargetPosToTargetEnemy) * 0.9f
                                          : (targetPositions.Count + 1) * distanceOldTargetPosToTargetEnemy / 2);
                        //: MathF.Clamp((1f - 10 * averageOffsetAlongFowardFromFront), 0.2f, 1))

                        offset *= 1.0f / (targetPositions.Count + 1);
                        //var unitPos = unit.Position.AsVec2;
                        //var diffOldTargetPosToUnitPos = unitPos - OldTargetPosition;
                        //var distanceOldTargetPosToUnitPos = diffOldTargetPosToUnitPos.Normalize();
                        //var unitPosToTargetEnemy = (targetAgentPos - unitPos).Normalized();
                        //offset += diffOldTargetPosToUnitPos * Math.Max(distanceOldTargetPosToUnitPos, 1) *
                        //          (1 - diffOldTargetPosToTargetEnemy.DotProduct(unitPosToTargetEnemy));
                        //if (offset.Length > 0.5f)
                            SetOldTargetPosition(OldTargetPosition + offset, resistanceDirection);

                        var result = unit.GetWorldPosition();
                        result.SetVec2(OldTargetPosition);
                        if (result.GetNavMesh() == UIntPtr.Zero ||
                            !Mission.Current.IsPositionInsideBoundaries(result.AsVec2))
                        {
                            var cachedValue = CurrentTargetPosition.GetCachedValue();
                            return cachedValue.IsValid ? cachedValue : unit.GetWorldPosition();
                        }
                        else
                            return result;
                        //var agents = selfFormationQuery.NearestAgents(unit.Position.AsVec2, 5);
                        //if (agents.Count > 1)
                        //{
                        //    var self = agents.Find(info => info.Agent == unit);
                        //    if (self == null)
                        //        return WorldPosition.Invalid;
                        //    Vec2 offset = Vec2.Zero;
                        //    foreach (var agentPointInfo in agents)
                        //    {
                        //        if (agentPointInfo != self)
                        //        {
                        //            var diff = agentPointInfo.Point - self.Point;
                        //            if (diff.Length > 5)
                        //                break;
                        //            var length = diff.Normalize();
                        //            offset += diff * (length - 5f);
                        //        }
                        //    }

                        //    offset *= 1.0f / (agents.Count);
                        //    var result = unit.GetWorldPosition();
                        //    result.SetVec2(result.AsVec2 + offset);
                        //    return result.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(result.AsVec2) ? unit.GetWorldPosition() : result;
                        //}
                        //return unit.GetWorldPosition();
                        //switch (formation.ArrangementOrder.OrderEnum)
                        //{
                        //    case ArrangementOrder.ArrangementOrderEnum.Line:
                        //    case ArrangementOrder.ArrangementOrderEnum.Loose:
                        //    case ArrangementOrder.ArrangementOrderEnum.Scatter:
                        //    case ArrangementOrder.ArrangementOrderEnum.Skein:
                        //        {
                        //            var formationUnit = unit as IFormationUnit;
                        //            var rankUnit = formationUnit.FormationRankIndex;
                        //            if (rankUnit > 0)
                        //            {
                        //                var frontAgent = (Agent)GetNeighbourUnit?.Invoke(formation, new object[]
                        //                {
                        //            unit, 0, -1
                        //                });
                        //                if (frontAgent != null)
                        //                {
                        //                    var offsetToFrontAgent = formation.GetCurrentGlobalPositionOfUnit(frontAgent, true) -
                        //                                 formation.GetCurrentGlobalPositionOfUnit(unit, true);
                        //                    var result1 = frontAgent.GetComponent<RTSCameraAgentComponent>().CurrentTargetPosition.Value;
                        //                    result1.SetVec2(result1.AsVec2 - offsetToFrontAgent);
                        //                    return result1.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(result1.AsVec2) ? unit.GetWorldPosition() : result1;
                        //                }
                        //            }
                        //            var offset = selfFormationQuery.PositionOffset.Value;
                        //            Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                        //            var enemyAtTargetPosition = targetFormation.NearestAgent(targetPosition);
                        //            var selfPosition = unit.Position;
                        //            var targetEnemy = unit.GetTargetAgent();
                        //            if (targetEnemy.Formation != formation.TargetFormation)
                        //            {
                        //                var enemyNearest = targetFormation.NearestAgent(unit.Position.AsVec2);
                        //                if (enemyNearest == null)
                        //                    return enemyAtTargetPosition?.GetWorldPosition() ?? WorldPosition.Invalid;
                        //                var enemyNearestDistance = enemyNearest.Position.Distance(selfPosition);
                        //                var enemyAtTargetPositionDistance = enemyAtTargetPosition.Position.Distance(selfPosition);
                        //                targetEnemy = enemyNearestDistance < 3 || enemyNearestDistance < enemyAtTargetPositionDistance * 0.2f
                        //                    ? enemyNearest
                        //                    : enemyAtTargetPosition;
                        //            }
                        //            var enemyDirection = (targetEnemy.Position - selfPosition).NormalizedCopy();
                        //            var result = targetEnemy.GetWorldPosition();
                        //            result.SetVec2(result.AsVec2 - enemyDirection.AsVec2 * 1f);
                        //            return result.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(result.AsVec2) ? unit.GetWorldPosition() : result;
                        //        }
                        //    default:
                        //        {
                        //            var offset = selfFormationQuery.PositionOffset.Value;
                        //            Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                        //            var enemyAtTargetPosition = targetFormation.NearestAgent(targetPosition);
                        //            var selfPosition = unit.Position;
                        //            var targetEnemy = unit.GetTargetAgent();
                        //            if (targetEnemy.Formation != formation.TargetFormation)
                        //            {
                        //                var enemyNearest = targetFormation.NearestAgent(unit.Position.AsVec2);
                        //                if (enemyNearest == null)
                        //                    return enemyAtTargetPosition?.GetWorldPosition() ?? WorldPosition.Invalid;
                        //                var enemyNearestDistance = enemyNearest.Position.Distance(selfPosition);
                        //                var enemyAtTargetPositionDistance =
                        //                    enemyAtTargetPosition.Position.Distance(selfPosition);
                        //                targetEnemy = enemyNearestDistance < 3 ||
                        //                              enemyNearestDistance < enemyAtTargetPositionDistance * 0.2f
                        //                    ? enemyNearest
                        //                    : enemyAtTargetPosition;
                        //            }
                        //            var enemyDirection = (targetEnemy.Position - selfPosition).NormalizedCopy();
                        //            var result = targetEnemy.GetWorldPosition();
                        //            result.SetVec2(result.AsVec2 - enemyDirection.AsVec2 * 1f + targetEnemy.GetCurrentVelocity() * 0.5f);
                        //            return result.GetNavMesh() == UIntPtr.Zero ||
                        //                   !Mission.Current.IsPositionInsideBoundaries(result.AsVec2)
                        //                ? unit.GetWorldPosition()
                        //                : result;
                        //        }
                        //    case ArrangementOrder.ArrangementOrderEnum.Circle:
                        //    case ArrangementOrder.ArrangementOrderEnum.ShieldWall:
                        //    case ArrangementOrder.ArrangementOrderEnum.Square:
                        //        {
                        //            var offset = selfFormationQuery.PositionOffset.Value;
                        //            Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                        //            var result = unit.GetWorldPosition();
                        //            result.SetVec2(targetPosition);
                        //            return result.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(result.AsVec2) ? unit.GetWorldPosition() : result;
                        //        }
                        //}
                    }
                    //else
                    //{
                    //    var offset = selfFormationQuery.PositionOffset.Value;
                    //    Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                    //    var enemyAtTargetPosition = targetFormation.NearestAgent(targetPosition);
                    //    var selfPosition = unit.Position;
                    //    var targetEnemy = unit.GetTargetAgent();
                    //    if (targetEnemy.Formation != formation.TargetFormation)
                    //    {
                    //        var enemyNearest = targetFormation.NearestAgent(unit.Position.AsVec2);
                    //        if (enemyNearest == null)
                    //            return enemyAtTargetPosition?.GetWorldPosition() ?? WorldPosition.Invalid;
                    //        var enemyNearestDistance = enemyNearest.Position.Distance(selfPosition);
                    //        var enemyAtTargetPositionDistance = enemyAtTargetPosition.Position.Distance(selfPosition);
                    //        targetEnemy = enemyNearestDistance < 3 || enemyNearestDistance < enemyAtTargetPositionDistance * 0.2f
                    //            ? enemyNearest
                    //            : enemyAtTargetPosition;
                    //    }
                    //    var enemyDirection = (targetEnemy.Position - selfPosition).NormalizedCopy();
                    //    var detectionPoint =
                    //        (enemyDirection * offsetFactor * (formation.Distance + formation.UnitDiameter)).AsVec2 + unit.Position.AsVec2;
                    //    var nearByAgents = selfFormationQuery.NearestAgents(detectionPoint, countThreshold);
                    //    if (nearByAgents.Count() >= countThreshold &&
                    //        detectionPoint.Distance(nearByAgents[nearByAgents.Count - 1].Point) <=
                    //        radiusFactor * (formation.Interval + formation.UnitDiameter))
                    //    {
                    //        var unitPosition = unit.GetWorldPosition();
                    //        unitPosition.SetVec2(unitPosition.AsVec2 - enemyDirection.AsVec2 * 1f);
                    //        return unitPosition;
                    //    }

                    //    var result = targetEnemy.GetWorldPosition();
                    //    result.SetVec2(result.AsVec2 - enemyDirection.AsVec2 * 1f);
                    //    return result;
                    //}
                }
                catch (Exception e)
                {
                    Utility.DisplayMessage(e.ToString());
                }

                return WorldPosition.Invalid;
            }, 0.1f);

        }

        protected override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);

            //if (Agent.Formation != null && Agent.Formation.MovementOrder.OrderType == OrderType.ChargeWithTarget &&
            //    Agent.Formation.TargetFormation != null)
            //{
            //    _ = CurrentTargetPosition.Value;
            //}
        }

        public void SetContourColor(int level, uint? color, bool alwaysVisible)
        {
            if (SetContourColorWithoutUpdate(level, color, alwaysVisible))
            {
                _currentLevel = color.HasValue ? level : EffectiveLevel(level - 1);
                SetColor();
            }
        }

        public bool SetContourColorWithoutUpdate(int level, uint? color, bool alwaysVisible)
        {
            if (level < 0 || level >= _colors.Length)
                return false;
            if (_colors[level].Color == color)
                return false;
            _colors[level].Color = color;
            _colors[level].AlwaysVisible = alwaysVisible;
            return _currentLevel <= level; // needs update.
        }

        public void UpdateColor()
        {
            _currentLevel = EffectiveLevel();
            SetColor();
        }

        [HandleProcessCorruptedStateExceptions]
        public void ClearContourColor()
        {
            try
            {
                for (int i = 0; i < _colors.Length; ++i)
                {
                    _colors[i].Color = null;
                }

                Agent.AgentVisuals?.SetContourColor(null);
                if (Agent.HasMount)
                    Agent.MountAgent.AgentVisuals?.SetContourColor(null);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        public void ClearTargetOrSelectedFormationColor()
        {
            bool needUpdate = SetContourColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            if (needUpdate)
                UpdateColor();
        }

        public void ClearFormationColor()
        {
            bool needUpdate = SetContourColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.MouseOverFormation, null, true);
            if (needUpdate)
                UpdateColor();
        }

        [HandleProcessCorruptedStateExceptions]
        protected override void OnMount(Agent mount)
        {
            base.OnMount(mount);

            try
            {
                mount.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        [HandleProcessCorruptedStateExceptions]
        protected override void OnDismount(Agent mount)
        {
            base.OnDismount(mount);

            try
            {
                mount.AgentVisuals?.SetContourColor(null);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        protected override void OnStopUsingGameObject()
        {
            base.OnStopUsingGameObject();

            if (Agent.Controller == Agent.ControllerType.Player)
            {
                Agent.DisableScriptedMovement();
                Agent.AIUseGameObjectEnable(false);
                Agent.AIMoveToGameObjectDisable();
                Agent.SetScriptedFlags(Agent.GetScriptedFlags() & ~Agent.AIScriptedFrameFlags.NoAttack);
            }
        }

        private int EffectiveLevel(int maxLevel = (int)ColorLevel.NumberOfLevel - 1)
        {
            for (int i = maxLevel; i > -1; --i)
            {
                if (_colors[i].Color.HasValue)
                    return i;
            }

            return -1;
        }

        [HandleProcessCorruptedStateExceptions]
        private void SetColor()
        {
            try
            {
                Agent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
                if (Agent.HasMount)
                    Agent.MountAgent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }
    }
}
