using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions.SiegeWeapon;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera
{
    public class RTSCameraSubModule : MBSubModuleBase
    {
        private readonly Harmony _harmony = new Harmony("RTSCameraPatch");
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            RTSCameraExtension.Clear();
            Module.CurrentModule.GlobalTextManager.LoadGameTexts(BasePath.Name + "Modules/RTSCamera/ModuleData/module_strings.xml");


            _harmony.Patch(
                typeof(Formation).GetMethod("LeaveDetachment", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyMethod(
                    typeof(Formation_LeaveDetachmentPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
            _harmony.Patch(
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserInput", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyMethod(
                    typeof(RangedSiegeWeaponView_HandleUserInputPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
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
            _harmony.UnpatchAll(_harmony.Id);
        }
    }
}
