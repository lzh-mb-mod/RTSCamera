using System;
using System.IO;
using System.Xml.Serialization;
using RTSCamera.Config.Basic;

namespace RTSCamera.Config
{
    public class RTSCameraConfig : RTSCameraConfigBase<RTSCameraConfig>
    {
        protected static Version BinaryVersion => new Version(1, 4);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion)
            {
                default:
                    Utility.DisplayLocalizedText("str_rts_camera_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    goto case "1.0";
                case "1.0":
                    ConstantSpeed = false;
                    Outdoor = true;
                    RestrictByBoundaries = true;
                    goto case "1.4";
                case "1.1":
                case "1.2":
                case "1.3":
                case "1.4":
                    break;
            }

            ConfigVersion = BinaryVersion.ToString(2);
        }

        private static RTSCameraConfig _instance;

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool UseFreeCameraByDefault;

        public float RaisedHeight = 10;

        public int PlayerFormation = 4;

        public bool AlwaysSetPlayerFormation;

        public bool ConstantSpeed;

        public bool Outdoor = true;

        public bool RestrictByBoundaries = true;

        public bool SlowMotionMode;

        public float SlowMotionFactor = 0.2f;

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public bool FixCircularArrangement = true;

        public bool DisplayMessage = true;

        public bool ControlAllyAfterDeath;

        public bool PreferToControlCompanions;

        public bool ControlTroopsInPlayerPartyOnly = true;

        public bool DisableDeath;

        public bool DisableDeathHotkeyEnabled;

        public bool SwitchTeamHotkeyEnabled;

        private static RTSCameraConfig CreateDefault()
        {
            return new RTSCameraConfig();
        }
        public static RTSCameraConfig Get()
        {
            if (_instance == null)
            {
                _instance = CreateDefault();
                _instance.SyncWithSave();
            }

            return _instance;
        }

        public static void Clear()
        {
            _instance = null;
        }

        protected override XmlSerializer serializer => new XmlSerializer(typeof(RTSCameraConfig));

        protected override void CopyFrom(RTSCameraConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            UseFreeCameraByDefault = other.UseFreeCameraByDefault;
            RaisedHeight = other.RaisedHeight;
            PlayerFormation = other.PlayerFormation;
            AlwaysSetPlayerFormation = other.AlwaysSetPlayerFormation;
            ConstantSpeed = other.ConstantSpeed;
            Outdoor = other.Outdoor;
            RestrictByBoundaries = other.RestrictByBoundaries;
            SlowMotionMode = other.SlowMotionMode;
            SlowMotionFactor = other.SlowMotionFactor;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
            FixCircularArrangement = other.FixCircularArrangement;
            DisplayMessage = other.DisplayMessage;
            ControlAllyAfterDeath = other.ControlAllyAfterDeath;
            PreferToControlCompanions = other.PreferToControlCompanions;
            ControlTroopsInPlayerPartyOnly = other.ControlTroopsInPlayerPartyOnly;
            DisableDeath = other.DisableDeath;
            DisableDeathHotkeyEnabled = other.DisableDeathHotkeyEnabled;
            SwitchTeamHotkeyEnabled = other.SwitchTeamHotkeyEnabled;
        }

        public override void ResetToDefault()
        {
            CopyFrom(CreateDefault());
        }
        [XmlIgnore]
        protected override string SaveName => Path.Combine(SavePath, nameof(RTSCameraConfig) + ".xml");
        [XmlIgnore]
        protected override string[] OldNames { get; } = { Path.Combine(OldSavePath, "EnhancedMissionConfig.xml") };
    }
}