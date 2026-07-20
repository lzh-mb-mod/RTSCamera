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

    public enum ElevatedCameraTriggerMode
    {
        Never,
        WhenOpeningOrderUI,
        WhenGivingMovementOrder
    }

    public enum AutoSetPlayerFormation
    {
        Never,
        DeploymentStage,
        Always,
        Count
    }

    public enum AssignPlayerFormation
    {
        DefaultOrGeneralFormation,
        Default,
        Overwrite,
        Count
    }

    public enum  FollowFaceDirection
    {
        Never,
        ControlNewTroopOnly,
        Always,
        Count
    }

    public enum ControlAllyAfterDeathTiming
    {
        Never,
        FreeCamera,
        Always,
        Count
    }

    public enum FastForwardHideout
    {
        Never,
        UntilBossFight,
        Always,
        Count
    }

    public enum PlayerShipController
    {
        None,
        AI,
        Player,
        Count
    }

    public enum SteeringMode
    {
        None,
        Soldier,
        DelegateCommand,
        Count
    }

    public enum HotkeyMode
    {
        Toggle,
        Hold,
        Count
    }

    public class RTSCameraConfig : RTSCameraConfigBase<RTSCameraConfig>
    {
        protected static Version BinaryVersion => new Version(1, 9);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion)
            {
                default:
                    Utility.DisplayMessage(Module.CurrentModule.GlobalTextManager.FindText("str_mission_library_config_incompatible").ToString(), new TaleWorlds.Library.Color(1, 0, 0));
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
                    TimingOfControlAllyAfterDeath = ControlAllyAfterDeath ? ControlAllyAfterDeathTiming.Always : ControlAllyAfterDeathTiming.FreeCamera;
                    goto case "1.8";
                case "1.8":
                    goto case "1.9";
                case "1.9":
                    break;
            }

            ConfigVersion = BinaryVersion.ToString(2);
        }

        public string ConfigVersion = BinaryVersion.ToString();

        public DefaultToFreeCamera DefaultToFreeCamera = DefaultToFreeCamera.DeploymentStage;

        public float RaisedHeight = 10;

        public int PlayerControllerInFreeCamera = (int)AgentControllerType.AI;

        public FormationClass PlayerFormation = FormationClass.General;

        // deprecated
        public AutoSetPlayerFormation AutoSetPlayerFormation = AutoSetPlayerFormation.Never;

        public AssignPlayerFormation AssignPlayerFormation = AssignPlayerFormation.Default;

        public bool ConstantSpeed;

        public bool CameraHeightFollowsTerrain;

        public bool IgnoreTerrain;

        public bool IgnoreBoundaries;

        public FollowFaceDirection FollowFaceDirection = FollowFaceDirection.ControlNewTroopOnly;

        public bool SlowMotionMode;

        public float SlowMotionFactor = 0.2f;

        public bool SlowMotionOnRtsView;

        public HotkeyMode SlowMotionHotkeyMode = HotkeyMode.Toggle;

        public bool OverrideFastForwardSpeed;

        public float FastForwardSpeed = 3f;

        public HotkeyMode FastForwardHotkeyMode = HotkeyMode.Toggle;

        public bool DisplayMessage = true;

        public bool HasHintDisplayed = false;

        // use TimingOfControlAllyAfterDeath instead.

        public bool ControlAllyAfterDeath;

        public bool IsControlAllyAfterDeathPrompted = false;

        public ControlAllyAfterDeathTiming TimingOfControlAllyAfterDeath = ControlAllyAfterDeathTiming.FreeCamera;

        public bool PreferUnitsInSameFormation = true;

        public bool ControlTroopsInPlayerPartyOnly = false;

        public bool ControlHeroOnly = false;

        public bool IgnoreRetreatingTroops = true;

        public bool DisableDeathHotkeyEnabled;

        public bool SwitchTeamHotkeyEnabled;

        public bool LimitCameraDistance = false;

        public float CameraDistanceLimitFactor = 1;

        public bool SwitchCameraOnOrdering = false;

        public ElevatedCameraTriggerMode ElevatedCameraTriggerMode = ElevatedCameraTriggerMode.WhenOpeningOrderUI;

        public float ElevatedHeight = 10f;

        public float ElevatedHeightInSiege = 0;

        public bool OrderOnSwitchingCamera = true;

        public bool KeepOrderUIOpenInFreeCamera = true;

        public bool KeepOrderUIOpenInElevatedCamera = false;

        public float ElevatedCameraDelay = 0.35f;

        public float ElevatedCameraDuration = 0.7f;

        public float ElevatedCameraOffDuration = 1f;

        public bool ShowHotKeyHint = true;

        public bool FastForwardHideoutPrompted = false;

        public FastForwardHideout FastForwardHideout = FastForwardHideout.Never;

        public PlayerShipController PlayerShipControllerInFreeCamera = PlayerShipController.AI;

        public bool SoldiersPilotShipInPlayerMode = true;

        public SteeringMode SteeringModeWhenPlayerStopsPiloting = SteeringMode.None;

        public bool SwitchNavalRetreatAndDelegateCommand = true;

        public bool IsCameraOptionVisible = true;

        public bool IsControlOptionVisible = true;

        public bool IsMiscellaneousOptionVisible = false;

        public bool IsTimeSpeedOptionVisible = false;

        public bool IsNavalOptionVisible = true;

        public bool IsCheatOptionVisible = true;

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
            AssignPlayerFormation = other.AssignPlayerFormation;
            ConstantSpeed = other.ConstantSpeed;
            CameraHeightFollowsTerrain = other.CameraHeightFollowsTerrain;
            IgnoreTerrain = other.IgnoreTerrain;
            IgnoreBoundaries = other.IgnoreBoundaries;
            FollowFaceDirection = other.FollowFaceDirection;
            SlowMotionMode = other.SlowMotionMode;
            SlowMotionFactor = other.SlowMotionFactor;
            SlowMotionOnRtsView = other.SlowMotionOnRtsView;
            SlowMotionHotkeyMode = other.SlowMotionHotkeyMode;
            OverrideFastForwardSpeed = other.OverrideFastForwardSpeed;
            FastForwardSpeed = other.FastForwardSpeed;
            FastForwardHotkeyMode = other.FastForwardHotkeyMode;
            DisplayMessage = other.DisplayMessage;
            HasHintDisplayed = other.HasHintDisplayed;
            ControlAllyAfterDeath = other.ControlAllyAfterDeath;
            IsControlAllyAfterDeathPrompted = other.IsControlAllyAfterDeathPrompted;
            TimingOfControlAllyAfterDeath = other.TimingOfControlAllyAfterDeath;
            IgnoreRetreatingTroops = other.IgnoreRetreatingTroops;
            PreferUnitsInSameFormation = other.PreferUnitsInSameFormation;
            ControlTroopsInPlayerPartyOnly = other.ControlTroopsInPlayerPartyOnly;
            ControlHeroOnly = other.ControlHeroOnly;
            DisableDeathHotkeyEnabled = other.DisableDeathHotkeyEnabled;
            SwitchTeamHotkeyEnabled = other.SwitchTeamHotkeyEnabled;
            LimitCameraDistance = other.LimitCameraDistance;
            CameraDistanceLimitFactor = other.CameraDistanceLimitFactor;
            SwitchCameraOnOrdering = other.SwitchCameraOnOrdering;
            ElevatedCameraTriggerMode = other.ElevatedCameraTriggerMode;
            ElevatedHeight = other.ElevatedHeight;
            ElevatedHeightInSiege = other.ElevatedHeightInSiege;
            OrderOnSwitchingCamera = other.OrderOnSwitchingCamera;
            KeepOrderUIOpenInFreeCamera = other.KeepOrderUIOpenInFreeCamera;
            KeepOrderUIOpenInElevatedCamera = other.KeepOrderUIOpenInElevatedCamera;
            ElevatedCameraDelay = other.ElevatedCameraDelay;
            ElevatedCameraDuration = other.ElevatedCameraDuration;
            ElevatedCameraOffDuration = other.ElevatedCameraOffDuration;
            ShowHotKeyHint = other.ShowHotKeyHint;
            FastForwardHideoutPrompted = other.FastForwardHideoutPrompted;
            FastForwardHideout = other.FastForwardHideout;
            PlayerShipControllerInFreeCamera = other.PlayerShipControllerInFreeCamera;
            SoldiersPilotShipInPlayerMode = other.SoldiersPilotShipInPlayerMode;
            SteeringModeWhenPlayerStopsPiloting = other.SteeringModeWhenPlayerStopsPiloting;
            SwitchNavalRetreatAndDelegateCommand = other.SwitchNavalRetreatAndDelegateCommand;
            IsCameraOptionVisible = other.IsCameraOptionVisible;
            IsControlOptionVisible = other.IsControlOptionVisible;
            IsMiscellaneousOptionVisible = other.IsMiscellaneousOptionVisible;
            IsTimeSpeedOptionVisible = other.IsTimeSpeedOptionVisible;
            IsNavalOptionVisible = other.IsNavalOptionVisible;
            IsCheatOptionVisible = other.IsCheatOptionVisible;
        }
        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, RTSCameraSubModule.ModuleId, nameof(RTSCameraConfig) + ".xml");
    }
}