using System;
using System.Security;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCameraAgentComponent
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

    public class RTSCameraComponent : AgentComponent
    {
        private readonly Contour[] _colors = new Contour[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        private bool _shouldUpdateColor = false;

        private bool _shouldClearColorOnRemove = false;

        private bool _agentLeavingMission;

        public delegate void OnComponentRemovedDelegate(RTSCameraComponent component);

        public event OnComponentRemovedDelegate OnComponentRemovedEvent;

        public RTSCameraComponent(Agent agent) : base(agent)
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i] = new Contour(null, false);
            }
        }

        public void SetContourColor(int level, uint? color, bool alwaysVisible, bool updateInstantly)
        {
            if (SetContourColorWithoutUpdate(level, color, alwaysVisible))
            {
                _currentLevel = color.HasValue ? level : EffectiveLevel(level - 1);
                if (updateInstantly)
                {
                    _shouldUpdateColor = false;
                    SetColor();
                }
                else
                {
                    _shouldUpdateColor = true;
                }
            }
        }

        private bool SetContourColorWithoutUpdate(int level, uint? color, bool alwaysVisible)
        {
            if (level < 0 || level >= _colors.Length)
                return false;
            if (_colors[level].Color == color)
                return false;
            _colors[level].Color = color;
            _colors[level].AlwaysVisible = alwaysVisible;
            return _currentLevel <= level; // needs update.
        }

        private void UpdateColor()
        {
            _currentLevel = EffectiveLevel();
            _shouldUpdateColor = true;
        }

        [SecurityCritical]
        public void ClearContourColor()
        {
            try
            {
                for (int i = 0; i < _colors.Length; ++i)
                {
                    _colors[i].Color = null;
                }

                Agent.AgentVisuals?.SetContourColor(null);
                SetMountContour(GetBoundMount(), null);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public void ClearTargetOrSelectedFormationColor()
        {
            bool needUpdate = SetContourColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            if (needUpdate)
                UpdateColor();
        }

        public void ClearFormationColor(bool updateInstantly)
        {
            bool needUpdate = SetContourColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int)ColorLevel.MouseOverFormation, null, true);
            if (needUpdate)
            {
                UpdateColor();
            }
            if (updateInstantly && needUpdate)
            {
                _shouldUpdateColor = false;
                SetColor();
            }
        }

        [SecurityCritical]
        public override void OnMount(Agent mount)
        {
            base.OnMount(mount);

            try
            {
                SetMountContour(mount, CurrentColor, CurrentAlwaysVisible);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        [SecurityCritical]
        public override void OnDismount(Agent mount)
        {
            base.OnDismount(mount);

            try
            {
                var rider = mount?.RiderAgent;
                if (CurrentColor != null && (rider == null || rider == Agent))
                    SetMountContour(mount, null);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public void UpdateContour()
        {
            if (_shouldUpdateColor)
            {
                _shouldUpdateColor = false;
                SetColor();
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

        [SecurityCritical]
        private void SetColor()
        {
            if (_agentLeavingMission)
                return;

            try
            {
                Agent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
                SetMountContour(GetBoundMount(), CurrentColor, CurrentAlwaysVisible);
                _shouldClearColorOnRemove = CurrentColor != null;
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        private Agent GetBoundMount()
        {
            var mount = Agent.MountAgent;
            return mount?.RiderAgent == Agent ? mount : null;
        }

        private static void SetMountContour(Agent mount, uint? color, bool alwaysVisible = true)
        {
            // Check the managed lifetime before asking the engine for the visuals.
            var component = mount?.GetComponent<RTSCameraComponent>();
            if (component == null || component._agentLeavingMission || !mount.HasBeenBuilt)
                return;

            var visuals = mount.AgentVisuals;
            if (visuals?.IsValid() == true)
                visuals.SetContourColor(color, alwaysVisible);
        }

        internal void MarkAgentLeavingMission() => _agentLeavingMission = true;

        public override void OnAgentRemoved()
        {
            base.OnAgentRemoved();
            MarkAgentLeavingMission();

            if (_shouldClearColorOnRemove)
            {
                ClearContourColor();
            }
            OnComponentRemovedEvent?.Invoke(this);
        }
    }
}
