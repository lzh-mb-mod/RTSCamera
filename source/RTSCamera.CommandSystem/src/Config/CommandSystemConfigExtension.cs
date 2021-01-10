using RTSCamera.CommandSystem.Config;

namespace RTSCamera.Config
{
    public static class CommandSystemConfigExtension
    {
        public static bool ShouldHighlightWithOutline(this CommandSystemConfig config)
        {
            return config.ClickToSelectFormation || config.AttackSpecificFormation;
        }
    }
}
