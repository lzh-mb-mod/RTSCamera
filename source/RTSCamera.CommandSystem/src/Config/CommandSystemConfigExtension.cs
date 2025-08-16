namespace RTSCamera.CommandSystem.Config
{
    public static class CommandSystemConfigExtension
    {
        public static bool IsMouseOverEnabled(this CommandSystemConfig config)
        {
            return config.ClickToSelectFormation || config.AttackSpecificFormation;
        }
    }
}
