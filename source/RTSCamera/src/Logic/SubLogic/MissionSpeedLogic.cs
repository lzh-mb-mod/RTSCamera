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

        public Mission Mission => _logic.Mission;

        public MissionSpeedLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void AfterStart()
        {            
            Mission.Scene.TimeSpeed = _config.SlowMotionFactor;
        }

        public void OnMissionTick(float dt)
        {
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.Pause).IsKeyPressed(Mission.InputManager))
            {
                TogglePause();
            }

            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SlowMotion).IsKeyPressed(Mission.InputManager))
            {                
                SetSlowMotionMode(Mission.Scene.TimeSpeed == 1);
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
            _config.SlowMotionMode = slowMotionMode;
            if (slowMotionMode)
            {
                if (!Mission.Current.GetRequestedTimeSpeed(69, out _))
                {
                    Mission.Current.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(_config.SlowMotionFactor, 69));
                }
            }
            else
            {
                if (Mission.Current.GetRequestedTimeSpeed(69, out _))
                {
                    Mission.Current.RemoveTimeSpeedRequest(69);
                }
            }
            Utility.DisplayLocalizedText(slowMotionMode ? "str_rts_camera_slow_motion_enabled" : "str_rts_camera_normal_mode_enabled", null);
            _config.Serialize();
        }

        public void SetSlowMotionFactor(float factor)
        {
            Mission.Scene.TimeSpeed = factor;
            _config.SlowMotionFactor = factor;
        }
    }
}
