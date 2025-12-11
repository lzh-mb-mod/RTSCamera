using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;

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
        private static readonly float _beginDraggingOffsetThreshold = 20;
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
                    typeof(GauntletOrderUIHandler).GetMethod(
                        nameof(GauntletOrderUIHandler.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionGauntletSingleplayerOrderUIHandler).GetMethod(
                        nameof(Postfix_OnMissionScreenTick), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
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


        private static bool ShouldBeginEarlyDragging(GauntletOrderUIHandler __instance)
        {
            return !_earlyDraggingMode &&
                   (__instance.MissionScreen.InputManager.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null) && IsDragKeyPressed(__instance);
        }

        private static bool IsDragKeyPressed(GauntletOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.ControllerLTrigger);
        }

        private static bool IsDragKeyDown(GauntletOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger);
        }

        private static bool IsDragKeyReleased(GauntletOrderUIHandler __instance)
        {
            return __instance.MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton) || __instance.MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.ControllerLTrigger);
        }

        private static void BeginEarlyDragging(OrderTroopPlacer ____orderTroopPlacer)
        {
            if ((bool?)typeof(OrderTroopPlacer).GetField("_isMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(____orderTroopPlacer) ?? false)
            {
                Patch_MissionOrderVM.AllowEscape = false;
            }
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

        private static void EndDrag(OrderTroopPlacer ____orderTroopPlacer)
        {
            if (_earlyDraggingMode && ((bool?)typeof(OrderTroopPlacer).GetField("_isMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(____orderTroopPlacer) ?? false))
            {
                typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(____orderTroopPlacer, new object[] { });
            }
            EndEarlyDragging();
            _rightButtonDraggingMode = false;
            Patch_MissionOrderVM.AllowEscape = true;
        }

        private static void UpdateMouseVisibility(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, ref bool ____isTransferEnabled)
        {
            if (__instance == null)
                return;
            bool mouseVisibility =
                (__instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ||
                 ____dataSource.IsToggleOrderShown && (__instance.Input.IsAltDown() || __instance.MissionScreen.LastFollowedAgent == null)) &&
                !_rightButtonDraggingMode && !_earlyDraggingMode;
            var inputUsageMask = __instance.IsDeployment || ____dataSource.TroopController.IsTransferActive ? InputUsageMask.All : RTSCameraSubModule.IsCommandSystemInstalled && UIConfig.DoNotUseGeneratedPrefabs ? InputUsageMask.All : InputUsageMask.Invalid;
            var layer = ____gauntletLayer;
            if (mouseVisibility != layer.InputRestrictions.MouseVisibility || inputUsageMask != layer.InputRestrictions.InputUsageMask)
            {
                layer.InputRestrictions.SetInputRestrictions(mouseVisibility,
                    inputUsageMask);
            }
            if (____dataSource.TroopController.IsTransferActive != ____isTransferEnabled)
            {
                ____isTransferEnabled = ____dataSource.TroopController.IsTransferActive;
                if (!____isTransferEnabled)
                {
                    ____gauntletLayer.UIContext.ContextAlpha = BannerlordConfig.HideBattleUI ? 0.0f : 1f;
                    ____gauntletLayer.IsFocusLayer = false;
                    ScreenManager.TryLoseFocus(____gauntletLayer);
                }
                else
                {
                    ____gauntletLayer.UIContext.ContextAlpha = 1f;
                    ____gauntletLayer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(____gauntletLayer);
                }
            }

            //if (__instance.MissionScreen.OrderFlag != null)
            //{
            //    bool orderFlagVisibility = (____dataSource.IsToggleOrderShown || IsAnyDeployment(__instance)) &&
            //                               !____dataSource.TroopController.IsTransferActive &&
            //                               !_rightButtonDraggingMode && !_earlyDraggingMode;
            //    if (orderFlagVisibility != __instance.MissionScreen.OrderFlag.IsVisible)
            //    {
            //        __instance.MissionScreen.SetOrderFlagVisibility(orderFlagVisibility);
            //    }
            //}
        }
        private static void UpdateOrderUIVisibility(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer)
        {
            // TODO: don't close the order ui and open it again.
            // Keep orders UI open after issuing an order in free camera mode.
            
            //if (____dataSource.IsToggleOrderShown)
            //{
            //    Patch_MissionOrderVM.EscapeRequested = false;
            //}
            //if (Patch_MissionOrderVM.EscapeRequested)
            //{
            //    return;
            //}
            //if (!____dataSource.IsToggleOrderShown && !____dataSource.TroopController.IsTransferActive && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            //{
            //    //____dataSource.OpenToggleOrder(false);
            //    var orderTroopPlacer = Mission.Current.GetMissionBehavior<OrderTroopPlacer>();
            //    if (orderTroopPlacer != null)
            //    {
            //        typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic)
            //            .Invoke(orderTroopPlacer, null);
            //    }
            //}
        }



        private static void UpdateDragData(GauntletOrderUIHandler __instance, MissionOrderVM ____dataSource, OrderTroopPlacer ____orderTroopPlacer)
        {
            if (_willEndDraggingMode)
            {
                _willEndDraggingMode = false;
                EndDrag(____orderTroopPlacer);
            }
            else if (!____dataSource.IsToggleOrderShown && !__instance.IsDeployment || IsDragKeyReleased(__instance))
            {
                if (_earlyDraggingMode || _rightButtonDraggingMode)
                    _willEndDraggingMode = true;
            }
            else if (____dataSource.IsToggleOrderShown || __instance.IsDeployment)
            {
                if (ShouldBeginEarlyDragging(__instance))
                {
                    BeginEarlyDragging(____orderTroopPlacer);
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

        public static void Postfix_OnMissionScreenTick(GauntletOrderUIHandler __instance, ref float ____latestDt, ref bool ____isReceivingInput, float dt, MissionOrderVM ____dataSource, GauntletLayer ____gauntletLayer, OrderTroopPlacer ____orderTroopPlacer, ref bool ____isTransferEnabled)
        {
            UpdateDragData(__instance, ____dataSource, ____orderTroopPlacer);
            UpdateMouseVisibility(__instance, ____dataSource, ____gauntletLayer, ref ____isTransferEnabled);
            UpdateOrderUIVisibility(__instance, ____dataSource, ____gauntletLayer);
            Patch_MissionOrderVM.TickAllowClosingOrderUI();
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
            var missionOrderVM = _dataSource?.GetValue(_uiHandler) as MissionOrderVM;
            if (missionOrderVM != null)
                Patch_MissionOrderVM.TryCloseToggleOrder(missionOrderVM);
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
