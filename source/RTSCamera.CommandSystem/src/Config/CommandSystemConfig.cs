using MissionSharedLibrary.Config;
using MissionSharedLibrary.Utilities;
using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Config
{
    public enum BehaviorAfterCharge
    {
        Charge, Hold, Count
    }

    public enum FormationLockCondition
    {
        Never,
        WhenPressed,
        WhenNotPressed,
        Count
    }

    public enum MovementTargetHighlightMode
    {
        Never,
        FreeCameraOnly,
        NightOrFreeCamera,
        Always,
        Count
    }

    public enum ShowMode
    {
        Never,
        FreeCameraOnly,
        Always,
        Count
    }

    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 2);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public BehaviorAfterCharge BehaviorAfterCharge = !CommandSystemSubModule.IsRealisticBattleModuleInstalled ? BehaviorAfterCharge.Hold : BehaviorAfterCharge.Charge;

        public ShowMode SelectedFormationHighlightMode = ShowMode.FreeCameraOnly;

        // deprecated. use SelectedFormationHighlightMode instead.
        public bool HighlightSelectedFormation = true;

        public ShowMode TargetFormationHighlightMode = ShowMode.FreeCameraOnly;

        // deprecated. use TargetFormationHighlightMode instead.
        public bool HighlightTargetFormation = true;

        // deprecated. use SelectedFormationHighlightMode and TargetFormationHighlightMode instead.
        public bool HighlightOnRtsViewOnly = true;

        public MovementTargetHighlightMode MovementTargetHighlightMode = MovementTargetHighlightMode.NightOrFreeCamera;

        // deprecated. use MovementTargetHighlightMode instead.
        public bool MoreVisibleMovementTarget = true;

        // deprecated. use MovementTargetHighlightMode instead.
        public bool MovementTargetMoreVisibleOnRtsViewOnly = true;

        public ShowMode CommandQueueFlagShowMode = ShowMode.Always;

        public ShowMode CommandQueueArrowShowMode = ShowMode.FreeCameraOnly;

        public FormationLockCondition FormationLockCondition = FormationLockCondition.WhenNotPressed;

        public bool HasHintDisplayed = false;

        protected override void CopyFrom(CommandSystemConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
            BehaviorAfterCharge = other.BehaviorAfterCharge;
            SelectedFormationHighlightMode = other.SelectedFormationHighlightMode;
            HighlightSelectedFormation = other.HighlightSelectedFormation;
            TargetFormationHighlightMode = other.TargetFormationHighlightMode;
            HighlightTargetFormation = other.HighlightTargetFormation;
            HighlightOnRtsViewOnly = other.HighlightOnRtsViewOnly;
            MovementTargetHighlightMode = other.MovementTargetHighlightMode;
            MoreVisibleMovementTarget = other.MoreVisibleMovementTarget;
            MovementTargetMoreVisibleOnRtsViewOnly = other.MovementTargetMoreVisibleOnRtsViewOnly;
            CommandQueueFlagShowMode = other.CommandQueueFlagShowMode;
            CommandQueueArrowShowMode = other.CommandQueueArrowShowMode;
            FormationLockCondition = other.FormationLockCondition;
            HasHintDisplayed = other.HasHintDisplayed;
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
                    Utility.DisplayMessage(Module.CurrentModule.GlobalTextManager.FindText("str_mission_library_config_incompatible").ToString(), new TaleWorlds.Library.Color(1, 0, 0));
                    ResetToDefault();
                    Serialize();
                    goto case "1.1";
                case "1.0":
                    if (HighlightSelectedFormation)
                    {
                        if (HighlightOnRtsViewOnly)
                        {
                            SelectedFormationHighlightMode = ShowMode.FreeCameraOnly;
                        }
                        else
                        {
                            SelectedFormationHighlightMode = ShowMode.Always;
                        }
                    }
                    else
                    {
                        SelectedFormationHighlightMode = ShowMode.Never;
                    }
                    if (HighlightTargetFormation)
                    {
                        if (HighlightOnRtsViewOnly)
                        {
                            TargetFormationHighlightMode = ShowMode.FreeCameraOnly;
                        }
                        else
                        {
                            TargetFormationHighlightMode = ShowMode.Always;
                        }
                    }
                    else
                    {
                        TargetFormationHighlightMode = ShowMode.Never;
                    }
                    if (MoreVisibleMovementTarget)
                    {
                        if (MovementTargetMoreVisibleOnRtsViewOnly)
                        {
                            MovementTargetHighlightMode = MovementTargetHighlightMode.FreeCameraOnly;
                        }
                        else
                        {
                            MovementTargetHighlightMode = MovementTargetHighlightMode.Always;
                        }
                    }
                    else
                    {
                        MovementTargetHighlightMode = MovementTargetHighlightMode.Never;
                    }
                    goto case "1.1";
                case "1.1":
                    if (MovementTargetHighlightMode == MovementTargetHighlightMode.FreeCameraOnly)
                    {
                        MovementTargetHighlightMode = MovementTargetHighlightMode.NightOrFreeCamera;
                    }
                    goto case "1.2";
                case "1.2":
                    ConfigVersion = "1.2";
                    break;
            }
        }

        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemConfig) + ".xml");
    }
}

