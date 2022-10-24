using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionOrderDeploymentControllerVM
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionOrderDeploymentControllerVM));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
                    typeof(MissionOrderDeploymentControllerVM).GetMethod(nameof(MissionOrderDeploymentControllerVM.ExecuteDeployAll),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderDeploymentControllerVM).GetMethod(
                        nameof(Prefix_ExecuteDeployAll), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_ExecuteDeployAll(MissionOrderDeploymentControllerVM __instance)
        {
            if (RTSCameraLogic.Instance != null && Mission.Current != null)
            {
                if (WatchBattleBehavior.WatchMode && Mission.Current.MainAgent == null)
                {
                    RTSCameraLogic.Instance.ControlTroopLogic.SetMainAgent();
                    if (Mission.Current.MainAgent != null)
                    {
                        Utility.SetIsPlayerAgentAdded(RTSCameraLogic.Instance.ControlTroopLogic.MissionScreen, true);
                        if (Mission.Current.PlayerTeam.IsPlayerGeneral)
                            Utility.SetPlayerAsCommander(true);
                        Mission.Current.PlayerTeam.PlayerOrderController?.SelectAllFormations();
                    }
                }
            }

            return true;
        }
    }
}
