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
        CameraMoveForward,
        CameraMoveBackward,
        CameraMoveLeft,
        CameraMoveRight,
        CameraMoveUp,
        CameraMoveDown,
        IncreaseCameraDistanceLimit,
        DecreaseCameraDistanceLimit,
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
            var nativeCategory = HotKeyManager.GetCategory("CombatHotKeyCategory");
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
                    InputKey.E
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
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveForward,
                nameof(GameKeyEnum.CameraMoveForward), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Up")?.KeyboardKey?.InputKey ?? InputKey.W
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveBackward,
                nameof(GameKeyEnum.CameraMoveBackward), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Down")?.KeyboardKey?.InputKey ?? InputKey.S
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveLeft,
                nameof(GameKeyEnum.CameraMoveLeft), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Left")?.KeyboardKey?.InputKey ?? InputKey.A
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveRight,
                nameof(GameKeyEnum.CameraMoveRight), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Right")?.KeyboardKey?.InputKey ?? InputKey.D
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveUp,
                nameof(GameKeyEnum.CameraMoveUp), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Jump")?.KeyboardKey?.InputKey ?? InputKey.Space
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.CameraMoveDown,
                nameof(GameKeyEnum.CameraMoveDown), CategoryId, new List<InputKey>
                {
                    nativeCategory?.GetGameKey("Crouch")?.KeyboardKey?.InputKey ?? InputKey.X
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.IncreaseCameraDistanceLimit,
                nameof(GameKeyEnum.IncreaseCameraDistanceLimit), CategoryId, new List<InputKey>
                {
                    InputKey.RightShift, InputKey.Equals
                }));
            result.AddGameKeySequence(new GameKeySequence((int)GameKeyEnum.DecreaseCameraDistanceLimit,
                nameof(GameKeyEnum.DecreaseCameraDistanceLimit), CategoryId, new List<InputKey>
                {
                    InputKey.RightShift, InputKey.Minus
                }));
            return result;
        }

        public static IGameKeySequence GetKey(GameKeyEnum key)
        {
            return Category?.GetGameKeySequence((int)key);
        }
    }
}
