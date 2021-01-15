using System;
using MissionLibrary.HotKey;
using MissionSharedLibrary.HotKey.Category;
using TaleWorlds.InputSystem;

namespace RTSCamera.CommandSystem.Config.HotKey
{
    public enum GameKeyEnum
    {
        SelectFormation,
        NumberOfGameKeyEnums
    }
    public class CommandSystemGameKeyCategory
    {
        public const string CategoryId = "RTSCameraCommandSystemHotKey";

        public static AGameKeyCategory Category => AGameKeyCategoryManager.Get().GetCategory(CategoryId);

        public static void RegisterGameKeyCategory()
        {
            AGameKeyCategoryManager.Get()?.AddCategory(CreateCategory, new Version(1, 0));
        }
        public static GameKeyCategory CreateCategory()
        {
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, CommandSystemGameKeyConfig.Get());
            result.AddGameKey(new GameKey((int) GameKeyEnum.SelectFormation, nameof(GameKeyEnum.SelectFormation),
                CategoryId, InputKey.MiddleMouseButton, CategoryId));
            return result;
        }

        public static InputKey GetKey(GameKeyEnum key)
        {
            return Category?.GetKey((int)key) ?? InputKey.Invalid;
        }
    }
}
