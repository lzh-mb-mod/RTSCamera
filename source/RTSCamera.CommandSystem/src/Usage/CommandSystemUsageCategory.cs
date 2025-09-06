using MissionLibrary.Usage;
using MissionSharedLibrary.Usage;
using RTSCamera.CommandSystem.Config.HotKey;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace RTSCamera.CommandSystem.Usage
{
    public class CommandSystemUsageCategory
    {
        public const string CategoryId = "CommandSystemUsage";

        public static AUsageCategory Category => AUsageCategoryManager.Get().GetItem(CategoryId);

        public static void RegisterUsageCategory()
        {
            AUsageCategoryManager.Get()?.RegisterItem(CreateCategory, CategoryId, new Version(1, 0));
        }

         public static UsageCategory CreateCategory()
        {
            var usageCategoryData = new UsageCategoryData(
                GameTexts.FindText("str_rts_camera_command_system_option_class"),
                new List<TaleWorlds.Localization.TextObject>
                {
                    GameTexts.FindText("str_rts_camera_command_system_order_queue_usage").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_command_system_lock_formation_usage"),
                    GameTexts.FindText("str_rts_camera_command_system_lock_formation_width_usage").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepFormationWidth).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_command_system_toggle_locking_usage").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.FormationLockMovement).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName",
                        CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_command_system_attack_specific_formation_alt_hint"),
                    GameTexts.FindText("str_rts_camera_command_system_target_only_usage"),
                });

            return new UsageCategory(CategoryId, usageCategoryData);
        }
    }
}
