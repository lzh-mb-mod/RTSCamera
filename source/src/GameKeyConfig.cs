using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace EnhancedMission
{
    public static class GameKeyCategory
    {
        public static string EnhancedMissionHotKey = nameof(EnhancedMissionHotKey);
    }
    public enum GameKeyEnum
    {
        OpenMenu,
        Pause,
        SlowMotion,
        FreeCamera,
        DisableDeath,
        ControlTroop,
        NumberOfGameKeyEnums,
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
    public class GameKeyConfig : EnhancedMissionConfigBase<GameKeyConfig>
    {
        protected static Version BinaryVersion => new Version(1, 1);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion?.ToString())
            {
                default:
                    Utility.DisplayLocalizedText("str_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.1":
                    break;
            }
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
            }
        }

        private GameKey[] _gameKeys;

        public SerializedGameKey OpenMenuGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.OpenMenu),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.O
        };

        public SerializedGameKey PauseGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.Pause),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.OpenBraces
        };

        public SerializedGameKey SlowMotionGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.SlowMotion),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.Apostrophe
        };

        public SerializedGameKey FreeCameraGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.FreeCamera),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.F10
        };

        public SerializedGameKey DisableDeathGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.DisableDeath),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.F11
        };

        public SerializedGameKey ControlTroopGameKey = new SerializedGameKey
        {
            Id = ToId(GameKeyEnum.ControlTroop),
            StringId = "",
            GroupId = "EnhancedMissionHotKey",
            Key = InputKey.F
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
            ToSerializedGameKeys();

            return base.Serialize();
        }

        public override bool Deserialize()
        {
            if (base.Deserialize())
            {
                FromSerializedGameKeys();
                return true;
            }

            return false;
        }

        protected override void CopyFrom(GameKeyConfig other)
        {
            this.OpenMenuGameKey = other.OpenMenuGameKey;
            this.PauseGameKey = other.PauseGameKey;
            this.SlowMotionGameKey = other.SlowMotionGameKey;
            this.FreeCameraGameKey = other.FreeCameraGameKey;
            this.DisableDeathGameKey = other.DisableDeathGameKey;
            this.ControlTroopGameKey = other.ControlTroopGameKey;
            this._gameKeys = other._gameKeys;
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
        }

        private void FromSerializedGameKeys()
        {
            _gameKeys = new GameKey[(int)GameKeyEnum.NumberOfGameKeyEnums];
            _gameKeys[(int) GameKeyEnum.OpenMenu] = OpenMenuGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.Pause] = PauseGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.SlowMotion] = SlowMotionGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.FreeCamera] = FreeCameraGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.DisableDeath] = DisableDeathGameKey.ToGameKey();
            _gameKeys[(int)GameKeyEnum.ControlTroop] = ControlTroopGameKey.ToGameKey();
        }

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        protected override string SaveName => SavePath + nameof(GameKeyConfig) + ".xml";
        protected override string[] OldNames { get; } = { };
    }
}
