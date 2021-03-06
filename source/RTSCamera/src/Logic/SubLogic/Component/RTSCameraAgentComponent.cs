﻿using System;
using System.Runtime.ExceptionServices;
using MissionSharedLibrary.Utilities;
using RTSCamera.QuerySystem;
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
        private readonly Contour[] _colors = new Contour[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        public Vec2 CurrentDirection;

        public QueryData<WorldPosition> CurrentTargetPosition { get; }

        public QueryData<Agent> TargetAgent { get; }

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
                        offset = targetFormation.Formation.CurrentPosition - formation.CurrentPosition;
                    }
                    else if (QueryLibrary.IsInfantry(unit) || QueryLibrary.IsRanged(unit) &&
                        formation.FiringOrder.OrderType == OrderType.HoldFire)
                    {
                        var targetAgent =
                            targetFormation.NearestAgent(formation.CurrentPosition);
                        if (targetAgent == null)
                            return WorldPosition.Invalid;
                        offset = targetAgent.Position.AsVec2 - formation.CurrentPosition;
                    }
                    else
                    {
                        return WorldPosition.Invalid;
                    }

                    Vec2 targetPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true) + offset;
                    var result = targetFormation.NearestAgent(targetPosition)?.GetWorldPosition() ??
                                 WorldPosition.Invalid;
                    return !result.IsValid || result.GetNavMesh() == UIntPtr.Zero
                        ? unit.GetWorldPosition()
                        : result;
                }
                catch (AccessViolationException e)
                {
                    Utility.DisplayMessage(e.ToString());
                }
                catch (Exception e)
                {
                    Utility.DisplayMessage(e.ToString());
                }

                return WorldPosition.Invalid;
            }, 0.3f);
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
