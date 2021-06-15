using System;
using System.IO;
using System.Xml.Serialization;
using MissionSharedLibrary.Config;

namespace RTSCamera.CommandSystem.Config
{
    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 0);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        protected override void CopyFrom(CommandSystemConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
        }

        public static void OnMenuClosed()
        {
            Get().Serialize();
        }
        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion)
            {
                default:
                    ResetToDefault();
                    Serialize();
                    goto case "1.0";
                case "1.0": break;
            }
        }

        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemConfig) + ".xml");
    }
}

