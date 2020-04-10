using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace EnhancedMission
{
    public enum MissionSpeed
    {
        Slow,
        Normal,
        Fast
    }

    class MissionSpeedLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public override void AfterStart()
        {
            base.AfterStart();

            _config = EnhancedMissionConfig.Get();
            if (Math.Abs(_config.SlowMotionFactor - 1.0) >= 0.01)
                SetSlowMotionFactor(_config.SlowMotionFactor);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.Pause)))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            MissionState.Current.Paused = !MissionState.Current.Paused;
            Utility.DisplayLocalizedText(MissionState.Current.Paused ? "str_mission_paused" : "str_mission_continued");
        }


        public void ResetSpeed()
        {
            _config.SlowMotionFactor = 1.0f;
            Mission.Scene.SlowMotionFactor = 0.2f;
            SetSpeedMode(MissionSpeed.Normal);
        }

        public void SetSpeedMode(MissionSpeed speed)
        {
            switch (speed)
            {
                case MissionSpeed.Slow:
                    SetSlowMotionMode();
                    break;
                case MissionSpeed.Normal:
                    SetNormalMode();
                    break;
                case MissionSpeed.Fast:
                    SetFastForwardMode();
                    break;
            }
        }

        public void SetSlowMotionFactor(float factor)
        {
            if (!Mission.Scene.SlowMotionMode)
                SetSlowMotionMode();
            this.Mission.Scene.SlowMotionFactor = factor;
            _config.SlowMotionFactor = factor;
        }

        public MissionSpeed CurrentSpeed
        {
            get
            {
                if (Mission.IsFastForward)
                    return MissionSpeed.Fast;
                if (Mission.Scene.SlowMotionMode)
                    return MissionSpeed.Slow;
                return MissionSpeed.Normal;
            }
        }

        public void SetSlowMotionMode()
        {
            SetFastForwardModeImpl(false);
            SetSlowMotionModeImpl(true);
            Utility.DisplayLocalizedText("str_slow_motion_enabled");
        }

        public void SetFastForwardMode()
        {
            SetSlowMotionModeImpl(false);
            SetFastForwardModeImpl(true);
            Utility.DisplayLocalizedText("str_fast_forward_mode_enabled");
        }

        public void SetNormalMode()
        {
            SetSlowMotionModeImpl(false);
            SetFastForwardModeImpl(false);
            Utility.DisplayLocalizedText("str_normal_mode_enabled");
        }

        private void SetSlowMotionModeImpl(bool enabled)
        {
            Mission.Scene.SlowMotionMode = enabled;
        }

        private void SetFastForwardModeImpl(bool enabled)
        {
            Mission.SetFastForwardingFromUI(enabled);
        }
    }
}
