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

    public class AgentContourComponent : AgentComponent
    {
        private readonly Contour[] _colors = new Contour[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        public AgentContourComponent(Agent agent) : base(agent)
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i] = new Contour(null, false);
            }
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
            bool needUpdate = SetContourColorWithoutUpdate((int) ColorLevel.TargetFormation, null, true);
            needUpdate |= SetContourColorWithoutUpdate((int) ColorLevel.SelectedFormation, null, true);
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
