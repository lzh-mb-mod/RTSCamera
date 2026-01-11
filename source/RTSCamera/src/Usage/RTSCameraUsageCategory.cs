using MissionLibrary.Usage;
using MissionSharedLibrary.HotKey;
using MissionSharedLibrary.Usage;
using RTSCamera.Config.HotKey;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace RTSCamera.Usage
{
    public class RTSCameraUsageCategory
    {
        public const string CategoryId = "RTSCameraUsage";

        public static AUsageCategory Category => AUsageCategoryManager.Get().GetItem(CategoryId);

        public static void RegisterUsageCategory()
        {
            AUsageCategoryManager.Get()?.RegisterItem(CreateCategory, CategoryId, new Version(1, 0));
        }

         public static UsageCategory CreateCategory()
        {
            var usageCategoryData = new UsageCategoryData(
                GameTexts.FindText("str_rts_camera_option_class"),
                new List<TaleWorlds.Localization.TextObject>
                {
                    GameTexts.FindText("str_mission_library_open_menu_hint").SetTextVariable("KeyName",
                        GeneralGameKeyCategory.GetKey(GeneralGameKey.OpenMenu).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_switch_camera_hint").SetTextVariable("KeyName",
                        RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_focus_on_formation_usage").SetTextVariable("KeyName",
                        RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop ).ToSequenceString()),
                    GameTexts.FindText("str_rts_camera_control_troop_usage").SetTextVariable("KeyName",
                        RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop ).ToSequenceString()),
                });

            return new UsageCategory(CategoryId, usageCategoryData);
        }
    }
}
