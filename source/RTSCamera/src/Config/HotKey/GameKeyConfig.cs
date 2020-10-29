using MissionSharedLibrary.Config;
using MissionSharedLibrary.Config.HotKey;
using System.IO;
using System.Xml.Serialization;

namespace RTSCamera.Config.HotKey
{
    public class GameKeyConfig : GameKeyConfigBase<GameKeyConfig>
    {

        private static GameKeyConfig CreateDefaultStatic()
        {
            return new GameKeyConfig();
        }


        protected override void CopyFrom(GameKeyConfig other)
        {
            base.CopyFrom(other);

            ConfigVersion = other.ConfigVersion;
        }

        protected override void UpgradeToCurrentVersion()
        {
        }

        protected override XmlSerializer Serializer => new XmlSerializer(typeof(GameKeyConfig));

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
