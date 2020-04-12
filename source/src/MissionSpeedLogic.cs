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
        private bool _paused = false;

        public override void AfterStart()
        {
            base.AfterStart();

            _config = EnhancedMissionConfig.Get();
            if (Math.Abs(_config.SlowMotionFactor - 1.0) >= 0.01)
                ApplySlowMotionFactor(_config.SlowMotionFactor);
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
            _paused = !_paused;
            Utility.DisplayLocalizedText(_paused ? "str_mission_paused" : "str_mission_continued");
            if (_paused)
            {
                Mission.Scene.SlowMotionFactor = 0.0001f;
                Mission.Scene.SlowMotionMode = true;
            }
            else
            {
                ApplySlowMotionFactor();
            }
        }


        public void ResetSpeed()
        {
            _config.SlowMotionFactor = 1.0f;
            SetNormalMode();
        }

        public void ApplySlowMotionFactor(float factor)
        {
            _config.SlowMotionFactor = factor;
            ApplySlowMotionFactor();
        }

        public void ApplySlowMotionFactor()
        {
            if (Math.Abs(_config.SlowMotionFactor - 1.0f) < 0.01f)
                SetNormalMode();
            else
            {
                SetFastForwardModeImpl(false);
                SetSlowMotionModeImpl(_config.SlowMotionFactor);
                SetFastForwardModeImpl(false);
                Utility.DisplayLocalizedText("str_slow_motion_enabled");
            }
        }

        public void SetFastForwardMode()
        {
            SetSlowMotionModeImpl(1.0f);
            SetFastForwardModeImpl(false);
            Utility.DisplayLocalizedText("str_fast_forward_mode_enabled");
        }

        public void SetNormalMode()
        {
            SetSlowMotionModeImpl(1.0f);
            SetFastForwardModeImpl(false);
            Utility.DisplayLocalizedText("str_normal_mode_enabled");
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
        private void SetSlowMotionModeImpl(float factor)
        {
            if (Math.Abs(factor - 1.0) < 0.01)
            {
                Mission.Scene.SlowMotionFactor = 0.2f;
                Mission.Scene.SlowMotionMode = false;
            }
            else
            {
                Mission.Scene.SlowMotionFactor = factor;
                Mission.Scene.SlowMotionMode = true;
            }
        }

        private void SetFastForwardModeImpl(bool enabled)
        {
            Mission.SetFastForwardingFromUI(enabled);
        }
    }
}
