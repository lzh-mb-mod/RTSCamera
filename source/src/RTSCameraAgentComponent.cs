using System;
using RTSCamera.QuerySystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
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
        private readonly Contour[] _colors = new Contour[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        public Vec2 CurrentDirection = Vec2.One;

        public QueryData<WorldPosition> CurrentTargetPosition { get; }

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

                    var unit = this.Agent;
                    var formation = unit.Formation;
                    if (formation == null)
                        return WorldPosition.Invalid;
                    var targetFormation = QueryDataStore.Get(formation.TargetFormation);

                    Vec2 unitPosition;

                    if (QueryLibrary.IsRangedCavalry(unit))
                    {
                        var targetAgent = unit.GetTargetAgent();
                        if (targetAgent == null || targetAgent.Formation != targetFormation.Formation)
                        {
                            unitPosition = unit.Position.AsVec2;
                            targetAgent = targetFormation.NearestAgent(unitPosition);
                        }
                        return targetAgent?.GetWorldPosition() ?? WorldPosition.Invalid;
                    }
                    if (QueryLibrary.IsCavalry(unit))
                    {
                        Agent targetAgent = null;
                        var estimatedPosition = formation.GetCurrentGlobalPositionOfUnit(unit, false);
                        unitPosition = estimatedPosition * 0.2f +
                                       unit.Position.AsVec2 * 0.8f;
                        if (targetAgent == null || targetAgent.Formation != targetFormation.Formation)
                            targetAgent = targetFormation.NearestOfAverageOfNearestPosition(unitPosition, 10);
                        if (targetAgent != null)
                        {
                            if (targetAgent.HasMount)
                            {
                                return targetAgent.GetWorldPosition();
                            }

                            var targetPosition = targetAgent.GetWorldPosition();
                            var targetDirection = targetPosition.AsVec2 - unit.Position.AsVec2;
                            var distance = targetDirection.Normalize();
                            var result = targetPosition;
                            var targetDepth = targetFormation.Formation.Depth;

                            // new
                            if (distance > 5)
                            {
                                if (targetDirection.DotProduct(CurrentDirection) > 0)
                                {
                                    CurrentDirection = targetDirection;
                                    result.SetVec2(CurrentDirection * 10 + targetPosition.AsVec2);
                                }
                                else
                                //if (distance > 20)
                                //{
                                //    CurrentDirection = targetDirection;
                                //    result.SetVec2(targetDirection * 10 + targetPosition.AsVec2);
                                //}
                                //else if (distance > 5 && targetDirection.DotProduct(CurrentDirection) < 0)
                                {
                                    var halfWidth = formation.Width / 2;
                                    var halfDepth = formation.Depth / 2;
                                    var formationCenter = formation.QuerySystem.AveragePosition;
                                    var formationDirection = formation.QuerySystem.EstimatedDirection;
                                    var leftFront =
                                        formationDirection.TransformToParentUnitF(new Vec2(-halfWidth, halfDepth));
                                    var rightFront =
                                        formationDirection.TransformToParentUnitF(new Vec2(halfWidth, halfDepth));
                                    var leftBack =
                                        formationDirection.TransformToParentUnitF(new Vec2(-halfWidth, -halfDepth));
                                    var rightBack =
                                        formationDirection.TransformToParentUnitF(new Vec2(halfWidth, -halfDepth));
                                    var addedStopDistance =
                                        CurrentDirection.DotProduct(estimatedPosition - formationCenter) * 1.5f +
                                        Math.Max(MathF.Max(-CurrentDirection.DotProduct(leftFront),
                                                -CurrentDirection.DotProduct(rightFront),
                                                -CurrentDirection.DotProduct(leftBack)),
                                            -CurrentDirection.DotProduct(rightBack));

                                    var remainingStopDistance = CurrentDirection.DotProduct(targetDirection * distance) + 20 +
                                                       addedStopDistance;
                                    // arrive
                                    if (remainingStopDistance < 5)
                                    {
                                        CurrentDirection = targetDirection;
                                        result.SetVec2(targetDirection * (10 + targetDepth) + targetPosition.AsVec2);
                                    }
                                    else
                                    {
                                        result.SetVec2(remainingStopDistance * CurrentDirection + unit.Position.AsVec2);
                                    }
                                }
                            }
                            else
                            {
                                result.SetVec2(CurrentDirection * (10 + targetDepth) + targetPosition.AsVec2);
                            }


                            // old
                            //if (distance < 3)
                            //{
                            //    result = unit.GetWorldPosition();
                            //    result.SetVec2(CurrentDirection * 20 + result.AsVec2);
                            //}
                            //else
                            //{
                            //    if (distance < 20 && targetDirection.DotProduct(CurrentDirection) < 0)
                            //    {
                            //        result.SetVec2(-targetDirection * 50 + result.AsVec2);
                            //    }
                            //    else
                            //    {
                            //        CurrentDirection = targetDirection;
                            //        result.SetVec2(targetDirection * 10 + result.AsVec2);
                            //    }

                            //}


                            return result.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(result.AsVec2) ? targetPosition : result;
                        }

                        return WorldPosition.Invalid;
                    }
                    else
                    {
                        var targetAgent = unit.GetTargetAgent();
                        if (targetAgent == null || targetAgent.Formation != targetFormation.Formation)
                        {
                            unitPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) * 0.2f +
                                           unit.Position.AsVec2 * 0.8f;
                            targetAgent = targetFormation.NearestAgent(unitPosition);
                        }
                        return targetAgent?.GetWorldPosition() ?? WorldPosition.Invalid;
                    }
                }
                catch (Exception e)
                {
                    Utility.DisplayMessage(e.ToString());
                }

                return WorldPosition.Invalid;
            }, 0.2f);
        }

        public void SetContourColor(int level, uint? color, bool alwaysVisible)
        {
            if (SetContourColorWithoutUpdate(level, color, alwaysVisible))
            {
                _currentLevel = color.HasValue ? level : EffectiveLevel(level - 1);
                SetColor();
            }
        }

        protected override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);
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

        public void ClearContourColor()
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i].Color = null;
                Agent.AgentVisuals?.SetContourColor(null);
                if (Agent.HasMount)
                    Agent.MountAgent.AgentVisuals?.SetContourColor(null);
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

        protected override void OnMount(Agent mount)
        {
            base.OnMount(mount);

            mount.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
        }

        protected override void OnDismount(Agent mount)
        {
            base.OnDismount(mount);

            mount.AgentVisuals?.SetContourColor(new uint?());
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

        private void SetColor()
        {
            Agent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
            if (Agent.HasMount)
                Agent.MountAgent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
        }
    }
}
