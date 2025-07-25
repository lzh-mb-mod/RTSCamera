﻿using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    // reload VM when switch player's team to avoid crash
    public class Patch_MissionGauntletSingleplayerOrderUIHandler
    {
        private static bool _patched;
        private static MissionGauntletSingleplayerOrderUIHandler _uiHandler;

        private static FieldInfo _dataSource =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo InitializeInADisgustingManner =
            typeof(OrderTroopPlacer).GetMethod("InitializeInADisgustingManner",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _isInSwitchTeamEvent;
        private static bool _willEndDraggingMode;
        private static bool _earlyDraggingMode;
        private static float _beginDraggingOffset;
        private static readonly float _beginDraggingOffsetThreshold = 10;
        private static bool _rightButtonDraggingMode;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod("OnMissionScreenInitialize",
                        BindingFlags.Public | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(Prefix_OnMissionScreenInitialize), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod("OnMissionScreenFinalize",
                        BindingFlags.Public | BindingFlags.Instance),
                    postfix: new HarmonyMethod(typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(Postfix_OnMissionScreenFinalize), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(MissionGauntletSingleplayerOrderUIHandler.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(Postfix_OnMissionScreenTick), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                return false;
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


        private static bool ShouldBeginEarlyDragging(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            return !_earlyDraggingMode &&
                   (__instance.MissionScreen.InputManager.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null) && IsDragKeyPressed(__instance);
        }

        private static bool IsDragKeyPressed(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.ControllerLTrigger);
        }

        private static bool IsDragKeyDown(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger);
        }

        private static bool IsDragKeyReleased(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.ControllerLTrigger);
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

        private static bool IsAnyDeployment(MissionGauntletSingleplayerOrderUIHandler __instance)
        {
            return __instance.IsBattleDeployment || __instance.IsSiegeDeployment;
        }

        private static void UpdateMouseVisibility(MissionGauntletSingleplayerOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer)
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

        private static void UpdateDragData(MissionGauntletSingleplayerOrderUIHandler __instance, MissionOrderVM ____dataSource)
        {
            if (_willEndDraggingMode)
            {
                _willEndDraggingMode = false;
                EndDrag();
            }
            else if (!____dataSource.IsToggleOrderShown && !IsAnyDeployment(__instance) || IsDragKeyReleased(__instance))
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
                else if (IsDragKeyDown(__instance))
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

        public static void Postfix_OnMissionScreenTick(MissionGauntletSingleplayerOrderUIHandler __instance, ref float ____latestDt, ref bool ____isReceivingInput, float dt, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer)
        {
            UpdateDragData(__instance, ____dataSource);
            UpdateMouseVisibility(__instance, ____dataSource, ____gauntletLayer);
            //return true;
        }

        private static void RegisterReload(MissionGauntletSingleplayerOrderUIHandler uiHandler)
        {
            if (_isInSwitchTeamEvent)
                return;
            _uiHandler = uiHandler;
            MissionLibrary.Event.MissionEvent.PreSwitchTeam += OnPreSwitchTeam;
            MissionLibrary.Event.MissionEvent.PostSwitchTeam += OnPostSwitchTeam;
        }

        private static void UnregisterReload()
        {
            if (_isInSwitchTeamEvent)
                return;
            _uiHandler = null;
            MissionLibrary.Event.MissionEvent.PreSwitchTeam -= OnPreSwitchTeam;
            MissionLibrary.Event.MissionEvent.PostSwitchTeam -= OnPostSwitchTeam;
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
