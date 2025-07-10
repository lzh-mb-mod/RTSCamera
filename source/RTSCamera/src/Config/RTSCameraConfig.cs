using MissionSharedLibrary.Config;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Config
{
    public enum DefaultToFreeCamera
    {
        Never,
        DeploymentStage,
        Always,
        Count
    }

    public enum AutoSetPlayerFormation
    {
        Never,
        DeploymentStage,
        Always,
        Count
    }
    public class RTSCameraConfig : RTSCameraConfigBase<RTSCameraConfig>
    {
        protected static Version BinaryVersion => new Version(1, 7);

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
                    IgnoreTerrain = false;
                    IgnoreBoundaries = false;
                    goto case "1.4";
                case "1.1":
                case "1.2":
                case "1.3":
                case "1.4":
                    goto case "1.5";
                case "1.5":
                    CameraDistanceLimitFactor = 1;
                    CameraHeightFollowsTerrain = false;
                    goto case "1.6";
                case "1.6":
                case "1.7":
                    break;
            }

            ConfigVersion = BinaryVersion.ToString(2);
        }

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public DefaultToFreeCamera DefaultToFreeCamera = DefaultToFreeCamera.DeploymentStage;

        public float RaisedHeight = 10;

        public int PlayerControllerInFreeCamera = (int)Agent.ControllerType.AI;

        public Agent.ControllerType GetPlayerControllerInFreeCamera(Mission mission)
        {
            if (WatchBattleBehavior.WatchMode || mission?.Mode == MissionMode.Deployment)
                return Agent.ControllerType.AI;
            return (Agent.ControllerType) PlayerControllerInFreeCamera;
        }

        public FormationClass PlayerFormation = FormationClass.Unset;

        public AutoSetPlayerFormation AutoSetPlayerFormation = AutoSetPlayerFormation.Never;

        public bool ConstantSpeed;

        public bool CameraHeightFollowsTerrain;

        public bool IgnoreTerrain;

        public bool IgnoreBoundaries;
        public bool FollowFacingDiretion = false;

        public bool SlowMotionMode;

        public float SlowMotionFactor = 0.2f;

        public bool SlowMotionOnRtsView;

        public bool DisplayMessage = true;

        public bool ControlAllyAfterDeath;

        public bool PreferUnitsInSameFormation = true;

        public bool ControlTroopsInPlayerPartyOnly = true;

        public bool IgnoreRetreatingTroops = true;

        public bool DisableDeathHotkeyEnabled;

        public bool SwitchTeamHotkeyEnabled;

        public bool LimitCameraDistance = false;

        public float CameraDistanceLimitFactor = 1;

        public bool OrderUIClickable = true;

        public bool SwitchCameraOnOrdering = false;

        public bool OrderOnSwitchingCamera = true;

        public static void OnMenuClosed()
        {
            Get().Serialize();
        }

        protected override void CopyFrom(RTSCameraConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            DefaultToFreeCamera = other.DefaultToFreeCamera;
            RaisedHeight = other.RaisedHeight;
            PlayerControllerInFreeCamera = other.PlayerControllerInFreeCamera;
            PlayerFormation = other.PlayerFormation;
            AutoSetPlayerFormation = other.AutoSetPlayerFormation;
            ConstantSpeed = other.ConstantSpeed;
            CameraHeightFollowsTerrain = other.CameraHeightFollowsTerrain;
            IgnoreTerrain = other.IgnoreTerrain;
            IgnoreBoundaries = other.IgnoreBoundaries;
            FollowFacingDiretion = other.FollowFacingDiretion;
            SlowMotionMode = other.SlowMotionMode;
            SlowMotionFactor = other.SlowMotionFactor;
            SlowMotionOnRtsView = other.SlowMotionOnRtsView;
            DisplayMessage = other.DisplayMessage;
            ControlAllyAfterDeath = other.ControlAllyAfterDeath;
            IgnoreRetreatingTroops = other.IgnoreRetreatingTroops;
            PreferUnitsInSameFormation = other.PreferUnitsInSameFormation;
            ControlTroopsInPlayerPartyOnly = other.ControlTroopsInPlayerPartyOnly;
            DisableDeathHotkeyEnabled = other.DisableDeathHotkeyEnabled;
            SwitchTeamHotkeyEnabled = other.SwitchTeamHotkeyEnabled;
            LimitCameraDistance = other.LimitCameraDistance;
            CameraDistanceLimitFactor = other.CameraDistanceLimitFactor;
            OrderUIClickable = other.OrderUIClickable;
            SwitchCameraOnOrdering = other.SwitchCameraOnOrdering;
            OrderOnSwitchingCamera = other.OrderOnSwitchingCamera;
        }
        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, RTSCameraSubModule.ModuleId, nameof(RTSCameraConfig) + ".xml");
    }
}