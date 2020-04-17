using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Library;

namespace EnhancedMission
{
    public class ExtensionVM : ViewModel
    {
        private Action _clicked;
        public ExtensionVM(string name, Action clicked)
        {
            Name = name;
            _clicked = clicked;
        }

        [DataSourceProperty] public string Name { get; }

        public void Clicked()
        {
            _clicked?.Invoke();
        }
    }
}
