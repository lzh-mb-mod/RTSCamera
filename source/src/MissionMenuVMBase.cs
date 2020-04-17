using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Library;

namespace EnhancedMission
{
    public abstract class MissionMenuVMBase : ViewModel
    {
        private readonly Action _closeMenu;

        public virtual void CloseMenu()
        {
            this._closeMenu?.Invoke();
        }

        protected MissionMenuVMBase(Action closeMenu)
        {
            _closeMenu = closeMenu;
        }
    }
}
