using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Logic;
using RTSCamera.Patch.Naval;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.TwoDimension;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.View
{
    public class ElevatedCameraSubView 
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private const float MinTransitionSeconds = 0.01f;
        private const float RiseStartThreshold = 0.19f;
        private const float RiseEndThreshold = 0.81f;
        private float _currentAgentScale = 0f;
        private Vec3 _targetLocalOffset;
        private Vec3 _localOffset;
        private float _targetScale = 0f;
        private float _inputScale = 0f;
        private float _resultProgress = 0f;
        private float _resultScale = 0;
        private float _riseParameterA;
        private float _riseParameterB;
        private float _fallParameterC;
        private float _riseElapsed;
        private float _fallElapsed;
        private float _fallStartProgress = 1f;
        private float? _resultProgressBeforeKeepingCamera = null;

        private bool _isOrderViewOpened = false;

        private bool _doNotTriggerElevatedCameraWhenOrderIsOpen = false;

        // user can scroll to trigger elevated camera.
        public bool IsElevatedCameraEnabled { get; private set; }


        private bool _isElevatedCameraTriggered;
        private bool _manuallyAdjusted;
        public bool IsKeepingElevatedCamera { get; private set; }
        public bool IsElevatedCameraApplied { get; private set; }

        public bool IsElevatedCameraNoticable => IsElevatedCameraEnabled && _resultScale > 5f;

        private bool _elevatedMessageShown = false;

        private float ScaleInConfig
        {
            get => Mission.IsSiegeBattle ? _config.ElevatedHeightInSiege : _config.ElevatedHeight;
            set
            {
#if DEBUG
                Utility.DisplayMessage($"scale set to {value}");
#endif
                if (Mission.IsSiegeBattle)
                {
                    _config.ElevatedHeightInSiege = value;
                }
                else
                {
                    _config.ElevatedHeight = value;
                }
            }
        }

        public Mission Mission => _logic.Mission;

        public ElevatedCameraSubView(RTSCameraLogic rTSCameraLogic)
        {
            this._logic = rTSCameraLogic;
        }

        public void OnMissionScreenInitialize()
        {
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
            RefreshTimingParameters();
        }

        public void OnMissionScreenFinalize()
        {
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }
        private void OnToggledOrderView(MissionPlayerToggledOrderViewEvent e)
        {
            if (e.IsOrderEnabled)
            {
#if DEBUG
                Utility.DisplayMessage("Toggled on order view");
#endif
                _isOrderViewOpened = true;
            }
            else
            {
#if DEBUG
                Utility.DisplayMessage("Toggled off order view");
#endif
                _isOrderViewOpened = false;
            }
        }

        private bool ShouldEnableElevatedCamera()
        {
            return AllowElevatedCameraInNaval() && _isOrderViewOpened && !_logic.SwitchFreeCameraLogic.IsSpectatorCamera && !CommandBattleBehavior.CommandMode && Agent.Main != null;
        }

        private bool AllowElevatedCameraInNaval()
        {
            if (IsPlayerControllingShip())
            {
                // avoid elevate camera with "soldier pilot ship" command when player is piloting the ship.
                // The camera will slide to player because player stops piloting ship.
                // Elevating camera will break the sliding.
                _doNotTriggerElevatedCameraWhenOrderIsOpen = true;
                return false;
            }
            if (!_isOrderViewOpened)
            {
                if (!Patch_MissionShip.AIPilotShipCommandJustGiven)
                {
                    _doNotTriggerElevatedCameraWhenOrderIsOpen = false;
                }
                return false;
            }
            return true;
        }

        private bool ShouldTriggerElevatedCamera()
        {
            if (_isOrderViewOpened && !_doNotTriggerElevatedCameraWhenOrderIsOpen && !_logic.SwitchFreeCameraLogic.IsSpectatorCamera && !CommandBattleBehavior.CommandMode && _config.ElevatedCameraTriggerMode >= ElevatedCameraTriggerMode.WhenOpeningOrderUI && Agent.Main != null)
            {
                var orderVM = Utility.GetMissionOrderVM(_logic.Mission);
                if (orderVM == null)
                    return false;
                if (_config.ElevatedCameraTriggerMode == ElevatedCameraTriggerMode.WhenGivingMovementOrder)
                {
                    return orderVM.SelectedOrderSet != null && !orderVM.SelectedOrderSet.HasSingleOrder && orderVM.SelectedOrderSet.OrderSet.StringId == "order_type_movement";
                }
                if (_config.KeepOrderUIOpenInElevatedCamera)
                {
                    // if we keep order UI open in elevated camera, we may want continuous ordering.
                    return true;
                }
                if (orderVM.SelectedOrderSet == null)
                    return true;
                if (orderVM.SelectedOrderSet.HasSingleOrder)
                    return false;
                var stringId = orderVM.SelectedOrderSet.OrderSet.StringId;
                if (stringId == "order_type_form" ||
                    stringId == "order_type_toggle" ||
                    // for volley in RTS Command
                    stringId == "order_type_volley")
                    return false;
                return true;
            }
            return false;
        }

        private bool IsPlayerControllingShip()
        {
            return Mission.IsNavalBattle && Utilities.Utility.GetPlayerControlledShip(Mission) != null;
        }

        public void EnableElevatedCamera(bool trigger)
        {
            bool shouldTrigger = trigger && (!IsKeepingElevatedCamera || !_config.KeepOrderUIOpenInElevatedCamera);
            if (IsElevatedCameraEnabled && (!shouldTrigger || _isElevatedCameraTriggered))
                return;
#if DEBUG
            Utility.DisplayMessage("Enable elevated camera");
#endif
            if (!_elevatedMessageShown && trigger)
            {
                _elevatedMessageShown = true;
                Utility.DisplayLocalizedText("str_rts_camera_elevated_camera_hint");
            }
            IsElevatedCameraEnabled = true;

            if (shouldTrigger)
            {
                if (_manuallyAdjusted)
                {
                    _isElevatedCameraTriggered = true;
                    ScaleInConfig = _targetScale;
                }
                else
                {
#if DEBUG
                    Utility.DisplayMessage("Trigger elevated camera");
#endif
                    IsKeepingElevatedCamera = true;
                    _isElevatedCameraTriggered = true;
                    _fallElapsed = 0f;

                    if (_resultProgressBeforeKeepingCamera.HasValue)
                    {
                        _resultProgress = _resultProgressBeforeKeepingCamera.Value;
                        _resultProgressBeforeKeepingCamera = null;
                    }
                    _riseElapsed = GetRiseElapsed(_resultProgress);
                    _inputScale = _targetScale = ScaleInConfig;
                }
            }
            else if (IsElevatedCameraApplied && IsElevatedCameraNoticable)
            {
                IsKeepingElevatedCamera = true;
                _fallElapsed = 0f;
                _isElevatedCameraTriggered = false;
                _resultProgressBeforeKeepingCamera = _resultProgress;
                _riseElapsed = float.MaxValue;
                _inputScale = _targetScale = _resultScale;
            }
        }

        public void DisableElevatedCamera()
        {
            if (!IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Disable elevated camera");
#endif
            IsElevatedCameraEnabled = false;
            IsKeepingElevatedCamera = false;
            _isElevatedCameraTriggered = false;
            _fallStartProgress = _resultProgress;
            _fallElapsed = 0f;
            _resultProgressBeforeKeepingCamera = null;

            _manuallyAdjusted = false;
        }

        public void OnMissionScreenTick(float dt)
        {
            if (ShouldEnableElevatedCamera())
            {
                EnableElevatedCamera(ShouldTriggerElevatedCamera());
            }
            else
            {
                if (IsElevatedCameraEnabled)
                    DisableElevatedCamera();
            }

            if (MBCommon.IsPaused || MissionState.Current.Paused)
                return;
            if (IsElevatedCameraEnabled && Agent.Main != null)
            {
                HandleInput(dt);
                if (IsKeepingElevatedCamera)
                {
                    _riseElapsed += dt;
                    _resultProgress = EvaluateRiseProgress(_riseElapsed);
                    UpdateOffset();
                    _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                    _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
                    IsElevatedCameraApplied = true;
                }
            }
            // Camera is falling to player camera.
            if (IsElevatedCameraApplied && !IsKeepingElevatedCamera)
            {
                _fallElapsed += dt;
                _resultProgress = _fallStartProgress * EvaluateFallProgress(_fallElapsed);
                UpdateOffset();
                if (_resultScale < 0.01f)
                {
                    _resultProgress = 0f;
                    _resultScale = 0f;
                    _riseElapsed = 0f;
                    _fallElapsed = 0f;
                    _fallStartProgress = 0f;
                    IsElevatedCameraApplied = false;

                    _inputScale = 0f;
                    _targetScale = 0f;
                }
                if (!IsPlayerControllingShip())
                {
                    _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                    _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
                }
            }
        }

        public void RefreshTimingParameters()
        {
            var delay = Math.Max(_config.ElevatedCameraDelay, MinTransitionSeconds);
            var duration = Math.Max(_config.ElevatedCameraDuration, MinTransitionSeconds);
            var offDuration = Math.Max(_config.ElevatedCameraOffDuration, MinTransitionSeconds);
            Utilities.Utility.CameraEaseSolver.SolveRiseParams(delay, duration, RiseStartThreshold, RiseEndThreshold, out double a, out double b);
            _riseParameterA = (float)a;
            _riseParameterB = (float)b;
            _fallParameterC = (float)RTSCamera.Utilities.Utility.CameraEaseSolver.SolveFallParam(offDuration, RiseStartThreshold);
        }

        private float GetRiseElapsed(float progress)
        {
            if (progress <= 0f)
            {
                return 0f;
            }

            var clampedProgress = Math.Min(progress, 0.9999f);
            var poweredProgress = Math.Pow(clampedProgress, 1d / _riseParameterB);
            return (float)(-Math.Log(1d - poweredProgress, 2d) / _riseParameterA);
        }

        private float EvaluateRiseProgress(float elapsed)
        {
            var riseBase = 1d - Math.Pow(2d, -_riseParameterA * elapsed);
            return MBMath.ClampFloat((float)Math.Pow(Math.Max(riseBase, 0d), _riseParameterB), 0f, 1f);
        }

        private float EvaluateFallProgress(float elapsed)
        {
            var u = _fallParameterC * elapsed;
            return MBMath.ClampFloat((float)((1d + u * Math.Log(2d)) * Math.Pow(2d, -u)), 0f, 1f);
        }

        private void UpdateOffset()
        {
            if (Agent.Main != null)
            {
                _currentAgentScale = Agent.Main.AgentScale;
            }
            var height = _currentAgentScale * 0.3f;
            var offset = _currentAgentScale * 0.7f;
            var smoothProgress = Smooth(_resultProgress);
            _resultScale = _inputScale * smoothProgress;
            _targetLocalOffset = Vec3.Up * height * _resultScale;
            //_targetLocalOffset = Vec3.Up * height * smoothProgress;
            _localOffset = new Vec3(0, -offset, offset * -0.03f) * _resultScale;
            //_localOffset = new Vec3(0, -offset, offset * 0.4f) * smoothProgress;
        }

        private static float Smooth(float x)
        {
            //return x * x * (3f - 2f * x);
            return x * x * x * (x * (6 * x - 15f) + 10f);
        }

        private void HandleInput(float dt)
        {
            var missionScreen = Utility.GetMissionScreen();
            var scroll = missionScreen.SceneLayer.Input.GetDeltaMouseScroll();
            if (scroll != 0)
            {
                if (!IsKeepingElevatedCamera)
                {
                    IsKeepingElevatedCamera = true;
                    _fallElapsed = 0f;
                    _riseElapsed = float.MaxValue;
                    _inputScale = _targetScale = _resultScale;
                }
                _manuallyAdjusted = true;
            }
            _targetScale = MBMath.ClampFloat(_targetScale - scroll / 60f * TaleWorlds.Library.MathF.Log10(Mathf.Max(_targetScale * 10f, 10)), 0f, 50f);

            if (_isElevatedCameraTriggered && scroll != 0)
            {
                ScaleInConfig = _targetScale;
            }
            _inputScale = LerpFPSIndependent(_inputScale, _targetScale, dt * 4f);
        }

        private static float LerpFPSIndependent(float valueFrom, float valueTo, float amount)
        {
            return MBMath.Lerp(valueTo, valueFrom, MathF.Pow(2f, 0f - amount));
        }
    }
}
