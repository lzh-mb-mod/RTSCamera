using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Conversation.Tags;
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
    public class FlyCameraMissionView : MissionView, ICameraModeLogic
    {
        private EnhancedMissionConfig _config;
        private SwitchFreeCameraLogic _freeCameraLogic;
        private MissionMainAgentController _missionMainAgentController;
        private EnhancedMissionOrderUIHandler _orderUIHandler;

        private int _shiftSpeedMultiplier = 3;
        private Vec3 _cameraSpeed;
        private float _cameraSpeedMultiplier;
        private bool _cameraSmoothMode;
        private float _cameraHeightToAdd;
        private float _cameraHeightLimit;
        private bool _classicMode = true;
        private bool _isOrderViewOpen = false;
        private bool _resetDraggingMode;
        private bool _rightButtonDraggingMode;
        private Vec2 _clickedPositionPixel = Vec2.Zero;
        private bool _setCombatActionOnNextTick = false;
        private bool _levelToEdge = false;
        private bool _lockToAgent = false;

        private bool _forceMove = false;
        private Vec3 _forceMoveVec;
        private float _forceMoveInvertedProgress = 0;

        private float _cameraBearingDelta;
        private float _cameraElevationDelta;
        private float _cameraViewAngle = 65.0f;
        private float _zoom = 1.0f;
        public float CameraBearing { get; private set; }

        public float CameraElevation { get; private set; }

        public bool CameraRotateSmoothMode = false;
        public float CameraViewAngle
        {
            get => _cameraViewAngle;
            set
            {
                _cameraViewAngle = value;
                UpdateOverridenCamera(0);
            }
        }

        public float Zoom
        {
            get => _zoom;
            set
            {
                _zoom = value;
                UpdateOverridenCamera(0);
            }
        }

        public float DepthOfFieldDistance
        {
            get => _depthOfFieldDistance;
            set
            {
                _depthOfFieldDistance = value;
                UpdateOverridenCamera(0);

            }
        }

        public float DepthOfFieldStart
        {
            get => _depthOfFieldStart;
            set
            {
                _depthOfFieldStart = value;
                UpdateOverridenCamera(0);
            }
        }

        public float DepthOfFieldEnd
        {
            get => _depthOfFieldEnd;
            set
            {
                _depthOfFieldEnd = value;
                UpdateOverridenCamera(0);
            }
        }


        public float CameraSpeedFactor = 1;

        public float CameraVerticalSpeedFactor = 1;
        private float _depthOfFieldDistance = 0;
        private float _depthOfFieldStart = 0;
        private float _depthOfFieldEnd = 0;

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
            this.ViewOrderPriorty = 25;

            _config = EnhancedMissionConfig.Get();
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
            _config = null;
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
            if (_freeCameraLogic == null || !_freeCameraLogic.isSpectatorCamera || _lockToAgent)
                return base.UpdateOverridenCamera(dt);

            UpdateFlyCamera(dt);
            return true;
        }

        public SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
        {
            ICameraModeLogic otherCameraModeLogic =
                this.Mission.MissionBehaviours.FirstOrDefault<MissionBehaviour>(
                        (Func<MissionBehaviour, bool>)(b => !(b is FlyCameraMissionView) && b is ICameraModeLogic)) as
                    ICameraModeLogic;
            return _lockToAgent && (_freeCameraLogic?.isSpectatorCamera ?? false)
                ? SpectatorCameraTypes.LockToAnyAgent
                : otherCameraModeLogic?.GetMissionCameraLockMode(lockedToMainPlayer) ?? SpectatorCameraTypes.Invalid;
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
            if (freeCamera && _isOrderViewOpen)
                _setCombatActionOnNextTick = true;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _orderUIHandler?.gauntletLayer.InputRestrictions.SetMouseVisibility(freeCamera && _isOrderViewOpen);
            if (_missionMainAgentController != null)
            {
                _missionMainAgentController.IsDisabled = freeCamera;
            }

            this._lockToAgent = false;
            if (freeCamera)
            {
                this.CameraBearing = MissionScreen.CameraBearing;
                this.CameraElevation = MissionScreen.CameraElevation;
                BeginForcedMove(new Vec3(0, 0, _config.RaisedHeight));
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
            HandleRotateInput(dt);
            MatrixFrame cameraFrame1 = MatrixFrame.Identity;
            cameraFrame1.rotation.RotateAboutSide(1.570796f);
            cameraFrame1.rotation.RotateAboutForward(this.CameraBearing);
            cameraFrame1.rotation.RotateAboutSide(this.CameraElevation);
            cameraFrame1.origin = CameraPosition;
            if (_forceMove)
                cameraFrame1.origin += ForcedMoveTick(dt);
            float heightAtPosition = this.Mission.Scene.GetGroundHeightAtPosition(cameraFrame1.origin, BodyFlags.CommonCollisionExcludeFlags, true);
            float heightFactorForHorizontalMove = MathF.Clamp((float)(1.0 + ((double)cameraFrame1.origin.z - (double)heightAtPosition - 0.5) / 2), 1, 30);
            float heightFactorForVerticalMove = MathF.Clamp((float)(1.0 + ((double)cameraFrame1.origin.z - (double)heightAtPosition - 0.5) / 2), 1, 20);
            if (this.DebugInput.IsHotKeyPressed("MissionScreenHotkeyIncreaseCameraSpeed"))
                this._cameraSpeedMultiplier *= 1.5f;
            if (this.DebugInput.IsHotKeyPressed("MissionScreenHotkeyDecreaseCameraSpeed"))
                this._cameraSpeedMultiplier *= 0.6666667f;
            if (this.DebugInput.IsHotKeyPressed("ResetCameraSpeed"))
                this._cameraSpeedMultiplier = 1f;
            this._cameraSpeed *= (float)(1.0 - 10 * (double)dt);
            if (this.DebugInput.IsControlDown())
            {
                float num = MissionScreen.SceneLayer.Input.GetDeltaMouseScroll() * 0.008333334f;
                if ((double)num > 0.00999999977648258)
                    this._cameraSpeedMultiplier *= 1.25f;
                else if ((double)num < -0.00999999977648258)
                    this._cameraSpeedMultiplier *= 0.8f;
            }
            float num1 = 3f * this._cameraSpeedMultiplier * CameraSpeedFactor;
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
                        if (x <= 0.01 && (y < 0.01 || y > 0.25))
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
            float horizontalLimit = heightFactorForHorizontalMove * num1;
            float verticalLimit = heightFactorForVerticalMove * num1 * CameraVerticalSpeedFactor;
            this._cameraSpeed.x = MBMath.ClampFloat(this._cameraSpeed.x, -horizontalLimit, horizontalLimit);
            this._cameraSpeed.y = MBMath.ClampFloat(this._cameraSpeed.y, -horizontalLimit, horizontalLimit);
            this._cameraSpeed.z = MBMath.ClampFloat(this._cameraSpeed.z, -verticalLimit, verticalLimit);
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
                if (!MissionScreen.SceneLayer.Input.IsControlDown())
                    this._cameraHeightToAdd -= (float)(TaleWorlds.InputSystem.Input.DeltaMouseScroll / 200.0) * verticalLimit;
                // hold middle button and move mouse vertically to adjust height
                if (MissionScreen.SceneLayer.Input.IsHotKeyDown("DeploymentCameraIsActive"))
                {
                    if (_levelToEdge == false)
                    {
                        _levelToEdge = true;
                        ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(true);
                    }
                    this._cameraHeightToAdd += 0.5f * TaleWorlds.InputSystem.Input.MouseMoveY;
                }
                else if (_levelToEdge == true)
                {
                    _levelToEdge = false;
                    ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(false);
                }
                this._cameraHeightToAdd = MathF.Clamp(this._cameraHeightToAdd, -verticalLimit, verticalLimit);
                if ((double)MathF.Abs(this._cameraHeightToAdd) > 1.0 / 1000.0)
                {
                    cameraFrame1.origin.z += (float)((double)this._cameraHeightToAdd * (double)dt);
                    this._cameraHeightToAdd *= (float)(1.0 - dt * 5.0);
                }
                else
                {
                    cameraFrame1.origin.z += this._cameraHeightToAdd * dt;
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
            this.CombatCamera.SetFovVertical((float)(this.CameraViewAngle * (Math.PI / 180.0)), TaleWorlds.Engine.Screen.AspectRatio, newDNear, 12500f);
            this.CombatCamera.Frame = cameraFrame1;
            MissionScreen.SceneView.SetCamera(this.CombatCamera);
            this.Mission.SetCameraFrame(cameraFrame1, Zoom);
        }

        private void BeginForcedMove(Vec3 vec)
        {
            _forceMove = true;
            _forceMoveInvertedProgress = 1;
            _forceMoveVec = vec;
        }

        private Vec3 ForcedMoveTick(float dt)
        {
            var previousProgress = _forceMoveInvertedProgress;
            _forceMoveInvertedProgress *= (float)Math.Pow(0.00001, (double)dt);
            if (Math.Abs(_forceMoveInvertedProgress) < 0.0001f)
            {
                _forceMove = false;
                _forceMoveInvertedProgress = 1;
                _forceMoveVec = Vec3.Zero;
            }
            return _forceMoveVec * (previousProgress - _forceMoveInvertedProgress);
        }

        private void HandleRotateInput(float dt)
        {

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
                        double num3 = (double)dt / 0.000600000028498471;
                        inputXRaw = (float)num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisX") +
                                    MissionScreen.SceneLayer.Input.GetMouseMoveX();
                        inputYRaw = (float)-num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisY") +
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
            if (this.CameraRotateSmoothMode)
            {
                num4 *= 0.10f;
                smoothFading = (float)Math.Pow(0.000001, dt);
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

            Mission.Scene.SetDepthOfFieldParameters(DepthOfFieldStart, DepthOfFieldEnd, false);
            if (Math.Abs(DepthOfFieldDistance) < 0.0001f)
            {
                this.Mission.Scene.RayCastForClosestEntityOrTerrain(this.CombatCamera.Position, this.CombatCamera.Position + this.CombatCamera.Direction * 3000f, out var terrainCollisionDistance, out GameEntity _, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags);
                this.Mission.RayCastForClosestAgent(this.CameraPosition,
                    this.CombatCamera.Position + this.CombatCamera.Direction * 3000f, out var agentCollisionDistance);
                if (float.IsNaN(terrainCollisionDistance))
                {
                    terrainCollisionDistance = float.MaxValue;
                }

                if (float.IsNaN(agentCollisionDistance))
                {
                    agentCollisionDistance = float.MaxValue;
                }
                this.Mission.Scene.SetDepthOfFieldFocus(Math.Min(terrainCollisionDistance, agentCollisionDistance));
            }
            else
            {
                Mission.Scene.SetDepthOfFieldFocus(DepthOfFieldDistance);
            }

            this.CameraBearing += this._cameraBearingDelta;
            this.CameraElevation += this._cameraElevationDelta;
            this.CameraElevation = MBMath.ClampFloat(this.CameraElevation, -1.36591f, 1.121997f);
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            if (_orderUIHandler != null)
                UpdateDragData();
            if (!_isOrderViewOpen &&
                (Input.IsGameKeyReleased(8) ||
                 (Input.IsGameKeyReleased(9) && !this._rightButtonDraggingMode)))
            {
                _lockToAgent = true;
            }

            else if (_lockToAgent && (Math.Abs(Input.GetDeltaMouseScroll()) > 0.0001f ||
                                 Input.IsGameKeyDown(0) || Input.IsGameKeyDown(1) ||
                                 Input.IsGameKeyDown(2) || Input.IsGameKeyDown(3) || Input.GetIsControllerConnected() &&
                                 (Input.GetKeyState(InputKey.ControllerLStick).y != 0.0 ||
                                  Input.GetKeyState(InputKey.ControllerLStick).x != 0.0)))
            {
                _lockToAgent = false;
                this.CameraBearing = MissionScreen.CameraBearing;
                this.CameraElevation = MissionScreen.CameraElevation;
            }
        }
    }
}
