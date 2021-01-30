using System.IO;
using MissionSharedLibrary.Config;
using MissionSharedLibrary.Config.HotKey;

namespace RTSCamera.Config.HotKey
{
    public class GameKeyConfig : GameKeyConfigBase<GameKeyConfig>
    {
        protected override string SaveName { get; } =
            Path.Combine(ConfigPath.ConfigDir, RTSCameraSubModule.ModuleId, nameof(GameKeyConfig) + ".xml");
        protected static string OldSavePathStatic { get; } = Path.Combine(ConfigPath.ConfigDir, RTSCameraSubModule.OldModuleId);
        protected override string OldSavePath => OldSavePathStatic;

        protected override string[] OldNames { get; } =
        {
            Path.Combine(OldSavePathStatic, "GameKeyConfig.xml")
        };
    }
}
