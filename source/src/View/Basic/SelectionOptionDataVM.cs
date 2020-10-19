using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RTSCamera.View.Basic
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
            get => _selector;
            set
            {
                if (value == _selector)
                    return;
                _selector = value;
                OnPropertyChanged(nameof(Selector));
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
            _selectionOptionData = option;
            Name = name.ToString();

            IEnumerable<SelectionItem> selectableItems = option.GetSelectableOptionNames();
            if (selectableItems.All(n => n.IsLocalizationId))
            {
                List<TextObject> textObjectList = new List<TextObject>();
                foreach (SelectionItem selectionItem in selectableItems)
                {
                    TextObject text = GameTexts.FindText(selectionItem.Data, selectionItem.Variation);
                    textObjectList.Add(text);
                }
                _selector = new SelectorVM<SelectorItemVM>(textObjectList, (int)_selectionOptionData.GetValue(), UpdateValue);
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
                _selector = new SelectorVM<SelectorItemVM>(stringList, (int)_selectionOptionData.GetValue(), UpdateValue);
            }
            _initialValue = (int)_selectionOptionData.GetValue();
            Selector.SelectedIndex = _initialValue;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            _selector?.RefreshValues();
        }

        public void UpdateValue(SelectorVM<SelectorItemVM> selector)
        {
            _selectionOptionData.SetValue(selector.SelectedIndex);
            _selectionOptionData.Commit();
        }
    }
}
