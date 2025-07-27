using MissionLibrary.HotKey;
using MissionSharedLibrary.Config.HotKey;
using MissionSharedLibrary.HotKey;
using MissionSharedLibrary.Usage;
using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace RTSCamera.CommandSystem.Config.HotKey
{
    public enum GameKeyEnum
    {
        SelectFormation,
        KeepMovementOrder,
        CommandQueue,
        NumberOfGameKeyEnums
    }
    public class CommandSystemGameKeyCategory
    {
        public const string CategoryId = "RTSCameraCommandSystemHotKey";

        public static AGameKeyCategory Category => AGameKeyCategoryManager.Get().GetItem(CategoryId);

        public static void RegisterGameKeyCategory()
        {
            AGameKeyCategoryManager.Get()?.RegisterGameKeyCategory(CreateCategory, CategoryId, new Version(1, 0));
        }
        public static GameKeyCategory CreateCategory()
        {
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, CommandSystemGameKeyConfig.Get());
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SelectFormation,
                nameof(GameKeyEnum.SelectFormation),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative
                    (
                        new List<InputKey> () {
                            InputKey.MiddleMouseButton
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.KeepMovementOrder,
                nameof(GameKeyEnum.KeepMovementOrder),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftAlt
                        })
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CommandQueue,
                nameof(GameKeyEnum.CommandQueue),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftShift
                        })
                }));
            return result;
        }

        public static IGameKeySequence GetKey(GameKeyEnum key)
        {
            return Category?.GetGameKeySequence((int)key);
        }
    }
}
