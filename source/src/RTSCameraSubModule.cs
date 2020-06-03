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
                    typeof(Patch_Formation).GetMethod("LeaveDetachment_Prefix", BindingFlags.Static | BindingFlags.Public)));
            //_harmony.Patch(
            //    typeof(Formation).GetMethod("GetOrderPositionOfUnit", BindingFlags.Instance | BindingFlags.Public),
            //    new HarmonyMethod(
            //        typeof(Patch_Formation).GetMethod("GetOrderPositionOfUnit_Prefix", BindingFlags.Static | BindingFlags.Public)));

            _harmony.Patch(
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserInput", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyMethod(
                    typeof(Patch_RangedSiegeWeaponView).GetMethod("HandleUserInput_Prefix", BindingFlags.Static | BindingFlags.Public)));

            //_harmony.Patch(typeof(MovementOrder).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic),
            //    postfix: new HarmonyMethod(
            //        typeof(Patch_MovementOrder).GetMethod("Tick_Postfix", BindingFlags.Static | BindingFlags.Public)));
            //_harmony.Patch(typeof(MovementOrder).GetMethod("GetPosition", BindingFlags.Instance | BindingFlags.Public),
            //    prefix: new HarmonyMethod(
            //        typeof(Patch_MovementOrder).GetMethod("GetPosition_Prefix", BindingFlags.Static | BindingFlags.Public)));
            //_harmony.Patch(typeof(MovementOrder).GetProperty("MovementState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetMethod,
            //    prefix: new HarmonyMethod(
            //        typeof(Patch_MovementOrder).GetMethod("Get_MovementState_Prefix", BindingFlags.Static | BindingFlags.Public)));

            //_harmony.Patch(typeof(BehaviorCharge).GetMethod("CalculateCurrentOrder", BindingFlags.Instance | BindingFlags.NonPublic),
            //    prefix: new HarmonyMethod(
            //        typeof(Patch_BehaviorCharge).GetMethod("CalculateCurrentOrder_Prefix", BindingFlags.Static | BindingFlags.Public)));
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
