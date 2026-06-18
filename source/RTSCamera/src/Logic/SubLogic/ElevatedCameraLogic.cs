using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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
        private float _progress = 0;
        private float _resultScale = 0;

        public bool IsElevatedCameraEnabled;
        private bool _isElevatedCameraApplied;
        private bool _elevatedMessageShown = false;
        private float _elevatedCameraTurnOnCountDown = 0.1f;

        public Mission Mission => _logic.Mission;

        public ElevatedCameraLogic(RTSCameraLogic rTSCameraLogic)
        {
            this._logic = rTSCameraLogic;
        }

        public void OnBehaviourInitialize()
        {
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
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
                if (ShouldElevatedCameraOnOrder())
                {
                    TurnOnElevatedCamera();
                }
            }
            else
            {
#if DEBUG
                Utility.DisplayMessage("Toggled off order view");
#endif
                TurnOffElevatedCamera();
            }
        }

        private bool ShouldElevatedCameraOnOrder()
        {
            if (_config.CameraModeOnOrdering == CameraModeOnOrdering.Elevated)
            {
                if (IsPlayerControllingShip())
                {
                    return false;
                }
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
            _elevatedCameraTurnOnCountDown = _progress > 0.01f ? 0.0f : 0.2f;
        }

        public void TurnOffElevatedCamera()
        {
            if (!IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Turned off overlook camera");
#endif
            IsElevatedCameraEnabled = false;
        }

        internal void OnPreMissionTick(float dt)
        {
            if (IsElevatedCameraEnabled && Agent.Main != null)
            {
                if (_elevatedCameraTurnOnCountDown > 0)
                {
                    _elevatedCameraTurnOnCountDown -= dt;
                    return;
                }
                HandleInput(dt);
                _progress = LerpFPSIndependent(_progress, 1, dt * 5f);
                UpdateOffset(dt);
                _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
                _isElevatedCameraApplied = true;
            }
            else if (_isElevatedCameraApplied)
            {

                //_targetLocalOffset = MBMath.Lerp(_targetLocalOffset, Vec3.Zero, dt * 5f, 1f / 1000f);
                //_localOffset = MBMath.Lerp(_localOffset, Vec3.Zero, dt * 3f, 1f / 1000f);
                _progress = LerpFPSIndependent(_progress, 0, dt * 5f);
                UpdateOffset(dt);
                if (_progress < 0.001f)
                {
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
            var scale = Mission.IsSiegeBattle ? _config.ElevatedHeightInSiege : _config.ElevatedHeight;
            var height = Agent.Main.AgentScale * 0.2f;
            var offset = Agent.Main.AgentScale * 0.7f;
            var smoothProgress = Smooth(_progress);
            var smoothScale = scale * smoothProgress;
            _resultScale = LerpFPSIndependent(_resultScale, smoothScale, dt * 8f);
            _targetLocalOffset = Vec3.Up * height * _resultScale;
            //_targetLocalOffset = Vec3.Up * height * smoothProgress;
            _localOffset = new Vec3(0, -offset, offset * 0.4f) * _resultScale;
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
            scale = MBMath.ClampFloat(scale - scroll / 100f  * TaleWorlds.Library.MathF.Log10(Mathf.Max(scale * 10f, 10)), 0f, 50f);
            if (Mission.IsSiegeBattle)
            {
                _config.ElevatedHeightInSiege = scale;
            }
            else
            {
                _config.ElevatedHeight = scale;
            }
        }

        private static float LerpFPSIndependent(float valueFrom, float valueTo, float amount)
        {
            return MBMath.Lerp(valueTo, valueFrom, MathF.Pow(2f, 0f - amount));
        }
    }
}
