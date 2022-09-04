using MissionSharedLibrary.Config;
using System.IO;

namespace RTSCamera.Config
{
    public static class OldConfigPath
    {
        public static string OldSavePath { get; } = Path.Combine(ConfigPath.ConfigDir, RTSCameraSubModule.OldModuleId);
    }

    public abstract class RTSCameraConfigBase<T> : MissionConfigBase<T> where T : RTSCameraConfigBase<T>
    {
        protected override string OldSavePath => OldConfigPath.OldSavePath;

        protected override string[] OldNames { get; } =
        {
            Path.Combine(OldConfigPath.OldSavePath, "EnhancedMissionConfig.xml")
        };
    }
}
