using System;
using System.Linq;
using System.Reflection;
using MissionLibrary.Controller.Camera;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera.View
{
    public class FlyCameraMissionView : MissionView, ICameraModeLogic, ICameraController
    {
        private static readonly FieldInfo CameraAddedElevation =
            typeof(MissionScreen).GetField("_cameraAddedElevation", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo SetLastFollowedAgent =
            typeof(MissionScreen).GetProperty("LastFollowedAgent")?.GetSetMethod(true);
        private static readonly MethodInfo SetCameraBearing =
            typeof(MissionScreen).GetProperty("CameraBearing")?.GetSetMethod(true);
        private static readonly MethodInfo SetCameraElevation =
            typeof(MissionScreen).GetProperty("CameraElevation")?.GetSetMethod(true);

        private static readonly FieldInfo CameraSpecialCurrentAddedElevation =
            typeof(MissionScreen).GetField("_cameraSpecialCurrentAddedElevation", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraSpecialCurrentAddedBearing =
            typeof(MissionScreen).GetField("_cameraSpecialCurrentAddedBearing", BindingFlags.Instance | BindingFlags.NonPublic);

        private RTSCameraConfig _config;
        private RTSCameraLogic _rtsCameraLogic;
        private SwitchFreeCameraLogic _freeCameraLogic;
        private MissionMainAgentController _missionMainAgentController;

        private readonly int _shiftSpeedMultiplier = 3;
        private Vec3 _cameraSpeed;
        private float _cameraSpeedMultiplier;
        private bool _cameraSmoothMode;
        private float _cameraHeightToAdd;
        private float _cameraHeightLimit;
        private readonly bool _classicMode = true;
        private bool _isOrderViewOpen;
        private bool _levelToEdge;
        private bool _lockToAgent;
        private GauntletLayer _showControlHintLayer;
        private ShowControlHintVM _showControlHintVM;

        public bool LockToAgent
        {
            get => _lockToAgent;
            set
            {
                if (value == _lockToAgent)
                    return;
                _forceMove = false;
                _lockToAgent = value;
                if (_lockToAgent)
                {
                    if (MissionScreen.LastFollowedAgent != null && MissionScreen.LastFollowedAgent.Team == Mission.PlayerTeam)
                    {
                        _showControlHintVM.SetShowText(true, true, MissionScreen.LastFollowedAgent.Name);
                    }
                }
                else if ((_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera))
                {
                    _showControlHintVM.SetShowText(false, false);
                }
            }
        }

        private bool _forceMove;
        private Vec3 _forceMoveVec;
        private float _forceMoveInvertedProgress;

        private float _cameraBearingDelta;
        private float _cameraElevationDelta;
        private float _viewAngle = 65.0f;
        public float CameraBearing { get; private set; }

        public float CameraElevation { get; private set; }

        public bool ConstantSpeed;

        public bool IgnoreTerrain = false;

        public bool IgnoreBoundaries = false;

        public float ViewAngle
        {
            get => _viewAngle;
            set
            {
                _viewAngle = value;
                if (_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera || LockToAgent)
                    return;
                UpdateViewAngle();
            }
        }

        public float Zoom { get; set; } = 1.0f;

        // legacy.
        public bool CameraRotateSmoothMode = true;
        public bool SmoothRotationMode
        {
            get => CameraRotateSmoothMode;
            set => CameraRotateSmoothMode = value;
        }

        // legacy
        public float CameraSpeedFactor = 1;

        // legacy
        public float CameraVerticalSpeedFactor = 1;

        public float MovementSpeedFactor
        {
            get => CameraSpeedFactor;
            set => CameraSpeedFactor = value;
        }
        public float VerticalMovementSpeedFactor
        {
            get => CameraVerticalSpeedFactor;
            set => CameraVerticalSpeedFactor = value;
        }

        public float DepthOfFieldDistance
        {
            get => _depthOfFieldDistance;
            set
            {
                _depthOfFieldDistance = value;
                if (_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera || LockToAgent)
                    return;
                UpdateDof();
            }
        }

        public float DepthOfFieldStart
        {
            get => _depthOfFieldStart;
            set
            {
                _depthOfFieldStart = value;
                if (_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera || LockToAgent)
                    return;
                UpdateDof();
            }
        }

        public float DepthOfFieldEnd
        {
            get => _depthOfFieldEnd;
            set
            {
                _depthOfFieldEnd = value;
                if (_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera || LockToAgent)
                    return;
                UpdateDof();
            }
        }

        private float _depthOfFieldDistance;
        private float _depthOfFieldStart;
        private float _depthOfFieldEnd;

        //public float CameraBearing => MissionScreen.CameraBearing;

        //public float CameraElevation => MissionScreen.CameraElevation;

        public Camera CombatCamera => MissionScreen.CombatCamera;

        public Vec3 CameraPosition { get; set; }

        public FlyCameraMissionView()
        {
            ACameraControllerManager.Get().Instance = this;
        }

        public void FocusOnAgent(Agent agent)
        {
            LockToAgent = true;
            typeof(MissionScreen).GetProperty("LastFollowedAgent")?.GetSetMethod(true)
                ?.Invoke(MissionScreen, new object[] { agent });
            if (!_freeCameraLogic.IsSpectatorCamera)
                _freeCameraLogic.SwitchCamera();
            Utility.SmoothMoveToAgent(MissionScreen, true, false);
        }

        private void LeaveFromAgent()
        {
            LockToAgent = false;
            CameraPosition = MissionScreen.CombatCamera.Position;
            CameraBearing = MissionScreen.CameraBearing +
                (float?)CameraSpecialCurrentAddedBearing?.GetValue(MissionScreen) ?? 0;
            CameraElevation = MissionScreen.CameraElevation +
                (float?)CameraSpecialCurrentAddedElevation?.GetValue(MissionScreen) ?? 0;
            SetLastFollowedAgent?.Invoke(MissionScreen, new object[] { null });
            MissionScreen.LastFollowedAgentVisuals = null;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _cameraSpeed = Vec3.Zero;
            _cameraSpeedMultiplier = 1f;
            _cameraHeightToAdd = 0.0f;
            _cameraHeightLimit = 0.0f;
            _cameraSmoothMode = true;
            ViewOrderPriorty = 1;

            _config = RTSCameraConfig.Get();
            ConstantSpeed = _config.ConstantSpeed;
            IgnoreTerrain = _config.IgnoreTerrain;
            IgnoreBoundaries = _config.IgnoreBoundaries;
            _rtsCameraLogic = Mission.GetMissionBehaviour<RTSCameraLogic>();
            _freeCameraLogic = _rtsCameraLogic.SwitchFreeCameraLogic;
            _missionMainAgentController = Mission.GetMissionBehaviour<MissionMainAgentController>();

            _showControlHintVM = new ShowControlHintVM(Mission.GetMissionBehaviour<SiegeDeploymentHandler>() == null);
            _showControlHintLayer = new GauntletLayer(ViewOrderPriorty);
            _showControlHintLayer.LoadMovie("RTSCameraShowControlHint", _showControlHintVM);
            MissionScreen.AddLayer(_showControlHintLayer);

            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            MissionLibrary.Event.MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;

            MissionScreen.OnSpectateAgentFocusIn += MissionScreenOnSpectateAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut += MissionScreenOnSpectateAgentFocusOut;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            MissionScreen.RemoveLayer(_showControlHintLayer);
            _showControlHintLayer = null;
            _showControlHintVM = null;

            MissionScreen.OnSpectateAgentFocusIn -= MissionScreenOnSpectateAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut -= MissionScreenOnSpectateAgentFocusOut;

            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            MissionLibrary.Event.MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            _freeCameraLogic = null;
            _missionMainAgentController = null;
            _config = null;

            ACameraControllerManager.Get().Clear();
        }

        public override bool UpdateOverridenCamera(float dt)
        {
            if (_freeCameraLogic == null || !_freeCameraLogic.IsSpectatorCamera || LockToAgent)
                return base.UpdateOverridenCamera(dt);

            UpdateFlyCamera(dt);
            return true;
        }

        public SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
        {
            ICameraModeLogic otherCameraModeLogic =
                Mission.MissionBehaviours.FirstOrDefault(
                        b => !(b is FlyCameraMissionView) && b is ICameraModeLogic) as
                    ICameraModeLogic;
            if (_freeCameraLogic?.IsSpectatorCamera ?? false)
            {
                return LockToAgent ? SpectatorCameraTypes.LockToAnyAgent : SpectatorCameraTypes.Free;
            }
            return otherCameraModeLogic?.GetMissionCameraLockMode(lockedToMainPlayer) ?? SpectatorCameraTypes.Invalid;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (affectedAgent == MissionScreen.LastFollowedAgent && LockToAgent)
            {
                LeaveFromAgent();
            }
        }

        private void MissionScreenOnSpectateAgentFocusIn(Agent agent)
        {
            _showControlHintVM.SetShowText(true,
                !WatchBattleBehavior.WatchMode && (LockToAgent || Mission.MainAgent == null) && agent.Team != null &&
                agent.Team == Mission.PlayerTeam, LockToAgent ? agent.Name : null);
        }

        private void MissionScreenOnSpectateAgentFocusOut(Agent agent)
        {
            _showControlHintVM.SetShowText(false,
                !WatchBattleBehavior.WatchMode && Mission.MainAgent == null &&
                Mission.PlayerTeam?.ActiveAgents.Count > 0);
        }

        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
        {
            _isOrderViewOpen = e.IsOrderEnabled;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            if (!freeCamera)
            {
                LockToAgent = false;
            }
            else if (!LockToAgent)
            {
                BeginForcedMove(new Vec3(0, 0, _config.RaisedHeight));
                LeaveFromAgent();
            }
        }


        private void UpdateFlyCamera(float dt)
        {
            HandleRotateInput(dt);
            MatrixFrame cameraFrame = MatrixFrame.Identity;
            cameraFrame.rotation.RotateAboutSide(1.570796f);
            cameraFrame.rotation.RotateAboutForward(CameraBearing);
            cameraFrame.rotation.RotateAboutSide(
                CameraElevation + (float?)CameraAddedElevation?.GetValue(MissionScreen) ?? 0f);
            cameraFrame.origin = CameraPosition;
            if (_forceMove)
                cameraFrame.origin += ForcedMoveTick(dt);
            float heightFactorForHorizontalMove;
            float heightFactorForVerticalMove;
            if (!ConstantSpeed)
            {
                float heightAtPosition =
                    Mission.Scene.GetGroundHeightAtPosition(cameraFrame.origin, BodyFlags.CommonCollisionExcludeFlags, false);
                heightFactorForHorizontalMove = MathF.Clamp((float)(1.0 + (cameraFrame.origin.z - (double)heightAtPosition - 0.5) / 2),
                    1, 30);
                heightFactorForVerticalMove = MathF.Clamp((float)(1.0 + (cameraFrame.origin.z - (double)heightAtPosition - 0.5) / 2),
                    1, 20);
            }
            else
            {
                heightFactorForHorizontalMove = 1;
                heightFactorForVerticalMove = 1;
            }
            if (MissionScreen.InputManager.IsHotKeyPressed("MissionScreenHotkeyIncreaseCameraSpeed"))
                _cameraSpeedMultiplier *= 1.5f;
            if (MissionScreen.InputManager.IsHotKeyPressed("MissionScreenHotkeyDecreaseCameraSpeed"))
                _cameraSpeedMultiplier *= 0.6666667f;
            if (MissionScreen.InputManager.IsHotKeyPressed("ResetCameraSpeed"))
                _cameraSpeedMultiplier = 1f;
            _cameraSpeed *= (float)(1.0 - 10 * (double)dt);
            if (MissionScreen.InputManager.IsControlDown())
            {
                float num = MissionScreen.SceneLayer.Input.GetDeltaMouseScroll() * 0.008333334f;
                if (num > 0.00999999977648258)
                    _cameraSpeedMultiplier *= 1.25f;
                else if (num < -0.00999999977648258)
                    _cameraSpeedMultiplier *= 0.8f;
            }
            float num1 = 3f * _cameraSpeedMultiplier * MovementSpeedFactor;
            if (MissionScreen.SceneLayer.Input.IsGameKeyDown(CombatHotKeyCategory.Zoom))
                num1 *= _shiftSpeedMultiplier;
            if (!_cameraSmoothMode)
            {
                _cameraSpeed.x = 0.0f;
                _cameraSpeed.y = 0.0f;
                _cameraSpeed.z = 0.0f;
            }
            if (!MissionScreen.InputManager.IsControlDown() || !MissionScreen.InputManager.IsAltDown())
            {
                Vec3 keyInput = Vec3.Zero;
                Vec3 mouseInput = Vec3.Zero;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveForward).IsKeyDown(Input))
                    ++keyInput.y;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveBackward).IsKeyDown(Input))
                    --keyInput.y;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveLeft).IsKeyDown(Input))
                    --keyInput.x;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveRight).IsKeyDown(Input))
                    ++keyInput.x;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveUp).IsKeyDown(Input))
                    ++keyInput.z;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveDown).IsKeyDown(Input))
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
                _cameraSpeed += (keyInput + mouseInput) * num1 * heightFactorForHorizontalMove;
            }
            float horizontalLimit = heightFactorForHorizontalMove * num1;
            float verticalLimit = heightFactorForVerticalMove * num1 * VerticalMovementSpeedFactor;
            _cameraSpeed.x = MBMath.ClampFloat(_cameraSpeed.x, -horizontalLimit, horizontalLimit);
            _cameraSpeed.y = MBMath.ClampFloat(_cameraSpeed.y, -horizontalLimit, horizontalLimit);
            _cameraSpeed.z = MBMath.ClampFloat(_cameraSpeed.z, -verticalLimit, verticalLimit);
            if (_classicMode)
            {
                cameraFrame.origin += _cameraSpeed.x * cameraFrame.rotation.s.AsVec2.ToVec3().NormalizedCopy() * dt;
                ref Vec3 local = ref cameraFrame.origin;
                Vec3 vec3_2 = local;
                double y = _cameraSpeed.y;
                Vec3 vec3_1 = cameraFrame.rotation.u.AsVec2.ToVec3();
                Vec3 vec3_3 = vec3_1.NormalizedCopy();
                Vec3 vec3_4 = (float)y * vec3_3 * dt;
                local = vec3_2 - vec3_4;
                cameraFrame.origin.z += _cameraSpeed.z * dt;
                if (!MissionScreen.SceneLayer.Input.IsControlDown())
                    _cameraHeightToAdd -= (float)(TaleWorlds.InputSystem.Input.DeltaMouseScroll / 200.0) * verticalLimit;
                // hold middle button and move mouse vertically to adjust height
                if (MissionScreen.SceneLayer.Input.IsHotKeyDown("DeploymentCameraIsActive"))
                {
                    if (_levelToEdge == false)
                    {
                        _levelToEdge = true;
                        ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(true);
                    }
                    _cameraHeightToAdd += 0.5f * TaleWorlds.InputSystem.Input.MouseMoveY;
                }
                else if (_levelToEdge)
                {
                    _levelToEdge = false;
                    ScreenManager.FirstHitLayer.InputRestrictions.SetMouseVisibility(false);
                }
                _cameraHeightToAdd = MathF.Clamp(_cameraHeightToAdd, -verticalLimit, verticalLimit);
                if (MathF.Abs(_cameraHeightToAdd) > 1.0 / 1000.0)
                {
                    cameraFrame.origin.z += (float)(_cameraHeightToAdd * (double)dt);
                    _cameraHeightToAdd *= (float)(1.0 - dt * 5.0);
                }
                else
                {
                    cameraFrame.origin.z += _cameraHeightToAdd * dt;
                    _cameraHeightToAdd = 0.0f;
                }
                if (_cameraHeightLimit > 0.0 && cameraFrame.origin.z > (double)_cameraHeightLimit)
                    cameraFrame.origin.z = _cameraHeightLimit;
            }
            else
            {
                cameraFrame.origin += _cameraSpeed.x * cameraFrame.rotation.s * dt;
                cameraFrame.origin -= _cameraSpeed.y * cameraFrame.rotation.u * dt;
                cameraFrame.origin += _cameraSpeed.z * cameraFrame.rotation.f * dt;
            }
            if (!MBEditor.IsEditModeOn)
            {
                if (!IgnoreBoundaries && !Mission.IsPositionInsideBoundaries(cameraFrame.origin.AsVec2))
                    cameraFrame.origin.AsVec2 = Mission.GetClosestBoundaryPosition(cameraFrame.origin.AsVec2);
                float heightAtPosition1 = Mission.Scene.GetGroundHeightAtPosition(cameraFrame.origin + new Vec3(0.0f, 0.0f, 100f));
                if (!MissionScreen.IsCheatGhostMode && !IgnoreTerrain && heightAtPosition1 < 9999.0)
                    cameraFrame.origin.z = Math.Max(cameraFrame.origin.z, heightAtPosition1 + 0.5f);
                if (cameraFrame.origin.z > heightAtPosition1 + 80.0)
                    cameraFrame.origin.z = heightAtPosition1 + 80f;
                if (cameraFrame.origin.z < -100.0)
                    cameraFrame.origin.z = -100f;
            }
            UpdateCameraFrameAndDof(cameraFrame);
        }

        private void UpdateCameraFrameAndDof(MatrixFrame matrixFrame)
        {
            CameraPosition = matrixFrame.origin;
            CombatCamera.Frame = matrixFrame;
            UpdateDof();
            UpdateViewAngle();
            MissionScreen.SceneView?.SetCamera(CombatCamera);
            Mission.SetCameraFrame(ref matrixFrame, Zoom);
            SetCameraBearing?.Invoke(MissionScreen, new object[1] { CameraBearing });
            SetCameraElevation?.Invoke(MissionScreen, new object[1] { CameraElevation });
        }

        private void UpdateDof()
        {
            if (DepthOfFieldEnd < 0.0001f)
            {
                Mission.Scene.SetDepthOfFieldParameters(0, 0, false);
                Mission.Scene.SetDepthOfFieldFocus(0);
            }
            else
            {
                Mission.Scene.SetDepthOfFieldParameters(DepthOfFieldStart, DepthOfFieldEnd, false);
                if (Math.Abs(DepthOfFieldDistance) < 0.0001f)
                {
                    Mission.Scene.RayCastForClosestEntityOrTerrain(CombatCamera.Position, CombatCamera.Position + CombatCamera.Direction * 3000f, out var terrainCollisionDistance, out GameEntity _, 0.5f, BodyFlags.CameraCollisionRayCastExludeFlags);
                    Mission.RayCastForClosestAgent(CameraPosition,
                        CombatCamera.Position + CombatCamera.Direction * 3000f, out var agentCollisionDistance);
                    if (float.IsNaN(terrainCollisionDistance))
                    {
                        terrainCollisionDistance = float.MaxValue;
                    }

                    if (float.IsNaN(agentCollisionDistance))
                    {
                        agentCollisionDistance = float.MaxValue;
                    }
                    Mission.Scene.SetDepthOfFieldFocus(Math.Min(terrainCollisionDistance, agentCollisionDistance));
                }
                else
                {
                    Mission.Scene.SetDepthOfFieldFocus(DepthOfFieldDistance);
                }
            }
        }

        private void UpdateViewAngle()
        {
            float newDNear = !Mission.CameraIsFirstPerson ? 0.1f : 0.065f;
            CombatCamera.SetFovVertical((float)(ViewAngle * (Math.PI / 180.0)), Screen.AspectRatio, newDNear, 12500f);
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
            _forceMoveInvertedProgress *= (float)Math.Pow(0.00001, dt);
            if (Math.Abs(_forceMoveInvertedProgress) < 0.00001f)
            {
                _forceMove = false;
                _forceMoveInvertedProgress = 1;
                _forceMoveVec = Vec3.Zero;
            }
            return _forceMoveVec * (previousProgress - _forceMoveInvertedProgress);
        }

        private void HandleRotateInput(float dt)
        {
            float mouseSensitivity = MissionScreen.SceneLayer.Input.GetMouseSensitivity();
            float inputXRaw = 0.0f;
            float inputYRaw = 0.0f;
            if (!MBCommon.IsPaused && Mission.Mode != MissionMode.Barter)
            {
                if (MissionScreen.MouseVisible)
                {
                    float x = MissionScreen.SceneLayer.Input.GetMousePositionRanged().x;
                    float y = MissionScreen.SceneLayer.Input.GetMousePositionRanged().y;
                    if (x <= 0.01 && y >= 0.01 && y <= 0.25)
                        inputXRaw = -1000f * dt;
                    else if (x >= 0.99 && y >= 0.01 && y <= 0.25)
                        inputXRaw = 1000f * dt;
                }
                else
                {
                    if (!MissionScreen.SceneLayer.Input.GetIsMouseActive())
                    {
                        double num3 = dt / 0.000600000028498471;
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
            if (SmoothRotationMode)
            {
                num4 *= 0.10f;
                smoothFading = (float)Math.Pow(0.000001, dt);
            }
            else
                smoothFading = 0.0f;
            _cameraBearingDelta *= smoothFading;
            _cameraElevationDelta *= smoothFading;
            bool isSessionActive = GameNetwork.IsSessionActive;
            float inputScale = num4 * mouseSensitivity * MissionScreen.CameraViewAngle;
            float inputX = -inputXRaw * inputScale;
            float inputY = (NativeConfig.InvertMouse ? inputYRaw : -inputYRaw) * inputScale;
            if (isSessionActive)
            {
                float maxValue = (float)(0.300000011920929 + 10.0 * dt);
                inputX = MBMath.ClampFloat(inputX, -maxValue, maxValue);
                inputY = MBMath.ClampFloat(inputY, -maxValue, maxValue);
            }
            _cameraBearingDelta += inputX;
            _cameraElevationDelta += inputY;
            if (isSessionActive)
            {
                float maxValue = (float)(0.300000011920929 + 10.0 * dt);
                _cameraBearingDelta = MBMath.ClampFloat(_cameraBearingDelta, -maxValue, maxValue);
                _cameraElevationDelta = MBMath.ClampFloat(_cameraElevationDelta, -maxValue, maxValue);
            }

            CameraBearing += _cameraBearingDelta;
            CameraElevation += _cameraElevationDelta;
            CameraElevation = MBMath.ClampFloat(CameraElevation, -1.36591f, 1.121997f);
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            //if (!_isOrderViewOpen &&
            //    (Input.IsGameKeyReleased(8) ||
            //     (Input.IsGameKeyReleased(9) && !_rightButtonDraggingMode)))
            //{
            //    LockToAgent = true;
            //}

            if (LockToAgent && (Math.Abs(Input.GetDeltaMouseScroll()) > 0.0001f ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveForward).IsKeyDown(Input) ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveBackward).IsKeyDown(Input) ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveLeft).IsKeyDown(Input) ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveRight).IsKeyDown(Input) ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveUp).IsKeyDown(Input) ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveDown).IsKeyDown(Input) ||
                                Input.GetIsControllerConnected() &&
                                (Input.GetKeyState(InputKey.ControllerLStick).y != 0.0 ||
                                 Input.GetKeyState(InputKey.ControllerLStick).x != 0.0)))
            {
                LeaveFromAgent();
            }
        }
    }
}
