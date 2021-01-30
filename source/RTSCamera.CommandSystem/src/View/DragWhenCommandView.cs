
using RTSCamera.CommandSystem.Utilities;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.CommandSystem.View
{
    public class DragWhenCommandView : MissionView
    {
        private CommandSystemOrderUIHandler _orderUIHandler;
        private bool _willEndDraggingMode;
        private bool _earlyDraggingMode;
        private float _beginDraggingOffset;
        private readonly float _beginDraggingOffsetThreshold = 100;
        private bool _rightButtonDraggingMode;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            Utility.PrintOrderHint();
            _orderUIHandler = Mission.GetMissionBehaviour<CommandSystemOrderUIHandler>();
        }

        private bool ShouldBeginEarlyDragging()
        {
            return !_earlyDraggingMode &&
                   (MissionScreen.InputManager.IsAltDown() || MissionScreen.LastFollowedAgent == null) &&
                   MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton);
        }

        private void BeginEarlyDragging()
        {
            _earlyDraggingMode = true;
            _beginDraggingOffset = 0;
        }

        private void EndEarlyDragging()
        {
            _earlyDraggingMode = false;
            _beginDraggingOffset = 0;
        }

        private bool ShouldBeginDragging()
        {
            return _earlyDraggingMode && _beginDraggingOffset > _beginDraggingOffsetThreshold;
        }

        private void BeginDrag()
        {
            EndEarlyDragging();
            _rightButtonDraggingMode = true;
            _orderUIHandler.ExitWithRightClick = false;
        }

        private void EndDrag()
        {
            EndEarlyDragging();
            _rightButtonDraggingMode = false;
            _orderUIHandler.ExitWithRightClick = true;
        }

        private void UpdateMouseVisibility()
        {
            if (_orderUIHandler == null)
                return;

            bool mouseVisibility =
                (_orderUIHandler.IsDeployment || _orderUIHandler.DataSource.TroopController.IsTransferActive ||
                 _orderUIHandler.DataSource.IsToggleOrderShown && (Input.IsAltDown() || MissionScreen.LastFollowedAgent == null)) &&
                !_rightButtonDraggingMode && !_earlyDraggingMode;
            if (mouseVisibility != _orderUIHandler.GauntletLayer.InputRestrictions.MouseVisibility)
            {
                _orderUIHandler.GauntletLayer.InputRestrictions.SetInputRestrictions(mouseVisibility,
                    mouseVisibility ? InputUsageMask.All : InputUsageMask.Invalid);
            }

            if (MissionScreen.OrderFlag != null)
            {
                bool orderFlagVisibility = (_orderUIHandler.DataSource.IsToggleOrderShown || _orderUIHandler.IsDeployment) &&
                                           !_orderUIHandler.DataSource.TroopController.IsTransferActive &&
                                           !_rightButtonDraggingMode && !_earlyDraggingMode;
                if (orderFlagVisibility != MissionScreen.OrderFlag.IsVisible)
                {
                    MissionScreen.SetOrderFlagVisibility(orderFlagVisibility);
                }
            }
        }

        private void UpdateDragData()
        {
            if (_willEndDraggingMode)
            {
                _willEndDraggingMode = false;
                EndDrag();
            }
            else if (!_orderUIHandler.DataSource.IsToggleOrderShown && !(_orderUIHandler?.IsDeployment ?? false) || MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton))
            {
                if (_earlyDraggingMode || _rightButtonDraggingMode)
                    _willEndDraggingMode = true;
            }
            else if (_orderUIHandler.DataSource.IsToggleOrderShown || (_orderUIHandler?.IsDeployment ?? false))
            {
                if (ShouldBeginEarlyDragging())
                {
                    BeginEarlyDragging();
                }
                else if (MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
                {
                    if (ShouldBeginDragging())
                    {
                        BeginDrag();
                    }
                    else if (_earlyDraggingMode)
                    {
                        float inputXRaw = MissionScreen.SceneLayer.Input.GetMouseMoveX();
                        float inputYRaw = MissionScreen.SceneLayer.Input.GetMouseMoveY();
                        _beginDraggingOffset += inputYRaw * inputYRaw + inputXRaw * inputXRaw;
                    }
                }
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            base.OnPreMissionTick(dt);

            if (_orderUIHandler != null)
                UpdateDragData();

            UpdateMouseVisibility();
        }
    }
}
