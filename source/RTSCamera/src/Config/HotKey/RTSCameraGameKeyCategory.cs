using MissionLibrary.HotKey;
using MissionSharedLibrary.Config.HotKey;
using MissionSharedLibrary.HotKey;
using MissionSharedLibrary.Usage;
using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Config.HotKey
{
    public enum GameKeyEnum
    {
        Pause,
        SlowMotion,
        Fastforward,
        FreeCamera,
        DisableDeath,
        ControlTroop,
        ToggleHUD,
        SwitchTeam,
        SelectCharacter,
        CameraMoveForward,
        CameraMoveBackward,
        CameraMoveLeft,
        CameraMoveRight,
        CameraMoveUp,
        CameraMoveDown,
        IncreaseCameraDistanceLimit,
        DecreaseCameraDistanceLimit,
        IncreaseCameraSpeed,
        DecreaseCameraSpeed,
        ResetCameraSpeed,
        CameraMoveByMouse,
        NumberOfGameKeyEnums
    }
    public class RTSCameraGameKeyCategory
    {
        public const string CategoryId = "RTSCameraHotKey";

        public static AGameKeyCategory Category => AGameKeyCategoryManager.Get().GetItem(CategoryId);

        public static void RegisterGameKeyCategory()
        {
            AGameKeyCategoryManager.Get()?.RegisterGameKeyCategory(CreateCategory, CategoryId, new Version(1, 0));
        }

        public static GameKeyCategory CreateCategory()
        {
            var combatHotKeyCategory = HotKeyManager.GetCategory("CombatHotKeyCategory");
            var result = new GameKeyCategory(CategoryId,
                (int)GameKeyEnum.NumberOfGameKeyEnums, GameKeyConfig.Get());
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.Pause, nameof(GameKeyEnum.Pause),
                CategoryId, new List<GameKeySequenceAlternative>
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SlowMotion,
                nameof(GameKeyEnum.SlowMotion), CategoryId, new List<GameKeySequenceAlternative>
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.Fastforward,
                nameof(GameKeyEnum.Fastforward), CategoryId, new List<GameKeySequenceAlternative>
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.FreeCamera,
                nameof(GameKeyEnum.FreeCamera), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.F10
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.DisableDeath,
                nameof(GameKeyEnum.DisableDeath), CategoryId, new List<GameKeySequenceAlternative>()
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.ControlTroop,
                nameof(GameKeyEnum.ControlTroop), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.E
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.ToggleHUD, nameof(GameKeyEnum.ToggleHUD),
                CategoryId, new List<GameKeySequenceAlternative>()
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SwitchTeam,
                nameof(GameKeyEnum.SwitchTeam), CategoryId, new List<GameKeySequenceAlternative>
                {
                }));
            result.AddGameKeySequence(new GameKeySequence((int) GameKeyEnum.SelectCharacter,
                nameof(GameKeyEnum.SelectCharacter), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.SemiColon
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveForward,
                nameof(GameKeyEnum.CameraMoveForward), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(GenericGameKeyContext.Up)?.KeyboardKey?.InputKey ?? InputKey.W
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveBackward,
                nameof(GameKeyEnum.CameraMoveBackward), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(GenericGameKeyContext.Down)?.KeyboardKey?.InputKey ?? InputKey.S
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveLeft,
                nameof(GameKeyEnum.CameraMoveLeft), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(GenericGameKeyContext.Left)?.KeyboardKey?.InputKey ?? InputKey.A
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveRight,
                nameof(GameKeyEnum.CameraMoveRight), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(GenericGameKeyContext.Right)?.KeyboardKey?.InputKey ?? InputKey.D
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveUp,
                nameof(GameKeyEnum.CameraMoveUp), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(CombatHotKeyCategory.Jump)?.KeyboardKey?.InputKey ?? InputKey.Space
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveDown,
                nameof(GameKeyEnum.CameraMoveDown), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            combatHotKeyCategory?.GetGameKey(CombatHotKeyCategory.Crouch)?.KeyboardKey?.InputKey ?? InputKey.X
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.IncreaseCameraDistanceLimit,
                nameof(GameKeyEnum.IncreaseCameraDistanceLimit), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.RightShift, InputKey.Equals
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.DecreaseCameraDistanceLimit,
                nameof(GameKeyEnum.DecreaseCameraDistanceLimit), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.RightShift, InputKey.Minus
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.IncreaseCameraSpeed,
                nameof(GameKeyEnum.IncreaseCameraSpeed), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.RightControl, InputKey.Up
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.DecreaseCameraSpeed,
                nameof(GameKeyEnum.DecreaseCameraSpeed), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.RightControl, InputKey.Down
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.ResetCameraSpeed,
                nameof(GameKeyEnum.ResetCameraSpeed), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey> () {
                            InputKey.RightControl, InputKey.MiddleMouseButton
                        }
                    )
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveByMouse,
                nameof(GameKeyEnum.CameraMoveByMouse), CategoryId, new List<GameKeySequenceAlternative>()
                {
                    new GameKeySequenceAlternative(
                        new List<InputKey>()
                        {
                            InputKey.LeftControl, InputKey.MiddleMouseButton
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
