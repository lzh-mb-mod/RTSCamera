using System;
using System.Xml.Serialization;

namespace EnhancedMission
{
    public class EnhancedMissionConfig : EnhancedMissionConfigBase<EnhancedMissionConfig>
    {
        protected static Version BinaryVersion => new Version(1, 0);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion?.ToString())
            {
                default:
                    Utility.DisplayLocalizedText("str_em_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.0":
                    break;
            }
        }

        private static EnhancedMissionConfig _instance;

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool UseFreeCameraByDefault = false;

        public float RaisedHeight = 10;

        public int PlayerFormation = 4;

        public bool DisableDeath = false;

        public bool SlowMotionMode = false;

        public float SlowMotionFactor = 0.2f;

        public bool DisplayMessage = true;

        public bool ControlAlliesAfterDeath = false;

        public bool PreferToControlCompanions = false;

        public bool ControlTroopsInPlayerPartyOnly = true;

        private static EnhancedMissionConfig CreateDefault()
        {
            return new EnhancedMissionConfig();
        }
        public static EnhancedMissionConfig Get()
        {
            if (_instance == null)
            {
                _instance = CreateDefault();
                _instance.SyncWithSave();
            }

            return _instance;
        }

        protected override XmlSerializer serializer => new XmlSerializer(typeof(EnhancedMissionConfig));

        protected override void CopyFrom(EnhancedMissionConfig other)
        {
            this.ConfigVersion = other.ConfigVersion;
            this.UseFreeCameraByDefault = other.UseFreeCameraByDefault;
            this.RaisedHeight = other.RaisedHeight;
            this.PlayerFormation = other.PlayerFormation;
            this.DisableDeath = other.DisableDeath;
            this.SlowMotionMode = other.SlowMotionMode;
            this.SlowMotionFactor = other.SlowMotionFactor;
            this.DisplayMessage = other.DisplayMessage;
            this.ControlAlliesAfterDeath = other.ControlAlliesAfterDeath;
            this.PreferToControlCompanions = other.PreferToControlCompanions;
            this.ControlTroopsInPlayerPartyOnly = other.ControlTroopsInPlayerPartyOnly;
        }

        public override void ResetToDefault()
        {
            CopyFrom(CreateDefault());
        }
        [XmlIgnore]
        protected override string SaveName => SavePath + nameof(EnhancedMissionConfig) + ".xml";
        [XmlIgnore]
        protected override string[] OldNames { get; } = { };
    }
}