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

    public enum MovementTargetHighlightStyle
    {
        Original,
        NewModelOnly,
        AlwaysVisible,
        Count
    }

    public enum ShowMode
    {
        Never,
        FreeCameraOnly,
        Always,
        Count
    }

    public enum TroopHighlightStyle
    {
        No,
        Outline,
        GroundMarker,
        Count
    }

    public enum CircleFormationUnitSpacingPreference
    {
        Tight,
        Loose,
        Count
    }

    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 3);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public BehaviorAfterCharge BehaviorAfterCharge = !CommandSystemSubModule.IsRealisticBattleModuleInstalled ? BehaviorAfterCharge.Hold : BehaviorAfterCharge.Charge;

        public TroopHighlightStyle TroopHighlightStyleInCharacterMode = TroopHighlightStyle.GroundMarker;

        public TroopHighlightStyle TroopHighlightStyleInRTSMode = TroopHighlightStyle.GroundMarker;

        public MovementTargetHighlightStyle MovementTargetHighlightStyleInCharacterMode = MovementTargetHighlightStyle.NewModelOnly;

        public MovementTargetHighlightStyle MovementTargetHighlightStyleInRTSMode = MovementTargetHighlightStyle.AlwaysVisible;

        // deprecated. use MovementTargetHighlightStyle instead.
        public MovementTargetHighlightMode MovementTargetHighlightMode = MovementTargetHighlightMode.Always;

        // deprecated. use MovementTargetHighlightStyle instead.
        public bool MoreVisibleMovementTarget = true;

        // deprecated. use MovementTargetHighlightStyle instead.
        public bool MovementTargetMoreVisibleOnRtsViewOnly = true;

        public ShowMode CommandQueueFlagShowMode = ShowMode.FreeCameraOnly;

        public ShowMode CommandQueueArrowShowMode = ShowMode.FreeCameraOnly;

        public ShowMode CommandQueueFormationShapeShowMode = ShowMode.Always;

        public FormationLockCondition FormationLockCondition = FormationLockCondition.WhenNotPressed;

        public bool HasHintDisplayed = false;

        public bool HollowSquare = true;

        public bool SquareFormationCornerFix = true;

        public bool OrderUIClickable = true;

        public bool OrderUIClickableExtension = false;

        public bool FacingEnemyByDefault = false;

        public CircleFormationUnitSpacingPreference CircleFormationUnitSpacingPreference = CircleFormationUnitSpacingPreference.Tight;

        protected override void CopyFrom(CommandSystemConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
            BehaviorAfterCharge = other.BehaviorAfterCharge;
            TroopHighlightStyleInCharacterMode = other.TroopHighlightStyleInCharacterMode;
            TroopHighlightStyleInRTSMode = other.TroopHighlightStyleInRTSMode;
            MovementTargetHighlightStyleInCharacterMode = other.MovementTargetHighlightStyleInCharacterMode;
            MovementTargetHighlightStyleInRTSMode = other.MovementTargetHighlightStyleInRTSMode;
            MovementTargetHighlightMode = other.MovementTargetHighlightMode;
            MoreVisibleMovementTarget = other.MoreVisibleMovementTarget;
            MovementTargetMoreVisibleOnRtsViewOnly = other.MovementTargetMoreVisibleOnRtsViewOnly;
            CommandQueueFlagShowMode = other.CommandQueueFlagShowMode;
            CommandQueueArrowShowMode = other.CommandQueueArrowShowMode;
            CommandQueueFormationShapeShowMode = other.CommandQueueFormationShapeShowMode;
            FormationLockCondition = other.FormationLockCondition;
            HasHintDisplayed = other.HasHintDisplayed;
            HollowSquare = other.HollowSquare;
            SquareFormationCornerFix = other.SquareFormationCornerFix;
            OrderUIClickable = other.OrderUIClickable;
            OrderUIClickableExtension = other.OrderUIClickableExtension;
            FacingEnemyByDefault = other.FacingEnemyByDefault;
            CircleFormationUnitSpacingPreference = other.CircleFormationUnitSpacingPreference;
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
                    switch (MovementTargetHighlightMode)
                    {
                        case MovementTargetHighlightMode.Never:
                            MovementTargetHighlightStyleInCharacterMode = MovementTargetHighlightStyle.Original;
                            MovementTargetHighlightStyleInRTSMode = MovementTargetHighlightStyle.Original;
                            break;
                        case MovementTargetHighlightMode.FreeCameraOnly:
                            MovementTargetHighlightStyleInCharacterMode = MovementTargetHighlightStyle.Original;
                            MovementTargetHighlightStyleInRTSMode = MovementTargetHighlightStyle.AlwaysVisible;
                            break;
                        case MovementTargetHighlightMode.Always:
                            MovementTargetHighlightStyleInCharacterMode = MovementTargetHighlightStyle.NewModelOnly;
                            MovementTargetHighlightStyleInRTSMode = MovementTargetHighlightStyle.AlwaysVisible;
                            break;
                    }
                    break;
                case "1.3":
                    ConfigVersion = "1.3";
                    break;
            }
        }

        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemConfig) + ".xml");
    }
}

