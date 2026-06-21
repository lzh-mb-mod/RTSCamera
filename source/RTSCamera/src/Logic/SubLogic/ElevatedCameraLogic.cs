using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Patch.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.TwoDimension;

namespace RTSCamera.Logic.SubLogic
{
    public class ElevatedCameraLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private Vec3 _targetLocalOffset;
        private Vec3 _localOffset;
        private float _inputScale = 0f;
        private float _progress = 0;
        private float _resultProgress = 0f;
        private float _resultScale = 0;

        private bool _isOrderViewOpened = false;

        private bool _doNotElevateCameraWhenOrderIsOpen = false;

        public bool IsElevatedCameraEnabled;
        private bool _isElevatedCameraApplied;
        private bool _elevatedMessageShown = false;
        private float _elevatedCameraTurnOnCountDown = 0.1f;
        private float _cameraTurningOnSpeedFactor = 3f;
        private float _cameraTurningOnDelayFactor = 4f;

        public Mission Mission => _logic.Mission;

        public ElevatedCameraLogic(RTSCameraLogic rTSCameraLogic)
        {
            this._logic = rTSCameraLogic;
        }

        public void OnBehaviourInitialize()
        {
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
            _inputScale = Mission.IsSiegeBattle ? _config.ElevatedHeightInSiege : _config.ElevatedHeight;
        }

        public void OnRemoveBehaviour()
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

        private bool ShouldElevatedCameraOnOrder()
        {
            if (IsPlayerControllingShip())
            {
                // avoid elevate camera with "soldier pilot ship" command when player is piloting the ship.
                // The camera will slide to player because player stops piloting ship.
                // Elevating camera will break the sliding.
                _doNotElevateCameraWhenOrderIsOpen = true;
                return false;
            }
            if (!_isOrderViewOpened)
            {
                if (!Patch_MissionShip.AIPilotShipCommandJustGiven)
                {
                    _doNotElevateCameraWhenOrderIsOpen = false;
                }
                return false;
            }
            if (_isOrderViewOpened && !_doNotElevateCameraWhenOrderIsOpen && !_logic.SwitchFreeCameraLogic.IsSpectatorCamera && !CommandBattleBehavior.CommandMode && _config.CameraModeOnOrdering == CameraModeOnOrdering.Elevated && Agent.Main != null)
            {
                var orderVM = Utility.GetMissionOrderVM(_logic.Mission);
                if (orderVM == null)
                    return false;
                if (_config.ElevateCameraWithMovementOrderOnly)
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

        public void TurnOnElevatedCamera()
        {
            if (IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Turned on overlook camera");
#endif
            if (!_elevatedMessageShown)
            {
                _elevatedMessageShown = true;
                Utility.DisplayLocalizedText("str_rts_camera_elevated_camera_hint");
            }
            IsElevatedCameraEnabled = true;
            //_elevatedCameraTurnOnCountDown = _progress > 0.01f ? 0.0f : 0.2f;
            _elevatedCameraTurnOnCountDown = 0;
            _progress = MathF.Pow(_resultProgress, 1 / _cameraTurningOnDelayFactor);
        }

        public void TurnOffElevatedCamera()
        {
            if (!IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Turned off overlook camera");
#endif
            IsElevatedCameraEnabled = false;
            _progress = _resultProgress;
        }

        internal void OnPreMissionTick(float dt)
        {
            if (ShouldElevatedCameraOnOrder())
            {
                if (!IsElevatedCameraEnabled)
                    TurnOnElevatedCamera();
            }
            else
            {
                if (IsElevatedCameraEnabled)
                    TurnOffElevatedCamera();
            }

            if (IsElevatedCameraEnabled && Agent.Main != null)
            {
                if (_elevatedCameraTurnOnCountDown > 0)
                {
                    _elevatedCameraTurnOnCountDown -= dt;
                    return;
                }
                HandleInput(dt);
                _progress = LerpFPSIndependent(_progress, 1, dt * _cameraTurningOnSpeedFactor);
                //_progress = MBMath.ClampFloat(_progress + dt / 0.8f, 0, 1f);
                _resultProgress = MathF.Pow(_progress, _cameraTurningOnDelayFactor);
                UpdateOffset(dt);
                _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
                _isElevatedCameraApplied = true;
            }
            else if (_isElevatedCameraApplied)
            {

                //_targetLocalOffset = MBMath.Lerp(_targetLocalOffset, Vec3.Zero, dt * 5f, 1f / 1000f);
                //_localOffset = MBMath.Lerp(_localOffset, Vec3.Zero, dt * 3f, 1f / 1000f);
                _progress = LerpFPSIndependent(_progress, 0, dt * 6f);
                //_progress = MBMath.ClampFloat(_progress - dt / 0.4f, 0, 1f);
                //_progress = MBMath.ClampFloat(_progress - dt / 1.2f, 0, 1f);
                _resultProgress = LerpFPSIndependent(_resultProgress, _progress, dt * 6f);
                UpdateOffset(dt);
                if (_resultScale < 0.01f)
                {
                    _resultProgress = 0f;
                    _resultScale = 0f;
                    _isElevatedCameraApplied = false;
                }
                if (!IsPlayerControllingShip())
                {
                    _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                    _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
                }
            }
        }

        private void UpdateOffset(float dt)
        {
            var height = Agent.Main.AgentScale * 0.3f;
            var offset = Agent.Main.AgentScale * 0.7f;
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
            var scale = Mission.IsSiegeBattle ? _config.ElevatedHeightInSiege : _config.ElevatedHeight;
            scale = MBMath.ClampFloat(scale - scroll / 60f  * TaleWorlds.Library.MathF.Log10(Mathf.Max(scale * 10f, 10)), 0f, 50f);

            if (Mission.IsSiegeBattle)
            {
                _config.ElevatedHeightInSiege = scale;
            }
            else
            {
                _config.ElevatedHeight = scale;
            }
            _inputScale = LerpFPSIndependent(_inputScale, scale, dt * 4f);
        }

        private static float LerpFPSIndependent(float valueFrom, float valueTo, float amount)
        {
            return MBMath.Lerp(valueTo, valueFrom, MathF.Pow(2f, 0f - amount));
        }
    }
}
