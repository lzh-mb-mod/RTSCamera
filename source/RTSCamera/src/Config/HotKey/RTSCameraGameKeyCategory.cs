using MissionLibrary;
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
        public static MissionSharedLibrary.Config.HotKey.GameKeyCategory Category { get; set; }

        public static void Initialize()
        {
            Category = new MissionSharedLibrary.Config.HotKey.GameKeyCategory(CategoryId,
                (int) GameKeyEnum.NumberOfGameKeyEnums, GameKeyConfig.Get());
            Category.AddGameKey(new GameKey((int) GameKeyEnum.Pause, nameof(GameKeyEnum.Pause),
                CategoryId, InputKey.OpenBraces, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.SlowMotion,
                nameof(GameKeyEnum.SlowMotion), CategoryId, InputKey.Apostrophe, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.FreeCamera,
                nameof(GameKeyEnum.FreeCamera), CategoryId, InputKey.F10, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.DisableDeath,
                nameof(GameKeyEnum.DisableDeath), CategoryId, InputKey.End, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.ControlTroop,
                nameof(GameKeyEnum.ControlTroop), CategoryId, InputKey.F, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.ToggleHUD, nameof(GameKeyEnum.ToggleHUD),
                CategoryId, InputKey.CloseBraces, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.SwitchTeam,
                nameof(GameKeyEnum.SwitchTeam), CategoryId, InputKey.F11, CategoryId));
            Category.AddGameKey(new GameKey((int) GameKeyEnum.SelectCharacter,
                nameof(GameKeyEnum.SelectCharacter), CategoryId, InputKey.SemiColon, CategoryId));
            Global.GameKeyCategoryManager.AddCategories(Category);
        }

        public static InputKey GetKey(GameKeyEnum key)
        {
            return Category?.GetKey((int) key) ?? InputKey.Invalid;
        }

        public static void Clear()
        {
            Category = null;
        }
    }
}
