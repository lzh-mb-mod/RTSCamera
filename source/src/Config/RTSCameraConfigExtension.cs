namespace RTSCamera.Config
{
    public static class RTSCameraConfigExtension
    {
        public static bool ShouldHighlightWithOutline(this RTSCameraConfig config)
        {
            return config.ClickToSelectFormation || config.AttackSpecificFormation;
        }
    }
}
