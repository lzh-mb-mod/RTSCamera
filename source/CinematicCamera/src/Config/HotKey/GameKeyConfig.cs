using MissionSharedLibrary.Config;
using MissionSharedLibrary.Config.HotKey;
using System.IO;

namespace CinematicCamera.Config.HotKey
{
    public class GameKeyConfig : GameKeyConfigBase<GameKeyConfig>
    {
        protected override string SaveName { get; } =
            Path.Combine(ConfigPath.ConfigDir, CinematicCameraSubModule.ModuleId, nameof(GameKeyConfig) + ".xml");
    }
}
