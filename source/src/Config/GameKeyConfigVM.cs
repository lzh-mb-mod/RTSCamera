using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace RTSCamera.Config
{
    class GameKeyConfigVM : ViewModel
    {
        private Dictionary<GameKey, InputKey> _keysToChangeOnDone = new Dictionary<GameKey, InputKey>();
        private readonly Action<GameKeyOptionVM> _onKeyBindRequest;
        private readonly Action _onClose;
        private GameKeyConfig _config;

        private Dictionary<string, List<GameKey>> _categories;
        private string _name;
        private MBBindingList<GameKeyGroupVM> _groups;
        private string _cancelLbl;
        private string _doneLbl;

        public GameKeyConfigVM(Action<GameKeyOptionVM> onKeyBindRequest, Action onClose)
        {
            _onKeyBindRequest = onKeyBindRequest;
            _onClose = onClose;
            _config = GameKeyConfig.Get();
            _categories = new Dictionary<string, List<GameKey>>
            {
                {
                    GameKeyCategory.RTSCameraHotKey,
                    _config.GameKeyEnums.Select(gameKeyEnum => _config.GetGameKey(gameKeyEnum)).ToList()
                }
            };
            Groups = new MBBindingList<GameKeyGroupVM>();
            foreach (var category in _categories)
            {
                if (category.Value.Count > 0)
                    Groups.Add(new GameKeyGroupVM(category.Key, category.Value, _onKeyBindRequest, UpdateKeysOfGameKeysWithID));
            }
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            CancelLbl = new TextObject("{=3CpNUnVl}Cancel").ToString();
            DoneLbl = new TextObject("{=WiNRdfsm}Done").ToString();
            Name = "ConfigKey";
            Groups.ApplyActionOnAllItems(x => x.RefreshValues());
        }

        public void OnReset()
        {
            foreach (GameKeyGroupVM group in Groups)
                group.OnReset();
            _keysToChangeOnDone.Clear();
        }

        public void OnDone()
        {
            foreach (GameKeyGroupVM group in Groups)
                group.OnDone();
            foreach (KeyValuePair<GameKey, InputKey> keyValuePair in _keysToChangeOnDone)
                keyValuePair.Key.PrimaryKey.ChangeKey(keyValuePair.Value);
            _config.Serialize();
        }

        private void UpdateKeysOfGameKeysWithID(int givenId, InputKey newKey)
        {
            foreach (var category in _categories)
            {
                    foreach (GameKey key in category.Value.Where(k => k != null && k.Id == givenId))
                    {
                        if (_keysToChangeOnDone.ContainsKey(key))
                            _keysToChangeOnDone[key] = newKey;
                        else
                            _keysToChangeOnDone.Add(key, newKey);
                    }
            }
        }
        public void ExecuteCancel()
        {
            OnReset();
            _onClose();
        }

        public void ExecuteDone()
        {
            OnDone();
            _onClose();
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public MBBindingList<GameKeyGroupVM> Groups
        {
            get => _groups;
            set
            {
                if (value == _groups)
                    return;
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        [DataSourceProperty]
        public string CancelLbl
        {
            get => _cancelLbl;
            set
            {
                if (value == _cancelLbl)
                    return;
                _cancelLbl = value;
                OnPropertyChanged(nameof(CancelLbl));
            }
        }

        [DataSourceProperty]
        public string DoneLbl
        {
            get => _doneLbl;
            set
            {
                if (value == _doneLbl)
                    return;
                _doneLbl = value;
                OnPropertyChanged(nameof(DoneLbl));
            }
        }
    }
}
