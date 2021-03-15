using System;
using MissionLibrary.HotKey;
using MissionSharedLibrary.Config.HotKey;
using MissionSharedLibrary.HotKey.Category;
using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace RTSCamera.Config.HotKey
{
    public enum GameKeyEnum
    {
        Pause,
        SlowMotion,
        FreeCamera,
        DisableDeath,
        ControlTroop,
        ToggleHUD,
        SwitchTeam,
        SelectCharacter,
        NumberOfGameKeyEnums
    }
    public class RTSCameraGameKeyCategory
    {
        public const string CategoryId = "RTSCameraHotKey";

        public static AGameKeyCategory Category => AGameKeyCategoryManager.Get().GetCategory(CategoryId);

        public static void RegisterGameKeyCategory()
        {
            AGameKeyCategoryManager.Get()?.AddCategory(CreateCategory, new Version(1, 0));
        }

        public static GameKeyCategory CreateCategory()
        {
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, GameKeyConfig.Get());
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.Pause, nameof(GameKeyEnum.Pause),
                CategoryId, new List<InputKey>
                {
                    InputKey.OpenBraces
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SlowMotion,
                nameof(GameKeyEnum.SlowMotion), CategoryId, new List<InputKey>
                {
                    InputKey.Apostrophe
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.FreeCamera,
                nameof(GameKeyEnum.FreeCamera), CategoryId, new List<InputKey>
                {
                    InputKey.F10
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.DisableDeath,
                nameof(GameKeyEnum.DisableDeath), CategoryId, new List<InputKey>
                {
                    InputKey.End
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.ControlTroop,
                nameof(GameKeyEnum.ControlTroop), CategoryId, new List<InputKey>
                {
                    InputKey.F
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.ToggleHUD, nameof(GameKeyEnum.ToggleHUD),
                CategoryId, new List<InputKey>
                {
                    InputKey.CloseBraces
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SwitchTeam,
                nameof(GameKeyEnum.SwitchTeam), CategoryId, new List<InputKey>
                {
                    InputKey.F11
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SelectCharacter,
                nameof(GameKeyEnum.SelectCharacter), CategoryId, new List<InputKey>
                {
                    InputKey.SemiColon
                }));
            return result;
        }

        public static IGameKeySequence GetKey(GameKeyEnum key)
        {
            return Category?.GetGameKeySequence((int)key);
        }
    }
}
