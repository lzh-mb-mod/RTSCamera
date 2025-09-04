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
        FormationLockMovement,
        SelectTargetForCommand,
        CommandQueue,
        KeepFormationWidth,
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
                        }),
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.RightAlt
                        })
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.FormationLockMovement,
                nameof(GameKeyEnum.FormationLockMovement),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftAlt
                        }),
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.RightAlt
                        })
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.SelectTargetForCommand,
                nameof(GameKeyEnum.SelectTargetForCommand),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftAlt
                        }),
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.RightAlt
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
                        }),
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.RightShift
                        })
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.KeepFormationWidth,
                nameof(GameKeyEnum.KeepFormationWidth),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftControl
                        }),
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.RightControl
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
