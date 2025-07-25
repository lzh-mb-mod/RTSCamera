﻿using MissionSharedLibrary.Config;
using MissionSharedLibrary.Utilities;
using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Config
{
    public enum BehaviorAfterCharge
    {
        Charge, Hold
    }

    public class CommandSystemConfig : MissionConfigBase<CommandSystemConfig>
    {
        protected override XmlSerializer Serializer => new XmlSerializer(typeof(CommandSystemConfig));

        protected static Version BinaryVersion => new Version(1, 0);
        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public bool ClickToSelectFormation = true;

        public bool AttackSpecificFormation = true;

        public BehaviorAfterCharge BehaviorAfterCharge = !CommandSystemSubModule.IsRealisticBattleModuleInstalled ? BehaviorAfterCharge.Hold : BehaviorAfterCharge.Charge;

        public bool HighlightSelectedFormation = true;

        public bool HighlightTargetFormation = true;

        public bool HighlightOnRtsViewOnly = true;

        public bool MoreVisibleMovementTarget = true;

        public bool MovementTargetMoreVisibleOnRtsViewOnly = true;

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
                    goto case "1.0";
                case "1.0": break;
            }
        }

        [XmlIgnore]
        protected override string SaveName => Path.Combine(ConfigPath.ConfigDir, "RTSCamera", nameof(CommandSystemConfig) + ".xml");
    }
}

