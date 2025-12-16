using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
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
        private bool _slowMotionByRTSView = false;
        private int _fastForwardHotKeyCollDown = 0;

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
        public void OnBehaviourInitialize()
        {
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
        }

        public void OnRemoveBehaviour()
        {
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            if (_slowMotionRequestAdded)
            {
                RemoveSlowMotionRequest();
            }
            if (_slowMotionByRTSView)
            {
                SetSlowMotionMode(false);
                _slowMotionByRTSView = false;
            }
        }

        public  void OnToggleFreeCamera(bool freeCamera)
        {
            if (_config.SlowMotionOnRtsView && !CommandBattleBehavior.CommandMode)
            {
                SetSlowMotionMode(freeCamera);
                _slowMotionByRTSView = freeCamera;
            }
        }

        public void OnMissionTick(float dt)
        {
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.Pause).IsKeyPressedInOrder())
            {
                if (Mission.IsFastForward)
                {
                    Mission.SetFastForwardingFromUI(false);
                }
                TogglePause();
            }

            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SlowMotion).IsKeyPressedInOrder())
            {
                if (Mission.IsFastForward)
                {
                    Mission.SetFastForwardingFromUI(false);
                }
                SetSlowMotionMode(!_config.SlowMotionMode);
            }

            if (_fastForwardHotKeyCollDown > 0)
            {
                // hotkey may be triggered multiple times in fast forward mode so we need to cool it down.
                _fastForwardHotKeyCollDown--;
            }
            else
            {
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.Fastforward).IsKeyPressedInOrder())
                {
                    _fastForwardHotKeyCollDown = 10;
                    Mission.Current.SetFastForwardingFromUI(!Mission.Current.IsFastForward);
                    _config.SlowMotionMode = false;
                }
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
                // Implemented through patch
                //Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(_config.SlowMotionFactor, RTSCameraSubModule.MissionTimeSpeedRequestId));
                Utility.DisplayLocalizedText("str_rts_camera_slow_motion_enabled");
                _slowMotionRequestAdded = true;
            }
        }

        private void RemoveSlowMotionRequest()
        {
            // Implemented through patch
            //Mission.RemoveTimeSpeedRequest(RTSCameraSubModule.MissionTimeSpeedRequestId);
            Utility.DisplayLocalizedText("str_rts_camera_normal_mode_enabled");
            _slowMotionRequestAdded = false;
        }

        private void UpdateSlowMotionRequest()
        {
            // Implemented through patch
            //Mission.RemoveTimeSpeedRequest(RTSCameraSubModule.MissionTimeSpeedRequestId);
            //Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(_config.SlowMotionFactor, RTSCameraSubModule.MissionTimeSpeedRequestId));
            _slowMotionRequestAdded = true;
        }
    }
}
