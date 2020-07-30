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
    public class AgentContourComponent : AgentComponent
    {
        private readonly Contour[] _colors = new Contour[4];
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
            if (Agent.HasMount)
                Agent.MountAgent.GetComponent<AgentContourComponent>()?.SetContourColor(level, color, alwaysVisible);
            if (level < 0 || level >= _colors.Length)
                return;
            if (_colors[level].Color == color)
                return;
            _colors[level].Color = color;
            _colors[level].AlwaysVisible = alwaysVisible;
            if (_currentLevel <= level)
            {
                _currentLevel = color.HasValue ? level : EffectiveLevel(level - 1);
                Agent.AgentVisuals?.SetContourColor(CurrentColor, CurrentAlwaysVisible);
            }
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

        private int EffectiveLevel(int maxLevel)
        {
            for (int i = maxLevel; i > -1; --i)
            {
                if (_colors[i].Color.HasValue)
                    return i;
            }

            return -1;
        }
    }
}
