using RTSCamera.Config.HotKey;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.ScreenSystem;

namespace RTSCamera.View
{
    class HideHUDView : MissionView
    {
        private bool _hideUI;
        private bool _isTemporarilyOpenUI;

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();

            MBDebug.DisableAllUI = false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if ((RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ToggleHUD).IsKeyPressed(Input)) || MBDebug.DisableAllUI && TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.Home))
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
    }
}
