using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using SandBox.Missions.MissionLogics.Hideout;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_HideoutMissionController
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(HideoutMissionController).GetMethod(nameof(HideoutMissionController.StartBossFightBattleMode),
                        BindingFlags.Static | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_HideoutMissionController).GetMethod(
                        nameof(Postfix_StartBossFightBattleMode), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(HideoutMissionController).GetMethod(nameof(HideoutMissionController.OnAgentRemoved),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_HideoutMissionController).GetMethod(
                        nameof(Postfix_OnAgentRemoved), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }
        public static void Postfix_StartBossFightBattleMode()
        {
            var rtsCameraLogic = Mission.Current.GetMissionBehavior<RTSCameraLogic>();
            if (rtsCameraLogic == null)
            {
                return;
            }
            if (RTSCameraConfig.Get().FastForwardHideout == FastForwardHideout.Always)
            {
                rtsCameraLogic.SwitchFreeCameraLogic.FastForwardHideoutNextTick = true;
            }
        }

        public static void Postfix_OnAgentRemoved(
            HideoutMissionController __instance,
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            if (affectedAgent.IsMainAgent)
            {
                var formation = __instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.FirstOrDefault();
                if (formation != null)
                {
                    if (formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Retreat)
                    {
                        __instance.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
                    }
                }
            }
        }
    }
}
