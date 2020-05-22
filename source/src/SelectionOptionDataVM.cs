using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class SelectionOptionDataVM : ViewModel
    {
        private readonly int _initialValue;
        private SelectionOptionData _selectionOptionData;
        private SelectorVM<SelectorItemVM> _selector;
        private string _name;

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> Selector
        {
            get => this._selector;
            set
            {
                if (value == this._selector)
                    return;
                this._selector = value;
                this.OnPropertyChanged(nameof(Selector));
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public SelectionOptionDataVM(SelectionOptionData option, TextObject name)
        {
            this._selectionOptionData = option;
            this.Name = name.ToString();

            IEnumerable<SelectionItem> selectableItems = option.GetSelectableOptionNames();
            if (selectableItems.All<SelectionItem>((Func<SelectionItem, bool>)(n => n.IsLocalizationId)))
            {
                List<TextObject> textObjectList = new List<TextObject>();
                foreach (SelectionItem selectionItem in selectableItems)
                {
                    TextObject text = GameTexts.FindText(selectionItem.Data, selectionItem.Variation);
                    textObjectList.Add(text);
                }
                this._selector = new SelectorVM<SelectorItemVM>((IEnumerable<TextObject>)textObjectList, (int)this._selectionOptionData.GetValue(), new Action<SelectorVM<SelectorItemVM>>(this.UpdateValue));
            }
            else
            {
                List<string> stringList = new List<string>();
                foreach (SelectionItem selectionItem in selectableItems)
                {
                    if (selectionItem.IsLocalizationId)
                    {
                        TextObject text = GameTexts.FindText(selectionItem.Data, selectionItem.Variation);
                        stringList.Add(text.ToString());
                    }
                    else
                        stringList.Add(selectionItem.Data);
                }
                this._selector = new SelectorVM<SelectorItemVM>((IEnumerable<string>)stringList, (int)this._selectionOptionData.GetValue(), new Action<SelectorVM<SelectorItemVM>>(this.UpdateValue));
            }
            this._initialValue = (int)this._selectionOptionData.GetValue();
            this.Selector.SelectedIndex = this._initialValue;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            this._selector?.RefreshValues();
        }

        public void UpdateValue(SelectorVM<SelectorItemVM> selector)
        {
            if (selector.SelectedIndex < 0)
                return;
            this._selectionOptionData.SetValue((float)selector.SelectedIndex);
            this._selectionOptionData.Commit();
        }
    }
}
