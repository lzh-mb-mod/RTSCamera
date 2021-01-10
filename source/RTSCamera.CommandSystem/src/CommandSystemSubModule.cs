using MissionLibrary.View;
using RTSCamera.CommandSystem.Config;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemSubModule : MBSubModuleBase
    {
        public static readonly string ModuleId = "RTSCamera.CommandSystem";
        public bool _isInitialized = false;
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (_isInitialized)
                return;

            AMenuManager.Get().OnMenuClosedEvent += CommandSystemConfig.OnMenuClosed;
            var menuClassCollection = AMenuManager.Get().MenuClassCollection;
            menuClassCollection.AddOptionClass(CommandSystemOptionClassFactory.CreateOptionClassProvider(menuClassCollection));
        }
    }
}
