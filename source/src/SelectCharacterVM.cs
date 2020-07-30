using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RTSCamera
{
    public class SelectCharacterVM : ViewModel
    {
        private string _selectCharacterHintString = GameTexts.FindText("str_em_select_character_hint").ToString();

        [DataSourceProperty]
        public string SelectCharacterHintString
        {
            get => _selectCharacterHintString;
            set
            {
                if (_selectCharacterHintString == value)
                    return;
                _selectCharacterHintString = value;
                OnPropertyChanged(nameof(SelectCharacterHintString));
            }
        }
    }
}
