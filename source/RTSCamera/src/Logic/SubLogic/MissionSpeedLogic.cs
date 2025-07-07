using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class MissionSpeedLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private bool _slowMotionRequestAdded = false;

        public Mission Mission => _logic.Mission;

        public MissionSpeedLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void AfterStart()
        {
            if (_config.SlowMotionMode && !_slowMotionRequestAdded)
            {
                AddSlowMotionRequest();
            }
        }

        public void OnMissionTick(float dt)
        {
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.Pause).IsKeyPressed(Mission.InputManager))
            {
                TogglePause();
            }

            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SlowMotion).IsKeyPressed(Mission.InputManager))
            {
                SetSlowMotionMode(!_config.SlowMotionMode);
            }
        }

        public void TogglePause()
        {
            var paused = !MissionState.Current.Paused;
            MissionState.Current.Paused = paused;
            Utility.DisplayLocalizedText(paused ? "str_rts_camera_mission_paused" : "str_rts_camera_mission_continued");
        }

        public void SetSlowMotionMode(bool slowMotionMode)
        {
            if (_config.SlowMotionMode == slowMotionMode)
                return;

            _config.SlowMotionMode = slowMotionMode;
            if (slowMotionMode)
            {
                AddSlowMotionRequest();
            }
            else
            {
                RemoveSlowMotionRequest();
            }
            _config.Serialize();
        }

        public void SetSlowMotionFactor(float factor)
        {
            _config.SlowMotionFactor = factor;

            if (_config.SlowMotionMode)
            {
                UpdateSlowMotionRequest();
            }
            _config.Serialize();
        }

        private void AddSlowMotionRequest()
        {
            if (!_slowMotionRequestAdded)
            {
                Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(_config.SlowMotionFactor, RTSCameraSubModule.MissionTimeSpeedRequestId));
                Utility.DisplayLocalizedText("str_rts_camera_slow_motion_enabled");
                _slowMotionRequestAdded = true;
            }
        }

        private void RemoveSlowMotionRequest()
        {
            Mission.RemoveTimeSpeedRequest(RTSCameraSubModule.MissionTimeSpeedRequestId);
            Utility.DisplayLocalizedText("str_rts_camera_normal_mode_enabled");
            _slowMotionRequestAdded = false;
        }

        private void UpdateSlowMotionRequest()
        {
            Mission.RemoveTimeSpeedRequest(RTSCameraSubModule.MissionTimeSpeedRequestId);
            Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(_config.SlowMotionFactor, RTSCameraSubModule.MissionTimeSpeedRequestId));
            _slowMotionRequestAdded = true;
        }
    }
}
