using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RTSCamera.View
{
    public class SelectCharacterVM : ViewModel
    {
        private string _selectCharacterHintString = GameTexts.FindText("str_rts_camera_select_character_view_hint").ToString();

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
