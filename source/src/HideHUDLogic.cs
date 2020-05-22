using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    class HideHUDLogic : MissionLogic
    {
        private GameKeyConfig _gameKeyConfig;
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private bool _oldDisplayTargetingReticule = true;
        private bool _hideUI = false;


        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _switchFreeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            if (_switchFreeCameraLogic != null)
            {
                _switchFreeCameraLogic.ToggleFreeCamera += OnToggleFreeCamera;
            }
            _gameKeyConfig = GameKeyConfig.Get();
            _oldDisplayTargetingReticule = BannerlordConfig.DisplayTargetingReticule;
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            if (_switchFreeCameraLogic != null)
                _switchFreeCameraLogic.ToggleFreeCamera -= OnToggleFreeCamera;
            RecoverTargetingReticule();

            MBDebug.DisableAllUI = false;
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            MBDebug.DisableAllUI = false;
            RecoverTargetingReticule();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.ToggleHUD)) || Input.IsKeyPressed(InputKey.Home))
                ToggleUI();
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
            if (_switchFreeCameraLogic != null && _switchFreeCameraLogic.isSpectatorCamera && !BannerlordConfig.DisplayTargetingReticule)
            {
                BannerlordConfig.DisplayTargetingReticule = _oldDisplayTargetingReticule;
            }
        }
    }
}
