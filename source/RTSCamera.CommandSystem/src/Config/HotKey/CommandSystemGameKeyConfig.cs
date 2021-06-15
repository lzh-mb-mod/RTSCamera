using System.IO;
using MissionSharedLibrary.Config;
using MissionSharedLibrary.Config.HotKey;

namespace RTSCamera.CommandSystem.Config.HotKey
{
    public class CommandSystemGameKeyConfig : GameKeyConfigBase<CommandSystemGameKeyConfig>
    {
        protected override string SaveName { get; } = Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemGameKeyConfig) + ".xml");
    }
}
