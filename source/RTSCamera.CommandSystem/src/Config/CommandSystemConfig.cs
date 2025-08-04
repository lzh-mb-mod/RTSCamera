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

    public enum HighlightMode
    {
        Never,
        FreeCameraOnly,
        Always,
        Count
    }

    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 1);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public BehaviorAfterCharge BehaviorAfterCharge = !CommandSystemSubModule.IsRealisticBattleModuleInstalled ? BehaviorAfterCharge.Hold : BehaviorAfterCharge.Charge;

        public HighlightMode SelectedFormationHighlightMode = HighlightMode.FreeCameraOnly;

        // deprecated. use SelectedFormationHighlightMode instead.
        public bool HighlightSelectedFormation = true;

        public HighlightMode TargetFormationHighlightMode = HighlightMode.FreeCameraOnly;

        // deprecated. use TargetFormationHighlightMode instead.
        public bool HighlightTargetFormation = true;

        // deprecated. use SelectedFormationHighlightMode and TargetFormationHighlightMode instead.
        public bool HighlightOnRtsViewOnly = true;

        public HighlightMode MovementTargetHighlightMode = HighlightMode.FreeCameraOnly;

        // deprecated. use MovementTargetHighlightMode instead.
        public bool MoreVisibleMovementTarget = true;

        // deprecated. use MovementTargetHighlightMode instead.
        public bool MovementTargetMoreVisibleOnRtsViewOnly = true;

        public HighlightMode CommandQueueFlagShowMode = HighlightMode.Always;

        public HighlightMode CommandQueueArrowShowMode = HighlightMode.FreeCameraOnly;

        public FormationLockCondition FormationLockCondition = FormationLockCondition.WhenNotPressed;

        protected override void CopyFrom(CommandSystemConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
            BehaviorAfterCharge = other.BehaviorAfterCharge;
            HighlightSelectedFormation = other.HighlightSelectedFormation;
            HighlightTargetFormation = other.HighlightTargetFormation;
            HighlightOnRtsViewOnly = other.HighlightOnRtsViewOnly;
            MoreVisibleMovementTarget = other.MoreVisibleMovementTarget;
            MovementTargetMoreVisibleOnRtsViewOnly = other.MovementTargetMoreVisibleOnRtsViewOnly;
            FormationLockCondition = other.FormationLockCondition;
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
                            SelectedFormationHighlightMode = HighlightMode.FreeCameraOnly;
                        }
                        else
                        {
                            SelectedFormationHighlightMode = HighlightMode.Always;
                        }
                    }
                    else
                    {
                        SelectedFormationHighlightMode = HighlightMode.Never;
                    }
                    if (HighlightTargetFormation)
                    {
                        if (HighlightOnRtsViewOnly)
                        {
                            TargetFormationHighlightMode = HighlightMode.FreeCameraOnly;
                        }
                        else
                        {
                            TargetFormationHighlightMode = HighlightMode.Always;
                        }
                    }
                    else
                    {
                        TargetFormationHighlightMode = HighlightMode.Never;
                    }
                    if (MoreVisibleMovementTarget)
                    {
                        if (MovementTargetMoreVisibleOnRtsViewOnly)
                        {
                            MovementTargetHighlightMode = HighlightMode.FreeCameraOnly;
                        }
                        else
                        {
                            MovementTargetHighlightMode = HighlightMode.Always;
                        }
                    }
                    else
                    {
                        MovementTargetHighlightMode = HighlightMode.Never;
                    }
                    break;
                case "1.1": break;
            }
        }

        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemConfig) + ".xml");
    }
}

