using MissionSharedLibrary.View.ViewModelCollection.Basic;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RTSCamera.View
{
    public class HotKeyHint
    {
        public string Key { get; set; }
        public TextObject Description { get; set; }
        
    }

    public class HotKeyHintVM : ViewModel
    {
        public HotKeyHintVM(HotKeyHint hotkeyHint)
        {
            Key = new TextViewModel(new TextObject(hotkeyHint.Key));
            Description = new TextViewModel(hotkeyHint.Description);
        }

        [DataSourceProperty]
        public TextViewModel Key { get; }

        [DataSourceProperty]
        public TextViewModel Description { get; }
    }


    public class HotKeyHintCollectionVM: ViewModel
    {
        private readonly Action _close;

        public void Close()
        {
            _close?.Invoke();
        }

        public HotKeyHintCollectionVM(Action close, List<HotKeyHint> hotKeyHints)
        {
            _close = close;
            foreach (var hotKeyHint in hotKeyHints)
            {
                HotKeyHintList.Add(new HotKeyHintVM(hotKeyHint));
            }
        }


        [DataSourceProperty]
        public MBBindingList<HotKeyHintVM> HotKeyHintList { get; } = new MBBindingList<HotKeyHintVM>();
    }
}