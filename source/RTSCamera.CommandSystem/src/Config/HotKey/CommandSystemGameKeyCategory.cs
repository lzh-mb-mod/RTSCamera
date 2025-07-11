using MissionLibrary.HotKey;
using MissionSharedLibrary.Config.HotKey;
using MissionSharedLibrary.HotKey;
using System;
using System.Collections.Generic;
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

        public static AGameKeyCategory Category => AGameKeyCategoryManager.Get().GetItem(CategoryId);

        public static void RegisterGameKeyCategory()
        {
            AGameKeyCategoryManager.Get()?.RegisterCategory(CreateCategory, CategoryId, new Version(1, 0));
        }
        public static GameKeyCategory CreateCategory()
        {
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, CommandSystemGameKeyConfig.Get());
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SelectFormation,
                nameof(GameKeyEnum.SelectFormation),
                CategoryId, new List<InputKey>
                {
                    InputKey.MiddleMouseButton
                }));
            return result;
        }

        public static IGameKeySequence GetKey(GameKeyEnum key)
        {
            return Category?.GetGameKeySequence((int)key);
        }
    }
}
