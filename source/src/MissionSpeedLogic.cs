using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace EnhancedMission
{

    class MissionSpeedLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public override void AfterStart()
        {
            base.AfterStart();

            _config = EnhancedMissionConfig.Get();
            Mission.Scene.SlowMotionFactor = _config.SlowMotionFactor;
            Mission.Scene.SlowMotionMode = _config.SlowMotionMode;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.Pause)))
            {
                TogglePause();
            }

            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.SlowMotion)))
            {
                SetSlowMotionMode(!Mission.Scene.SlowMotionMode);
            }
        }

        public void TogglePause()
        {
            var paused = !MissionState.Current.Paused;
            MissionState.Current.Paused = paused;
            Utility.DisplayLocalizedText(paused ? "str_em_mission_paused" : "str_em_mission_continued");
        }

        public void SetSlowMotionMode(bool slowMotionMode)
        {
            Mission.Scene.SlowMotionMode = slowMotionMode;
            _config.SlowMotionMode = slowMotionMode;
            Utility.DisplayLocalizedText(slowMotionMode ? "str_em_slow_motion_enabled" : "str_em_normal_mode_enabled");
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
        //        Utility.DisplayLocalizedText("str_em_slow_motion_enabled");
        //    }
        //}

        public void SetFastForwardMode()
        {
            Mission.SetFastForwardingFromUI(true);
            Utility.DisplayLocalizedText("str_em_fast_forward_mode_enabled");
        }
    }
}
