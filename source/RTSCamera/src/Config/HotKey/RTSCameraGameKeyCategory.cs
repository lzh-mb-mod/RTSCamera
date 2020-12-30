using MissionLibrary.HotKey;
using MissionSharedLibrary.HotKey.Category;
using System;
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

        public static void Initialize()
        {
            AGameKeyCategoryManager.Get()?.AddCategory(CreateCategory, new Version(1, 0));
        }

        public static GameKeyCategory CreateCategory()
        {
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, GameKeyConfig.Get());
            result.AddGameKey(new GameKey((int)GameKeyEnum.Pause, nameof(GameKeyEnum.Pause),
                CategoryId, InputKey.OpenBraces, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.SlowMotion,
                nameof(GameKeyEnum.SlowMotion), CategoryId, InputKey.Apostrophe, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.FreeCamera,
                nameof(GameKeyEnum.FreeCamera), CategoryId, InputKey.F10, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.DisableDeath,
                nameof(GameKeyEnum.DisableDeath), CategoryId, InputKey.End, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.ControlTroop,
                nameof(GameKeyEnum.ControlTroop), CategoryId, InputKey.F, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.ToggleHUD, nameof(GameKeyEnum.ToggleHUD),
                CategoryId, InputKey.CloseBraces, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.SwitchTeam,
                nameof(GameKeyEnum.SwitchTeam), CategoryId, InputKey.F11, CategoryId));
            result.AddGameKey(new GameKey((int)GameKeyEnum.SelectCharacter,
                nameof(GameKeyEnum.SelectCharacter), CategoryId, InputKey.SemiColon, CategoryId));
            return result;
        }

        public static InputKey GetKey(GameKeyEnum key)
        {
            return Category?.GetKey((int) key) ?? InputKey.Invalid;
        }
    }
}
