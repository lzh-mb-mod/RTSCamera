using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    class HideHUDLogic : MissionLogic
    {
        private GameKeyConfig _gameKeyConfig;
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private bool _originallyDisplayTargetingReticule = true;
        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            _switchFreeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            if (_switchFreeCameraLogic != null)
            {
                _switchFreeCameraLogic.ToggleFreeCamera += OnToggleFreeCamera;
            }
            _gameKeyConfig = GameKeyConfig.Get();
            _originallyDisplayTargetingReticule = BannerlordConfig.DisplayTargetingReticule;
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            if (_switchFreeCameraLogic != null)
                _switchFreeCameraLogic.ToggleFreeCamera -= OnToggleFreeCamera;
            BannerlordConfig.DisplayTargetingReticule = _originallyDisplayTargetingReticule;
            MBDebug.DisableAllUI = false;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.ToggleHUD)) || Input.IsKeyPressed(InputKey.Home))
                ToggleUI();
        }

        public void ToggleUI()
        {
            CommandLineFunctionality.CallFunction("ui.toggle_ui", "");
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            if (freeCamera)
            {
                _originallyDisplayTargetingReticule = BannerlordConfig.DisplayTargetingReticule;
                BannerlordConfig.DisplayTargetingReticule = false;
            }
            else
            {
                BannerlordConfig.DisplayTargetingReticule = _originallyDisplayTargetingReticule;
            }
        }
    }
}
