using HarmonyLib;
using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View.Missions;
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

        private static readonly MethodInfo InitializeInADisgustingManner =
            typeof(OrderTroopPlacer).GetMethod("InitializeInADisgustingManner",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _isInSwitchTeamEvent;
        private static bool _willEndDraggingMode;
        private static bool _earlyDraggingMode;
        private static float _beginDraggingOffset;
        private static readonly float _beginDraggingOffsetThreshold = 100;
        private static bool _rightButtonDraggingMode;

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
                Harmony.Patch(
                    typeof(MissionOrderGauntletUIHandler).GetMethod(
                        nameof(MissionOrderGauntletUIHandler.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderGauntletUIHandler).GetMethod(
                        nameof(Prefix_OnMissionScreenTick), BindingFlags.Static | BindingFlags.Public)));
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

        public static bool Prefix_OnMissionScreenInitialize(MissionOrderGauntletUIHandler __instance)
        {
            RegisterReload(__instance);
            return true;
        }

        public static void Postfix_OnMissionScreenFinalize()
        {
            UnregisterReload();
        }


        private static bool ShouldBeginEarlyDragging(MissionOrderGauntletUIHandler __instance)
        {
            return !_earlyDraggingMode &&
                   (__instance.MissionScreen.InputManager.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null) &&
                   __instance.MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton);
        }

        private static void BeginEarlyDragging()
        {
            _earlyDraggingMode = true;
            _beginDraggingOffset = 0;
        }

        private static void EndEarlyDragging()
        {
            _earlyDraggingMode = false;
            _beginDraggingOffset = 0;
        }

        private static bool ShouldBeginDragging()
        {
            return _earlyDraggingMode && _beginDraggingOffset > _beginDraggingOffsetThreshold;
        }

        private static void BeginDrag()
        {
            EndEarlyDragging();
            _rightButtonDraggingMode = true;
            Patch_MissionOrderVM.AllowEscape = false;
        }

        private static void EndDrag()
        {
            EndEarlyDragging();
            _rightButtonDraggingMode = false;
            Patch_MissionOrderVM.AllowEscape = true;
        }

        private static bool IsAnyDeployment(MissionOrderGauntletUIHandler __instance)
        {
            return __instance.IsBattleDeployment || __instance.IsSiegeDeployment;
        }

        private static void UpdateMouseVisibility(MissionOrderGauntletUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer)
        {
            if (__instance == null)
                return;

            bool mouseVisibility =
                (IsAnyDeployment(__instance) || ____dataSource.TroopController.IsTransferActive ||
                 ____dataSource.IsToggleOrderShown && (__instance.Input.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null)) &&
                !_rightButtonDraggingMode && !_earlyDraggingMode;
            if (mouseVisibility != ____gauntletLayer.InputRestrictions.MouseVisibility)
            {
                ____gauntletLayer.InputRestrictions.SetInputRestrictions(mouseVisibility,
                    mouseVisibility ? InputUsageMask.All : InputUsageMask.Invalid);
            }

            if (__instance.MissionScreen.OrderFlag != null)
            {
                bool orderFlagVisibility = (____dataSource.IsToggleOrderShown || IsAnyDeployment(__instance)) &&
                                           !____dataSource.TroopController.IsTransferActive &&
                                           !_rightButtonDraggingMode && !_earlyDraggingMode;
                if (orderFlagVisibility != __instance.MissionScreen.OrderFlag.IsVisible)
                {
                    __instance.MissionScreen.SetOrderFlagVisibility(orderFlagVisibility);
                }
            }
        }

        private static void UpdateDragData(MissionOrderGauntletUIHandler __instance, MissionOrderVM ____dataSource)
        {
            if (_willEndDraggingMode)
            {
                _willEndDraggingMode = false;
                EndDrag();
            }
            else if (!____dataSource.IsToggleOrderShown && !IsAnyDeployment(__instance) || __instance.MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton))
            {
                if (_earlyDraggingMode || _rightButtonDraggingMode)
                    _willEndDraggingMode = true;
            }
            else if (____dataSource.IsToggleOrderShown || IsAnyDeployment(__instance))
            {
                if (ShouldBeginEarlyDragging(__instance))
                {
                    BeginEarlyDragging();
                }
                else if (__instance.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
                {
                    if (ShouldBeginDragging())
                    {
                        BeginDrag();
                    }
                    else if (_earlyDraggingMode)
                    {
                        float inputXRaw = __instance.MissionScreen.SceneLayer.Input.GetMouseMoveX();
                        float inputYRaw = __instance.MissionScreen.SceneLayer.Input.GetMouseMoveY();
                        _beginDraggingOffset += inputYRaw * inputYRaw + inputXRaw * inputXRaw;
                    }
                }
            }
        }

        public static bool Prefix_OnMissionScreenTick(MissionOrderGauntletUIHandler __instance, float dt, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer)
        {
            UpdateDragData(__instance, ____dataSource);
            UpdateMouseVisibility(__instance, ____dataSource, ____gauntletLayer);
            return true;
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
            InitializeInADisgustingManner?.Invoke(Mission.Current.GetMissionBehavior<OrderTroopPlacer>(),
                new object[] { });
            _isInSwitchTeamEvent = false;

        }
    }
}
