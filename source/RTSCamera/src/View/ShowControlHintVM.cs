using RTSCamera.Config;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera.View
{
    public class ShowControlHintVM : ViewModel
    {
        private string _text;
        private bool _showText;

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        [DataSourceProperty]
        public bool ShowText
        {
            get => _showText;
            set
            {
                _showText = value;
                if (_showText) 
                    RefreshValues();
                OnPropertyChanged(nameof(ShowText));
            }
        }

        private bool _focusedOnAgent;

        public ShowControlHintVM(bool showText)
        {
            _showText = showText;
            RefreshValues();
        }

        public sealed override void RefreshValues()
        {
            base.RefreshValues();

            Text = GetText().ToString();

        }

        public void SetShowText(bool focusedOnAgent, bool showText)
        {
            _focusedOnAgent = focusedOnAgent;
            ShowText = showText;
        }

        private TextObject GetText()
        {
            GameTexts.SetVariable("KeyName", Utility.TextForKey(GameKeyConfig.Get().GetKey(GameKeyEnum.ControlTroop)));
            if (_focusedOnAgent)
            {
                return GameTexts.FindText("str_rts_camera_control_current_agent_hint");
            }
            else
            {
                return GameTexts.FindText("str_rts_camera_control_troop_hint");
            }
        }
    }
}
