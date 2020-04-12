using System;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;

namespace EnhancedMission
{
    class FlyCameraMissionView : MissionView
    {
        private SwitchFreeCameraLogic _freeCameraLogic;
        private MissionMainAgentController _missionMainAgentController;
        private EnhancedMissionOrderUIHandler _orderUIHandler;

        private int _shiftSpeedMultiplier = 3;
        private Vec3 _cameraSpeed;
        private float _cameraSpeedMultiplier;
        private bool _cameraSmoothMode;
        private bool _cameraRotateSmoothMode;
        private float _cameraHeightToAdd;
        private float _cameraHeightLimit;
        private bool _classicMode = true;
        private bool _isOrderViewOpen = false;
        private bool _resetDraggingMode;
        private bool _rightButtonDraggingMode;
        private Vec2 _clickedPositionPixel = Vec2.Zero;
        private bool _setCombatActionOnNextTick = false;
        private bool _levelToEdge = false;

        private float _cameraBearingDelta;
        private float _cameraElevationDelta;
        public float CameraBearing { get; private set; }

        public float CameraElevation { get; private set; }
        //public float CameraBearing => MissionScreen.CameraBearing;

        //public float CameraElevation => MissionScreen.CameraElevation;

        public Camera CombatCamera => MissionScreen.CombatCamera;

        public Vec3 CameraPosition
        {
            get => CombatCamera.Position;
            set => CombatCamera.Position = value;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._cameraSpeed = Vec3.Zero;
            this._cameraSpeedMultiplier = 1f;
            this._cameraHeightToAdd = 0.0f;
            this._cameraHeightLimit = 0.0f;
            this._cameraSmoothMode = true;
            this._cameraRotateSmoothMode = false;
            this.ViewOrderPriorty = 25;

            _freeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            _missionMainAgentController = Mission.GetMissionBehaviour<MissionMainAgentController>();
            _orderUIHandler = Mission.GetMissionBehaviour<EnhancedMissionOrderUIHandler>();

            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            if (_freeCameraLogic != null)
                _freeCameraLogic.ToggleFreeCamera += OnToggleFreeCamera;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            if (_freeCameraLogic != null)
                _freeCameraLogic.ToggleFreeCamera -= OnToggleFreeCamera;
            _freeCameraLogic = null;
            _missionMainAgentController = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (_setCombatActionOnNextTick)
            {
                _setCombatActionOnNextTick = false;
                Agent.Main?.SetIsCombatActionsDisabled(false);
            }
        }

        public override bool UpdateOverridenCamera(float dt)
        {
            if (_freeCameraLogic == null || !_freeCameraLogic.isSpectatorCamera)
                return base.UpdateOverridenCamera(dt);

            UpdateFlyCamera(dt);
            return true;
        }

        private void EndDrag()
        {
            _orderUIHandler.exitWithRightClick = true;
            _orderUIHandler.gauntletLayer.InputRestrictions.SetMouseVisibility(true);
            MissionScreen.SetOrderFlagVisibility(true);
        }

        private void BeginDrag()
        {
            _orderUIHandler.exitWithRightClick = false;
            _orderUIHandler.gauntletLayer.InputRestrictions.SetMouseVisibility(false);
            MissionScreen.SetOrderFlagVisibility(false);
        }

        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
        {
            _isOrderViewOpen = e.IsOrderEnabled;
            bool freeCamera = _freeCameraLogic != null && _freeCameraLogic.isSpectatorCamera;
            _orderUIHandler.gauntletLayer.InputRestrictions.SetMouseVisibility(freeCamera && _isOrderViewOpen);
            _setCombatActionOnNextTick = true;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _orderUIHandler?.gauntletLayer.InputRestrictions.SetMouseVisibility(freeCamera && _isOrderViewOpen);
            if (_missionMainAgentController != null)
            {
                _missionMainAgentController.IsDisabled = freeCamera;
            }

            if (freeCamera)
            {
                this.CameraBearing = MissionScreen.CameraBearing;
                this.CameraElevation = MissionScreen.CameraElevation;
            }
        }

        private void UpdateDragData()
        {
            if (this._resetDraggingMode)
            {
                this._rightButtonDraggingMode = false;
                this._resetDraggingMode = false;
                EndDrag();
            }
            else if (_rightButtonDraggingMode && MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton))
                this._resetDraggingMode = true;
            else if (MissionScreen.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton))
            {
                this._clickedPositionPixel = MissionScreen.SceneLayer.Input.GetMousePositionPixel();
            }
            else
            {
                if (!_isOrderViewOpen || !MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || MissionScreen.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton) || ((double)MissionScreen.SceneLayer.Input.GetMousePositionPixel().DistanceSquared(this._clickedPositionPixel) <= 10.0 || this._rightButtonDraggingMode))
                    return;
                BeginDrag();
                this._rightButtonDraggingMode = true;
            }
        }

        private void UpdateFlyCamera(float dt)
        {
            if (_orderUIHandler != null)
                UpdateDragData();
            MatrixFrame cameraFrame1 = MatrixFrame.Identity;
            cameraFrame1.rotation.RotateAboutSide(1.570796f);
            cameraFrame1.rotation.RotateAboutForward(this.CameraBearing);
            cameraFrame1.rotation.RotateAboutSide(this.CameraElevation);
            cameraFrame1.origin = CameraPosition;
            float heightAtPosition = this.Mission.Scene.GetGroundHeightAtPosition(cameraFrame1.origin, BodyFlags.CommonCollisionExcludeFlags, true);
            float heightFactorForHorizontalMove = 0.5f * MathF.Clamp((float)(1.0 + ((double)cameraFrame1.origin.z - (double)heightAtPosition - 1.0) / 1), 1, 50);
            float heightFactorForVerticalMove = MathF.Clamp((float)(1.0 + ((double)cameraFrame1.origin.z - (double)heightAtPosition - 1.0) / 3), 1, 20) / 40;
            this._cameraSpeed *= (float)(1.0 - 5.0 * (double)dt);
            this._cameraSpeed.x = MBMath.ClampFloat(this._cameraSpeed.x, -heightFactorForHorizontalMove, heightFactorForHorizontalMove);
            this._cameraSpeed.y = MBMath.ClampFloat(this._cameraSpeed.y, -heightFactorForHorizontalMove, heightFactorForHorizontalMove);
            this._cameraSpeed.z = MBMath.ClampFloat(this._cameraSpeed.z, -heightFactorForHorizontalMove, heightFactorForHorizontalMove);
            if (this.DebugInput.IsHotKeyPressed("MissionScreenHotkeyIncreaseCameraSpeed"))
                this._cameraSpeedMultiplier *= 1.5f;
            if (this.DebugInput.IsHotKeyPressed("MissionScreenHotkeyDecreaseCameraSpeed"))
                this._cameraSpeedMultiplier *= 0.6666667f;
            if (this.DebugInput.IsHotKeyPressed("ResetCameraSpeed"))
                this._cameraSpeedMultiplier = 1f;
            if (this.DebugInput.IsControlDown())
            {
                float num = MissionScreen.SceneLayer.Input.GetDeltaMouseScroll() * 0.008333334f;
                if ((double)num > 0.00999999977648258)
                    this._cameraSpeedMultiplier *= 1.25f;
                else if ((double)num < -0.00999999977648258)
                    this._cameraSpeedMultiplier *= 0.8f;
            }
            float num1 = 3f * this._cameraSpeedMultiplier;
            if (MissionScreen.SceneLayer.Input.IsGameKeyDown(23))
                num1 *= (float)this._shiftSpeedMultiplier;
            if (!this._cameraSmoothMode)
            {
                this._cameraSpeed.x = 0.0f;
                this._cameraSpeed.y = 0.0f;
                this._cameraSpeed.z = 0.0f;
            }
            if (!this.DebugInput.IsControlDown() || !this.DebugInput.IsAltDown())
            {
                Vec3 keyInput = Vec3.Zero;
                Vec3 mouseInput = Vec3.Zero;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(0))
                    ++keyInput.y;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(1))
                    --keyInput.y;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(2))
                    --keyInput.x;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(3))
                    ++keyInput.x;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(13))
                    ++keyInput.z;
                if (MissionScreen.SceneLayer.Input.IsGameKeyDown(14))
                    --keyInput.z;
                if (MissionScreen.MouseVisible && !MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
                {
                    if (Mission.Mode != MissionMode.Conversation)
                    {
                        float x = MissionScreen.SceneLayer.Input.GetMousePositionRanged().x;
                        float y = MissionScreen.SceneLayer.Input.GetMousePositionRanged().y;
                        if ( x <= 0.01 && (y < 0.01 || y > 0.25))
                            --mouseInput.x;
                        else if (x >= 0.99 && (y < 0.01 || y > 0.25))
                            ++mouseInput.x;
                        if (y <= 0.01)
                            ++mouseInput.y;
                        else if (y >= 0.99)
                            --mouseInput.y;
                    }
                }

                if (keyInput.LengthSquared > 0.0)
                    keyInput.Normalize();
                if (mouseInput.LengthSquared > 0.0)
                    mouseInput.Normalize();
                this._cameraSpeed += (keyInput + mouseInput) * num1 * heightFactorForHorizontalMove;
            }
            if (_classicMode)
            {
                Vec2 asVec2 = cameraFrame1.origin.AsVec2;
                cameraFrame1.origin += this._cameraSpeed.x * new Vec3(cameraFrame1.rotation.s.AsVec2, 0.0f, -1f).NormalizedCopy() * dt;
                ref Vec3 local = ref cameraFrame1.origin;
                Vec3 vec3_2 = local;
                double y = (double)this._cameraSpeed.y;
                Vec3 vec3_1 = new Vec3(cameraFrame1.rotation.u.AsVec2, 0.0f, -1f);
                Vec3 vec3_3 = vec3_1.NormalizedCopy();
                Vec3 vec3_4 = (float)y * vec3_3 * dt;
                local = vec3_2 - vec3_4;
                cameraFrame1.origin.z += this._cameraSpeed.z * dt;
                this._cameraHeightToAdd -= (float)(3.0 * TaleWorlds.InputSystem.Input.DeltaMouseScroll / 120.0) * num1 *
                                                    heightFactorForVerticalMove;
                if (MissionScreen.SceneLayer.Input.IsHotKeyDown("DeploymentCameraIsActive"))
                {
                    if (_levelToEdge == false)
                    {
                        _levelToEdge = true;
                        ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(true);
                    }
                    this._cameraHeightToAdd += 0.05f * TaleWorlds.InputSystem.Input.MouseMoveY;
                }
                else if (_levelToEdge == true)
                {
                    _levelToEdge = false;
                    ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(false);
                }
                if ((double)MathF.Abs(this._cameraHeightToAdd) > 1.0 / 1000.0)
                {
                    cameraFrame1.origin.z += (float)((double)this._cameraHeightToAdd * (double)dt * 10.0);
                    this._cameraHeightToAdd *= (float)(1.0 - (double)dt * 10.0);
                }
                else
                {
                    cameraFrame1.origin.z += this._cameraHeightToAdd;
                    this._cameraHeightToAdd = 0.0f;
                }
                if ((double)this._cameraHeightLimit > 0.0 && (double)cameraFrame1.origin.z > (double)this._cameraHeightLimit)
                    cameraFrame1.origin.z = this._cameraHeightLimit;
            }
            else
            {
                cameraFrame1.origin += this._cameraSpeed.x * cameraFrame1.rotation.s * dt;
                cameraFrame1.origin -= this._cameraSpeed.y * cameraFrame1.rotation.u * dt;
                cameraFrame1.origin += this._cameraSpeed.z * cameraFrame1.rotation.f * dt;
            }
            if (!MBEditor.IsEditModeOn)
            {
                if (!this.Mission.IsPositionInsideBoundaries(cameraFrame1.origin.AsVec2))
                    cameraFrame1.origin.AsVec2 = this.Mission.GetClosestBoundaryPosition(cameraFrame1.origin.AsVec2);
                float heightAtPosition1 = this.Mission.Scene.GetGroundHeightAtPosition(cameraFrame1.origin + new Vec3(0.0f, 0.0f, 100f, -1f), BodyFlags.CommonCollisionExcludeFlags, true);
                if (!MissionScreen.IsCheatGhostMode && (double)heightAtPosition1 < 9999.0)
                    cameraFrame1.origin.z = Math.Max(cameraFrame1.origin.z, heightAtPosition1 + 0.5f);
                if ((double)cameraFrame1.origin.z > (double)heightAtPosition1 + 80.0)
                    cameraFrame1.origin.z = heightAtPosition1 + 80f;
                if ((double)cameraFrame1.origin.z < -100.0)
                    cameraFrame1.origin.z = -100f;
            }
            float newDNear = !this.Mission.CameraIsFirstPerson ? 0.1f : 0.065f;
            this.CombatCamera.Frame = cameraFrame1;
            MissionScreen.SceneView.SetCamera(this.CombatCamera);
            this.Mission.SetCameraFrame(cameraFrame1, 65f / MissionScreen.CameraViewAngle);
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            float mouseSensitivity = MissionScreen.SceneLayer.Input.GetMouseSensivity();
            float inputXRaw = 0.0f;
            float inputYRaw = 0.0f;
            if (!MBCommon.IsPaused && this.Mission.Mode != MissionMode.Barter)
            {
                if (MissionScreen.MouseVisible && !MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
                {
                    float x = MissionScreen.SceneLayer.Input.GetMousePositionRanged().x;
                    float y = MissionScreen.SceneLayer.Input.GetMousePositionRanged().y;
                    if (x <= 0.01 && y >= 0.01 && y <= 0.25)
                        inputXRaw = -1000f * dt;
                    else if (x >= 0.99 && (y >= 0.01 && y <= 0.25))
                        inputXRaw = 1000f * dt;
                }
                else
                {
                    if (!MissionScreen.SceneLayer.Input.GetIsMouseActive())
                    {
                        double num3 = (double) dt / 0.000600000028498471;
                        inputXRaw = (float) num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisX") +
                                    MissionScreen.SceneLayer.Input.GetMouseMoveX();
                        inputYRaw = (float) -num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisY") +
                                    MissionScreen.SceneLayer.Input.GetMouseMoveY();
                    }
                    else
                    {
                        inputXRaw = MissionScreen.SceneLayer.Input.GetMouseMoveX();
                        inputYRaw = MissionScreen.SceneLayer.Input.GetMouseMoveY();
                    }
                }
            }
            float num4 = 5.4E-05f;
            float smoothFading;
            if (this._cameraRotateSmoothMode)
            {
                num4 *= 0.02f;
                smoothFading = Math.Max(0.0f, (float)(1.0 - 2.0 * (0.0199999995529652 + (double)dt - 8.0 * ((double)dt * (double)dt))));
            }
            else
                smoothFading = 0.0f;
            this._cameraBearingDelta *= smoothFading;
            this._cameraElevationDelta *= smoothFading;
            bool isSessionActive = GameNetwork.IsSessionActive;
            float inputScale = num4 * mouseSensitivity * MissionScreen.CameraViewAngle;
            float inputX = -inputXRaw * inputScale;
            float inputY = (NativeConfig.InvertMouse ? inputYRaw : -inputYRaw) * inputScale;
            if (isSessionActive)
            {
                float maxValue = (float)(0.300000011920929 + 10.0 * (double)dt);
                inputX = MBMath.ClampFloat(inputX, -maxValue, maxValue);
                inputY = MBMath.ClampFloat(inputY, -maxValue, maxValue);
            }
            this._cameraBearingDelta += inputX;
            this._cameraElevationDelta += inputY;
            if (isSessionActive)
            {
                float maxValue = (float)(0.300000011920929 + 10.0 * (double)dt);
                this._cameraBearingDelta = MBMath.ClampFloat(this._cameraBearingDelta, -maxValue, maxValue);
                this._cameraElevationDelta = MBMath.ClampFloat(this._cameraElevationDelta, -maxValue, maxValue);
            }
            this.Mission.Scene.RayCastForClosestEntityOrTerrain(this.CombatCamera.Position, this.CombatCamera.Position + this.CombatCamera.Direction * 3000f, out var collisionDistance, out GameEntity _, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
            this.Mission.Scene.SetDepthOfFieldFocus(collisionDistance);

            this.CameraBearing += this._cameraBearingDelta;
            this.CameraElevation += this._cameraElevationDelta;
            this.CameraElevation = MBMath.ClampFloat(this.CameraElevation, -1.36591f, 1.121997f);
        }
    }
}
