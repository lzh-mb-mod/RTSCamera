using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using RTSCamera.Config.Basic;
using TaleWorlds.InputSystem;

namespace RTSCamera.Config
{
    public static class GameKeyCategory
    {
        public static string RTSCameraHotKey = nameof(RTSCameraHotKey);
    }
    public enum GameKeyEnum
    {
        OpenMenu,
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

    public struct SerializedGameKey
    {
        public int Id { get; set; }

        public string StringId { get; set; }

        public string GroupId { get; set; }

        public string MainCategoryId { get; set; }

        public InputKey Key { get; set; }

        public static SerializedGameKey FromGameKey(GameKey gameKey)
        {
            return new SerializedGameKey
            {
                Id = gameKey.Id,
                StringId = gameKey.StringId,
                GroupId = gameKey.GroupId,
                MainCategoryId = gameKey.MainCategoryId,
                Key = gameKey.PrimaryKey.InputKey
            };
        }

        public GameKey ToGameKey()
        {
            return new GameKey(Id, StringId, GroupId, Key, MainCategoryId);
        }
    }
    public class GameKeyConfig : RTSCameraConfigBase<GameKeyConfig>
    {
        protected static Version BinaryVersion => new Version(1, 5);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion)
            {
                default:
                    Utility.DisplayLocalizedText("str_rts_camera_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.1":
                    if (DisableDeathGameKey.Key == InputKey.F11)
                    {
                        DisableDeathGameKey.Key = InputKey.End;
                        FromSerializedGameKeys();
                        Serialize();
                    }

                    goto case "1.2";
                case "1.2":
                case "1.3":
                    ResetToDefault();
                    Serialize();
                    goto case "1.4";
                case "1.4":
                    if (OpenMenuGameKey.Key == InputKey.O)
                    {
                        OpenMenuGameKey.Key = InputKey.L;
                        FromSerializedGameKeys();
                        Serialize();
                    }
                    goto case "1.5";
                case "1.5":
                    break;
            }

            ConfigVersion = BinaryVersion.ToString(2);
        }

        [XmlIgnore]
        public IEnumerable<GameKeyEnum> GameKeyEnums
        {
            get
            {
                yield return GameKeyEnum.OpenMenu;
                yield return GameKeyEnum.Pause;
                yield return GameKeyEnum.SlowMotion;
                yield return GameKeyEnum.FreeCamera;
                yield return GameKeyEnum.DisableDeath;
                yield return GameKeyEnum.ControlTroop;
                yield return GameKeyEnum.ToggleHUD;
                yield return GameKeyEnum.SwitchTeam;
                yield return GameKeyEnum.SelectCharacter;
            }
        }

        private GameKey[] _gameKeys;

        public SerializedGameKey OpenMenuGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.OpenMenu),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.L
        };

        public SerializedGameKey PauseGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.Pause),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.OpenBraces
        };

        public SerializedGameKey SlowMotionGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.SlowMotion),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.Apostrophe
        };

        public SerializedGameKey FreeCameraGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.FreeCamera),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.F10
        };

        public SerializedGameKey DisableDeathGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.DisableDeath),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.End
        };

        public SerializedGameKey ControlTroopGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.ControlTroop),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.F
        };

        public SerializedGameKey ToggleHUDGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.ToggleHUD),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.CloseBraces
        };

        public SerializedGameKey SwitchTeamGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.SwitchTeam),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.F11
        };

        public SerializedGameKey SelectCharacterGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.SelectCharacter),
            StringId = "",
            GroupId = "RTSCameraHotKey",
            Key = InputKey.SemiColon
        };

        private static GameKeyConfig _instance;

        public static GameKeyConfig Get()
        {
            if (_instance == null)
            {
                _instance = CreateDefault();
                _instance.SyncWithSave();
            }

            return _instance;
        }

        public static void Clear()
        {
            _instance = null;
        }

        public InputKey GetKey(GameKeyEnum gameKeyEnum)
        {
            return GetGameKey(gameKeyEnum).PrimaryKey.InputKey;
        }

        public void SetKey(GameKeyEnum gameKeyEnum, InputKey inputKey)
        {
            GetGameKey(gameKeyEnum).PrimaryKey.ChangeKey(inputKey);
        }

        public GameKey GetGameKey(GameKeyEnum gameKeyEnum)
        {
            if (gameKeyEnum >= GameKeyEnum.NumberOfGameKeyEnums)
            {
                Utility.DisplayMessage("Error: Game key not registered.");
                return null;
            }

            return _gameKeys[(int)gameKeyEnum];
        }

        private static int ToId(GameKeyEnum gameKeyEnum)
        {
            return (int)gameKeyEnum;
        }
        private static GameKeyConfig CreateDefault()
        {
            var newConfig = new GameKeyConfig();
            newConfig.FromSerializedGameKeys();
            return newConfig;
        }

        public override bool Serialize()
        {
            try
            {

                ToSerializedGameKeys();
                return base.Serialize();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

        }

        public override bool Deserialize()
        {
            try
            {
                if (base.Deserialize())
                {
                    FromSerializedGameKeys();
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        protected override void CopyFrom(GameKeyConfig other)
        {
            ConfigVersion = other.ConfigVersion;
            OpenMenuGameKey = other.OpenMenuGameKey;
            PauseGameKey = other.PauseGameKey;
            SlowMotionGameKey = other.SlowMotionGameKey;
            FreeCameraGameKey = other.FreeCameraGameKey;
            DisableDeathGameKey = other.DisableDeathGameKey;
            ControlTroopGameKey = other.ControlTroopGameKey;
            ToggleHUDGameKey = other.ToggleHUDGameKey;
            SwitchTeamGameKey = other.SwitchTeamGameKey;
            SelectCharacterGameKey = other.SelectCharacterGameKey;
            _gameKeys = other._gameKeys;
        }

        protected override XmlSerializer serializer => new XmlSerializer(typeof(GameKeyConfig));

        public override void ResetToDefault()
        {
            CopyFrom(CreateDefault());
        }

        private void ToSerializedGameKeys()
        {
            OpenMenuGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.OpenMenu));
            PauseGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.Pause));
            SlowMotionGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.SlowMotion));
            FreeCameraGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.FreeCamera));
            DisableDeathGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.DisableDeath));
            ControlTroopGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.ControlTroop));
            ToggleHUDGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.ToggleHUD));
            SwitchTeamGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.SwitchTeam));
            SelectCharacterGameKey = SerializedGameKey.FromGameKey(GetGameKey(GameKeyEnum.SelectCharacter));
        }

        private void FromSerializedGameKeys()
        {
            _gameKeys = new GameKey[(int)GameKeyEnum.NumberOfGameKeyEnums];
            _gameKeys[(int)GameKeyEnum.OpenMenu] = OpenMenuGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.Pause] = PauseGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.SlowMotion] = SlowMotionGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.FreeCamera] = FreeCameraGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.DisableDeath] = DisableDeathGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.ControlTroop] = ControlTroopGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.ToggleHUD] = ToggleHUDGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.SwitchTeam] = SwitchTeamGameKey.ToGameKey();
            _gameKeys[(int) GameKeyEnum.SelectCharacter] = SelectCharacterGameKey.ToGameKey();
        }

        public string ConfigVersion { get; set; } = BinaryVersion.ToString(2);

        protected override string SaveName => Path.Combine(SavePath, nameof(GameKeyConfig) + ".xml");
        protected override string[] OldNames { get; } = { Path.Combine(OldSavePath, "GameKeyConfig.xml") };
    }
}
