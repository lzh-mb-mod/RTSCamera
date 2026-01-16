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

    public enum FormationSpeedSyncMode
    {
        Disabled,
        Linear,
        CatchUp,
        WaitForLastFormation,
        Count
    }

    public enum VolleyPreAimingMode
    {
        InAutoVolley,
        BothAutoAndManualVolley,
        Count
    }

    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 3);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public bool DisableNativeAttack = false;

        public BehaviorAfterCharge BehaviorAfterCharge = !CommandSystemSubModule.IsRealisticBattleModuleInstalled ? BehaviorAfterCharge.Hold : BehaviorAfterCharge.Charge;

        public TroopHighlightStyle TroopHighlightStyleInCharacterMode = TroopHighlightStyle.GroundMarker;

        public TroopHighlightStyle TroopHighlightStyleInRTSMode = TroopHighlightStyle.GroundMarker;

        public ShowMode HighlightTroopsWhenShowingIndicators = ShowMode.Always;

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

        public FormationSpeedSyncMode FormationSpeedSyncMode = FormationSpeedSyncMode.WaitForLastFormation;

        public bool HasHintDisplayed = false;

        public bool HollowSquare = true;

        public bool SquareFormationCornerFix = true;

        public bool OrderUIClickable = true;

        public bool OrderUIClickableExtension = false;

        public bool FacingEnemyByDefault = false;

        public CircleFormationUnitSpacingPreference CircleFormationUnitSpacingPreference = CircleFormationUnitSpacingPreference.Tight;

        public float MountedUnitsIntervalThreshold = 0.1f;

        public bool FixAdvaneOrderForThrowing = true;

        public bool ApplyAdvanceOrderFixForAI = false;

        public float ThrowerRatioThreshold = 0.5f;

        public float RemainingAmmoRatioThreshold = 0.1f;

        public bool ShortenRangeBasedOnRemainingAmmo = false;

        public VolleyPreAimingMode VolleyPreAimingMode = VolleyPreAimingMode.BothAutoAndManualVolley;

        public float ReadyRatioInAutoVolley = 0.8f;

        public float MaxAimingTime = 1.5f;

        public bool AutoVolleyByWeaponTypeForNonThrown = true;

        public bool AutoVolleyByWeaponTypeForThrown = true;

        public bool IsCommandOptionVisible = true;

        public bool IsAdvanceOrderOptionVisible = true;

        public bool IsVolleyOrderOptionVisible = true;

        protected override void CopyFrom(CommandSystemConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            ClickToSelectFormation = other.ClickToSelectFormation;
            AttackSpecificFormation = other.AttackSpecificFormation;
            DisableNativeAttack = other.DisableNativeAttack;
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
            FormationSpeedSyncMode = other.FormationSpeedSyncMode;
            HasHintDisplayed = other.HasHintDisplayed;
            HollowSquare = other.HollowSquare;
            SquareFormationCornerFix = other.SquareFormationCornerFix;
            OrderUIClickable = other.OrderUIClickable;
            OrderUIClickableExtension = other.OrderUIClickableExtension;
            FacingEnemyByDefault = other.FacingEnemyByDefault;
            CircleFormationUnitSpacingPreference = other.CircleFormationUnitSpacingPreference;
            MountedUnitsIntervalThreshold = other.MountedUnitsIntervalThreshold;
            FixAdvaneOrderForThrowing = other.FixAdvaneOrderForThrowing;
            ApplyAdvanceOrderFixForAI = other.ApplyAdvanceOrderFixForAI;
            ThrowerRatioThreshold = other.ThrowerRatioThreshold;
            RemainingAmmoRatioThreshold = other.RemainingAmmoRatioThreshold;
            ShortenRangeBasedOnRemainingAmmo = other.ShortenRangeBasedOnRemainingAmmo;
            VolleyPreAimingMode = other.VolleyPreAimingMode;
            ReadyRatioInAutoVolley = other.ReadyRatioInAutoVolley;
            MaxAimingTime = other.MaxAimingTime;
            AutoVolleyByWeaponTypeForNonThrown = other.AutoVolleyByWeaponTypeForNonThrown;
            AutoVolleyByWeaponTypeForThrown = other.AutoVolleyByWeaponTypeForThrown;
            IsCommandOptionVisible = other.IsCommandOptionVisible;
            IsAdvanceOrderOptionVisible = other.IsAdvanceOrderOptionVisible;
            IsVolleyOrderOptionVisible = other.IsVolleyOrderOptionVisible;
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

