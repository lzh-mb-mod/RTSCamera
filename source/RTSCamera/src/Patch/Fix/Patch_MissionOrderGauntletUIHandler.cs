using System;
using System.Reflection;
using HarmonyLib;
using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    // reload VM when switch player's team to avoid crash
    public class Patch_MissionOrderGauntletUIHandler
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionOrderGauntletUIHandler));
        private static bool _patched;        
        private static MissionGauntletSingleplayerOrderUIHandler _uiHandler;

        private static FieldInfo _dataSource =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo InitializeInADisgustingManner =
            typeof(OrderTroopPlacer).GetMethod("InitializeInADisgustingManner",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool _isInSwitchTeamEvent;

        public static void Patch()
        {
            try
            {
                if (_patched)
                    return;
                _patched = true;

                Harmony.Patch(
                    typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod("OnMissionScreenInitialize",
                        BindingFlags.Public | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderGauntletUIHandler).GetMethod(
                        nameof(Prefix_OnMissionScreenInitialize), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod("OnMissionScreenFinalize",
                        BindingFlags.Public | BindingFlags.Instance),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderGauntletUIHandler).GetMethod(
                        nameof(Postfix_OnMissionScreenFinalize), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }


        public static void UnPatch()
        {
            try
            {
                if (!_patched)
                    return;
                _patched = false;
                Harmony.UnpatchAll(Harmony.Id);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        public static bool Prefix_OnMissionScreenInitialize(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            RegisterReload(__instance);
            return true;
        }

        public static void Postfix_OnMissionScreenFinalize()
        {
            UnregisterReload();
        }

        private static void RegisterReload(MissionGauntletSingleplayerOrderUIHandler uiHandler)
        {
            if (_isInSwitchTeamEvent)
                return;
            _uiHandler = uiHandler;
            MissionEvent.PreSwitchTeam += OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam += OnPostSwitchTeam;
        }

        private static void UnregisterReload()
        {
            if (_isInSwitchTeamEvent)
                return;
            _uiHandler = null;
            MissionEvent.PreSwitchTeam -= OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam -= OnPostSwitchTeam;
        }
        private static void OnPreSwitchTeam()
        {
            (_dataSource?.GetValue(_uiHandler) as MissionOrderVM)?.TryCloseToggleOrder();
            _isInSwitchTeamEvent = true;
            _uiHandler.OnMissionScreenFinalize();
            _isInSwitchTeamEvent = false;
        }

        private static void OnPostSwitchTeam()
        {
            _isInSwitchTeamEvent = true;
            _uiHandler.OnMissionScreenInitialize();
            _uiHandler.OnMissionScreenActivate();
            InitializeInADisgustingManner?.Invoke(Mission.Current.GetMissionBehavior<OrderTroopPlacer>(),
                new object[] { });
            _isInSwitchTeamEvent = false;
        }
    }
}
