using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.View
{
    class HideHUDView : MissionView
    {
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private bool _oldDisplayTargetingReticule = true;
        private bool _hideUI;
        private bool _isTemporarilyOpenUI;

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _switchFreeCameraLogic = Mission.GetMissionBehaviour<RTSCameraLogic>().SwitchFreeCameraLogic;
            MissionLibrary.Event.MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
            _oldDisplayTargetingReticule = BannerlordConfig.DisplayTargetingReticule;
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            MissionLibrary.Event.MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            RecoverTargetingReticule();

            MBDebug.DisableAllUI = false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (TaleWorlds.InputSystem.Input.IsKeyPressed(RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ToggleHUD)) || MBDebug.DisableAllUI && TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.Home))
                ToggleUI();

            if (!_isTemporarilyOpenUI)
            {
                if (ScreenManager.FocusedLayer != MissionScreen.SceneLayer)
                {
                    _isTemporarilyOpenUI = true;
                    BeginTemporarilyOpenUI();
                }
            }
            else
            {
                if (ScreenManager.FocusedLayer == MissionScreen.SceneLayer)
                {
                    _isTemporarilyOpenUI = false;
                    EndTemporarilyOpenUI();
                }
            }
        }

        public void ToggleUI()
        {
            MBDebug.DisableAllUI = !_hideUI && !MBDebug.DisableAllUI;
            _hideUI = MBDebug.DisableAllUI;
        }

        public void BeginTemporarilyOpenUI()
        {
            _hideUI = MBDebug.DisableAllUI;
            MBDebug.DisableAllUI = false;
        }

        public void EndTemporarilyOpenUI()
        {
            MBDebug.DisableAllUI = _hideUI;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            if (freeCamera)
            {
                _oldDisplayTargetingReticule = BannerlordConfig.DisplayTargetingReticule;
                BannerlordConfig.DisplayTargetingReticule = false;
            }
            else
            {
                BannerlordConfig.DisplayTargetingReticule = _oldDisplayTargetingReticule;
            }
        }

        private void RecoverTargetingReticule()
        {
            if (_switchFreeCameraLogic != null && _switchFreeCameraLogic.IsSpectatorCamera && !BannerlordConfig.DisplayTargetingReticule)
            {
                BannerlordConfig.DisplayTargetingReticule = _oldDisplayTargetingReticule;
            }
        }
    }
}
