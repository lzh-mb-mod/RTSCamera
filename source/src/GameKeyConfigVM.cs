using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace EnhancedMission
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
            this._onKeyBindRequest = onKeyBindRequest;
            this._onClose = onClose;
            this._config = GameKeyConfig.Get();
            this._categories = new Dictionary<string, List<GameKey>>()
            {
                {
                    GameKeyCategory.EnhancedMissionHotKey,
                    _config.GameKeyEnums.Select(gameKeyEnum => _config.GetGameKey(gameKeyEnum)).ToList()
                },
            };
            this.Groups = new MBBindingList<GameKeyGroupVM>();
            foreach (var category in this._categories)
            {
                if (category.Value.Count > 0)
                    this.Groups.Add(new GameKeyGroupVM(category.Key, category.Value, _onKeyBindRequest, UpdateKeysOfGameKeysWithID));
            }
            this.RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            this.CancelLbl = new TextObject("{=3CpNUnVl}Cancel").ToString();
            this.DoneLbl = new TextObject("{=WiNRdfsm}Done").ToString();
            this.Name = "ConfigKey";
            this.Groups.ApplyActionOnAllItems((Action<GameKeyGroupVM>)(x => x.RefreshValues()));
        }

        public void OnReset()
        {
            foreach (GameKeyGroupVM group in Groups)
                group.OnReset();
            this._keysToChangeOnDone.Clear();
        }

        public void OnDone()
        {
            foreach (GameKeyGroupVM group in Groups)
                group.OnDone();
            foreach (KeyValuePair<GameKey, InputKey> keyValuePair in this._keysToChangeOnDone)
                keyValuePair.Key.PrimaryKey.ChangeKey(keyValuePair.Value);
            _config.Serialize();
        }

        private void UpdateKeysOfGameKeysWithID(int givenId, InputKey newKey)
        {
            foreach (var category in _categories)
            {
                    foreach (GameKey key in category.Value.Where<GameKey>((Func<GameKey, bool>)(k => k != null && k.Id == givenId)))
                    {
                        if (this._keysToChangeOnDone.ContainsKey(key))
                            this._keysToChangeOnDone[key] = newKey;
                        else
                            this._keysToChangeOnDone.Add(key, newKey);
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
            get => this._name;
            set
            {
                if (value == this._name)
                    return;
                this._name = value;
                this.OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public MBBindingList<GameKeyGroupVM> Groups
        {
            get => this._groups;
            set
            {
                if (value == this._groups)
                    return;
                this._groups = value;
                this.OnPropertyChanged(nameof(Groups));
            }
        }

        [DataSourceProperty]
        public string CancelLbl
        {
            get => this._cancelLbl;
            set
            {
                if (value == this._cancelLbl)
                    return;
                this._cancelLbl = value;
                this.OnPropertyChanged(nameof(CancelLbl));
            }
        }

        [DataSourceProperty]
        public string DoneLbl
        {
            get => this._doneLbl;
            set
            {
                if (value == this._doneLbl)
                    return;
                this._doneLbl = value;
                this.OnPropertyChanged(nameof(DoneLbl));
            }
        }
    }
}
