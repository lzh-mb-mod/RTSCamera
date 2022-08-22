using MissionSharedLibrary.Config;
using MissionSharedLibrary.Utilities;
using System;
using System.IO;
using System.Xml.Serialization;

namespace CinematicCamera
{
    public class CinematicCameraConfig : MissionConfigBase<CinematicCameraConfig>
    {
        protected static Version BinaryVersion => new Version(1, 0);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion?.ToString())
            {
                default:
                    Utility.DisplayLocalizedText("str_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.0":
                    break;
            }
        }

        public bool PlayerInvulnerable = false;

        public float CameraFov = 65f;

        public bool RotateSmoothMode = true;

        //public float Zoom = 1.0f;

        public float SpeedFactor = 1.0f;

        public float VerticalSpeedFactor = 1.0f;

        public float DepthOfFieldDistance = 0;

        public float DepthOfFieldStart = 0;

        public float DepthOfFieldEnd = 0;


        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CinematicCameraConfig));
        
        protected override void CopyFrom(CinematicCameraConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            PlayerInvulnerable = other.PlayerInvulnerable;
            CameraFov = other.CameraFov;
            RotateSmoothMode = other.RotateSmoothMode;
            //Zoom = other.Zoom;
            SpeedFactor = other.SpeedFactor;
            VerticalSpeedFactor = other.VerticalSpeedFactor;
            DepthOfFieldDistance = other.DepthOfFieldDistance;
            DepthOfFieldStart = other.DepthOfFieldStart;
            DepthOfFieldEnd = other.DepthOfFieldEnd;
        }

        protected static string SavePathStatic { get; } = Path.Combine(ConfigPath.ConfigDir, CinematicCameraSubModule.ModuleId);
        protected static string OldSavePathStatic { get; } = Path.Combine(ConfigPath.ConfigDir, "RTSCamera");

        protected override string SaveName => Path.Combine(SavePathStatic, nameof(CinematicCameraConfig) + ".xml");

        protected override string OldSavePath => OldSavePathStatic;
        protected override string[] OldNames { get; } =
        {
            Path.Combine(OldSavePathStatic, "CinematicCameraConfig.xml"),
            Path.Combine(OldSavePathStatic, "CinematicCameraConfig.xml")
        };
    }
}
