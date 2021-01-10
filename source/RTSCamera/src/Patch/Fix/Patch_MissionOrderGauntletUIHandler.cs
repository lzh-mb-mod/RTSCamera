using System;
using System.Reflection;
using HarmonyLib;
using MissionLibrary.Event;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace RTSCamera.Patch.Fix
{
    // reload VM when switch player's team to avoid crash
    public class Patch_MissionOrderGauntletUIHandler
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionOrderGauntletUIHandler));
        private static bool _patched;
        private static MissionOrderGauntletUIHandler _uiHandler;

        private static FieldInfo _dataSource =
            typeof(MissionOrderGauntletUIHandler).GetField(nameof(_dataSource),
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
                    typeof(MissionOrderGauntletUIHandler).GetMethod("OnMissionScreenInitialize",
                        BindingFlags.Public | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderGauntletUIHandler).GetMethod(
                        nameof(Prefix_OnMissionScreenInitialize), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionOrderGauntletUIHandler).GetMethod("OnMissionScreenFinalize",
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
                RTSCamera.Utility.DisplayMessage(e.ToString());
            }
        }

        public static bool Prefix_OnMissionScreenInitialize(MissionOrderGauntletUIHandler __instance)
        {
            RegisterReload(__instance);
            return true;
        }

        public static void Postfix_OnMissionScreenFinalize()
        {
            UnregisterReload();
        }

        private static void RegisterReload(MissionOrderGauntletUIHandler uiHandler)
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
            _isInSwitchTeamEvent = false;
        }
    }
}
