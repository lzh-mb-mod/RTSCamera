using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class RTSCameraSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            RTSCameraExtension.Clear();
            Module.CurrentModule.GlobalTextManager.LoadGameTexts(BasePath.Name + "Modules/RTSCamera/ModuleData/module_strings.xml");

            new Harmony("LeaveDetachmentPatch").PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            game.GameTextManager.LoadGameTexts(BasePath.Name + "Modules/RTSCamera/ModuleData/module_strings.xml");
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            RTSCameraExtension.Clear();
        }
    }
}
