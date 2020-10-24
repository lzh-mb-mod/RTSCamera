using System;
using TaleWorlds.Library;

namespace RTSCamera.View
{
    public class ExtensionVM : ViewModel
    {
        private readonly Action _clicked;
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
