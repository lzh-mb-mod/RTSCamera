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
        protected static Version BinaryVersion => new Version(1, 0);

        protected override void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion?.ToString())
            {
                default:
                    Utility.DisplayLocalizedText("str_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.0":
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
                yield return GameKeyEnum.FreeCamera;
                yield return GameKeyEnum.DisableDeath;
                yield return GameKeyEnum.ControlTroop;
            }
        }

        private GameKey[] _gameKeys;


        public SerializedGameKey[] GameKeys =
        {
            new SerializedGameKey
                {Id = ToId(GameKeyEnum.OpenMenu), StringId = "", GroupId = "EnhancedMissionHotKey", Key = InputKey.O},
            new SerializedGameKey
            {
                Id = ToId(GameKeyEnum.Pause), StringId = "", GroupId = "EnhancedMissionHotKey",
                Key = InputKey.OpenBraces
            },
            new SerializedGameKey
            {
                Id = ToId(GameKeyEnum.FreeCamera), StringId = "", GroupId = "EnhancedMissionHotKey", Key = InputKey.F10
            },
            new SerializedGameKey
            {
                Id = ToId(GameKeyEnum.DisableDeath), StringId = "", GroupId = "EnhancedMissionHotKey",
                Key = InputKey.F11
            },
            new SerializedGameKey
            {
                Id = ToId(GameKeyEnum.ControlTroop), StringId = "", GroupId = "EnhancedMissionHotKey", Key = InputKey.F
            },
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
            newConfig._gameKeys = newConfig.GameKeys.Select(serializedGameKey => serializedGameKey.ToGameKey()).ToArray();
            return newConfig;
        }

        public override bool Serialize()
        {
            GameKeys = _gameKeys.Select(SerializedGameKey.FromGameKey).ToArray();

            return base.Serialize();
        }

        public override bool Deserialize()
        {
            if (base.Deserialize())
            {
                _gameKeys = GameKeys.Select(serializedGameKey => serializedGameKey.ToGameKey()).ToArray();
                return true;
            }

            return false;
        }

        protected override void CopyFrom(GameKeyConfig other)
        {
            this.GameKeys = other.GameKeys;
            this._gameKeys = other._gameKeys;
        }

        protected override XmlSerializer serializer => new XmlSerializer(typeof(GameKeyConfig));

        public override void ResetToDefault()
        {
            CopyFrom(CreateDefault());
        }

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        protected override string SaveName => SavePath + nameof(GameKeyConfig) + ".xml";
        protected override string[] OldNames { get; } = { };
    }
}
