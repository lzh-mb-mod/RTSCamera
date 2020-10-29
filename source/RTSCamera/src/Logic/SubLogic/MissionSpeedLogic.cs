using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using TaleWorlds.InputSystem;
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

            Mission.Scene.SlowMotionFactor = _config.SlowMotionFactor;
            Mission.Scene.SlowMotionMode = _config.SlowMotionMode;
        }

        public void OnMissionTick(float dt)
        {
            if (Input.IsKeyPressed(RTSCameraGameKeyCategory.GetKey(GameKeyEnum.Pause)))
            {
                TogglePause();
            }

            if (Input.IsKeyPressed(RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SlowMotion)))
            {
                SetSlowMotionMode(!Mission.Scene.SlowMotionMode);
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
            Mission.Scene.SlowMotionMode = slowMotionMode;
            _config.SlowMotionMode = slowMotionMode;
            Utility.DisplayLocalizedText(slowMotionMode ? "str_rts_camera_slow_motion_enabled" : "str_rts_camera_normal_mode_enabled");
            _config.Serialize();
        }

        public void SetSlowMotionFactor(float factor)
        {
            Mission.Scene.SlowMotionFactor = factor;
            _config.SlowMotionFactor = factor;
        }

        //public void ApplySlowMotionFactor()
        //{
        //    if (Math.Abs(_config.SlowMotionFactor - 1.0f) < 0.01f)
        //        SetNormalMode();
        //    else
        //    {
        //        SetFastForwardModeImpl(false);
        //        SetSlowMotionModeImpl(_config.SlowMotionFactor);
        //        SetFastForwardModeImpl(false);
        //        Utility.DisplayLocalizedText("str_rts_camera_slow_motion_enabled");
        //    }
        //}

        //public void SetFastForwardMode()
        //{
        //    Mission.SetFastForwardingFromUI(true);
        //    Utility.DisplayLocalizedText("str_rts_camera_fast_forward_mode_enabled");
        //}
    }
}
