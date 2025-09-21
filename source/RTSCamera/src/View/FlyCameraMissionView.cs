﻿using MissionLibrary.Controller.Camera;
using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using MathF = TaleWorlds.Library.MathF;

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

        private static readonly FieldInfo CameraBearingDelta =
            typeof(MissionScreen).GetField("_cameraBearingDelta", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraElevationDelta =
            typeof(MissionScreen).GetField("_cameraElevationDelta", BindingFlags.Instance | BindingFlags.NonPublic);


        private RTSCameraConfig _config;
        private RTSCameraLogic _rtsCameraLogic;
        private SwitchFreeCameraLogic _freeCameraLogic;

        private readonly int _shiftSpeedMultiplier = 3;
        private Vec3 _cameraSpeed;
        private float _cameraSpeedMultiplier;
        private bool _cameraSmoothMode;
        private float _cameraHeightToAdd;
        private float _cameraHeightLimit;
        private float? _previousHeightToTerrain;
        private readonly bool _classicMode = true;
        private bool _levelToEdge;
        private bool _lockToAgent;
        private GauntletLayer _showControlHintLayer;
        private ShowControlHintVM _showControlHintVM;

        private float _cameraDistanceLimitVerticalScale = 1.5f;

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
                    DisableControlHint();
                }
            }
        }

        public Formation FocusedFormation { get; set; } = null;
        private Vec3? _currentPositionLookingAt = null;
        private float _lookingDistance = 10f;
        private float _lookingDistanceToAdd = 0f;

        private bool _forceMove;
        private Vec3 _forceMoveVec;
        private float _forceMoveInvertedProgress;

        private float _cameraBearingDelta;
        private float _cameraElevationDelta;
        private float _viewAngle = 65.0f;
        public float CameraBearing { get; private set; }

        public float CameraElevation { get; private set; }

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
        public float RollAngle { get; set; }

        public FlyCameraMissionView()
        {
            ACameraControllerManager.Get().Instance = this;
        }

        public void FocusOnAgent(Agent agent)
        {
            FocusOnFormation(null);
            LockToAgent = true;
            var shouldSmoothMove = MissionScreen.LastFollowedAgent != agent;
            typeof(MissionScreen).GetProperty("LastFollowedAgent")?.GetSetMethod(true)
                ?.Invoke(MissionScreen, new object[] { agent });
            if (!_freeCameraLogic.IsSpectatorCamera)
                _freeCameraLogic.SwitchCamera();
            if (shouldSmoothMove)
            {
                Utility.SmoothMoveToAgent(MissionScreen, true, false);
            }
        }

        public void FocusOnFormation(Formation formation)
        {
            if (formation != null && LockToAgent)
            {
                LeaveFromAgent();
            }
            FocusedFormation = formation;
            _currentPositionLookingAt = null;
            
        }

        private void LeaveFromAgent()
        {
            LockToAgent = false;
            CameraPosition = MissionScreen.CombatCamera.Frame.origin;
            CameraBearing = MissionScreen.CameraBearing +
                (float?)CameraSpecialCurrentAddedBearing?.GetValue(MissionScreen) ?? 0;
            CameraElevation = MissionScreen.CameraElevation +
                (float)(CameraSpecialCurrentAddedElevation?.GetValue(MissionScreen) ?? 0) +
                (float)(CameraAddedElevation?.GetValue(MissionScreen) ?? 0);
            CameraBearingDelta?.SetValue(MissionScreen, 0);
            CameraElevationDelta?.SetValue(MissionScreen, 0);
            SetLastFollowedAgent?.Invoke(MissionScreen, new object[] { null });
            MissionScreen.LastFollowedAgentVisuals = null;
            _previousHeightToTerrain = null;
        }

        private void LeaveFromAgentOnAgentRemoved()
        {
            LockToAgent = false;
            CameraPosition = MissionScreen.CombatCamera.Frame.origin;
            CameraBearing = MissionScreen.CameraBearing +
                (float?)CameraSpecialCurrentAddedBearing?.GetValue(MissionScreen) ?? 0;
            CameraElevation = MissionScreen.CameraElevation +
                (float)(CameraSpecialCurrentAddedElevation?.GetValue(MissionScreen) ?? 0) +
                (float)(CameraAddedElevation?.GetValue(MissionScreen) ?? 0);
            CameraBearingDelta?.SetValue(MissionScreen, 0);
            CameraElevationDelta?.SetValue(MissionScreen, 0);
            _previousHeightToTerrain = null;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _cameraSpeed = Vec3.Zero;
            _cameraSpeedMultiplier = 1f;
            _cameraHeightToAdd = 0.0f;
            _cameraHeightLimit = 0.0f;
            _cameraSmoothMode = true;
            ViewOrderPriority = 1;

            _config = RTSCameraConfig.Get();
            _rtsCameraLogic = Mission.GetMissionBehavior<RTSCameraLogic>();
            _freeCameraLogic = _rtsCameraLogic.SwitchFreeCameraLogic;

            _showControlHintVM = new ShowControlHintVM(Mission.GetMissionBehavior<SiegeDeploymentHandler>() == null);
            _showControlHintLayer = new GauntletLayer(ViewOrderPriority);
            _showControlHintLayer.LoadMovie("RTSCameraShowControlHint", _showControlHintVM);
            _showControlHintLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Invalid);
            MissionScreen.AddLayer(_showControlHintLayer);

            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;

            MissionScreen.OnSpectateAgentFocusIn += MissionScreenOnSpectateAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut += MissionScreenOnSpectateAgentFocusOut;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            MissionScreen.RemoveLayer(_showControlHintLayer);

            MissionScreen.OnSpectateAgentFocusIn -= MissionScreenOnSpectateAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut -= MissionScreenOnSpectateAgentFocusOut;
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            _showControlHintLayer = null;
            _showControlHintVM = null;

            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            _freeCameraLogic = null;
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
                Mission.MissionBehaviors.FirstOrDefault(
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

            // Since 1.3.0 MissionScreen may be null because that OnMissionScreenFinalize may be already called.
            if (MissionScreen == null)
                return;
            if (affectedAgent == MissionScreen.LastFollowedAgent && _freeCameraLogic.IsSpectatorCamera && LockToAgent)
            {
                LeaveFromAgentOnAgentRemoved();
                //if (!Utility.IsAgentDead(Mission.MainAgent))
                //{
                //    FocusOnAgent(Mission.MainAgent);
                //}
            }
        }

        private void MissionScreenOnSpectateAgentFocusIn(Agent agent)
        {
            _showControlHintVM.SetShowText(true,
                !CommandBattleBehavior.CommandMode && (LockToAgent || Mission.MainAgent == null) && Utility.IsTeamValid(agent.Team) &&
                agent.Team == Mission.PlayerTeam, LockToAgent ? agent.Name : null);
        }

        public void DisableControlHint()
        {
            _showControlHintVM.SetShowText(false, false);
        }

        private void MissionScreenOnSpectateAgentFocusOut(Agent agent)
        {
            _showControlHintVM.SetShowText(false,
                !CommandBattleBehavior.CommandMode && Mission.MainAgent == null &&
                Mission.PlayerTeam?.ActiveAgents.Count > 0);
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            if (!freeCamera)
            {
                LockToAgent = false;
                FocusOnFormation(null);
            }
            else if (!LockToAgent)
            {
                if (Mission.Mode != MissionMode.Deployment)
                {
                    // Move camera backward and up when switching to free camera
                    var direction = MissionScreen.CombatCamera.Direction;
                    var z = direction.z;
                    var directionZLimit = -0.75f;
                    if (z >= directionZLimit)
                    {
                        direction = (direction.AsVec2.Normalized() * MathF.Sqrt(1f - directionZLimit * directionZLimit)).ToVec3() + Vec3.Up * directionZLimit;
                    }
                    var diff = direction / direction.z * _config.RaisedHeight;
                    if (RTSCameraSkillBehavior.ShouldLimitCameraDistance(Mission))
                    {
                        var limit = MathF.Max(RTSCameraSkillBehavior.CameraDistanceLimit - 3f, 1f);
                        var scale = MathF.Min(1f, limit / MathF.Max(diff.AsVec2.Length ,1f), limit * _cameraDistanceLimitVerticalScale / MathF.Max(diff.z, 1f));
                        diff *= scale;
                    }
                    if (MissionScreen.LastFollowedAgent != null)
                    {
                        var moveOrigin = Utility.GetCameraFrameWhenLockedToAgent(MissionScreen, MissionScreen.LastFollowedAgent, SpectatorCameraTypes.LockToMainPlayer, MissionScreen.CameraElevation, MissionScreen.CameraBearing);
                        diff = moveOrigin.origin - MissionScreen.CombatCamera.Frame.origin + diff;
                    }
                    BeginForcedMove(diff);
                }
                LeaveFromAgent();
            }
        }

        private void UpdateFlyCamera(float dt)
        {
            HandleRotateInput(dt);
            MatrixFrame cameraFrame = MatrixFrame.Identity;
            cameraFrame.rotation.RotateAboutSide(1.57079637f);
            cameraFrame.rotation.RotateAboutForward(CameraBearing);
            cameraFrame.rotation.RotateAboutSide(CameraElevation);
            //if (!MissionScreen.IsPhotoModeEnabled)
            //    cameraFrame.rotation.RotateAboutSide((float?)CameraAddedElevation?.GetValue(MissionScreen) ?? 0f);
            cameraFrame.origin = CameraPosition;


            if (FocusedFormation != null)
            {
                if (FocusOnFormationMode.GetPositionToLookAt(FocusedFormation, out Vec3 positionToLookAt))
                {
                    var direction = cameraFrame.rotation.TransformToParent(-Vec3.Up).NormalizedCopy();
                    if (_currentPositionLookingAt == null)
                    {
                        _currentPositionLookingAt = cameraFrame.origin + direction * _lookingDistance;
                        _lookingDistanceToAdd = 0f;
                    }
                    _currentPositionLookingAt += (positionToLookAt - _currentPositionLookingAt) * 2f * dt;
                    var targetPosition = (Vec3)_currentPositionLookingAt - direction * _lookingDistance; // Adjust the distance as needed
                    cameraFrame.origin = targetPosition;
                }
                else
                {
                    FocusOnFormation(null);
                }
            }

            float heightFactorForHorizontalMove;
            float heightFactorForVerticalMove;
            var groundHeight = Mission.Scene.GetGroundHeightAtPosition(cameraFrame.origin);
            var terrainHeight = Mission.Scene.GetTerrainHeight(cameraFrame.origin.AsVec2);

            if (!_config.ConstantSpeed)
            {
                float heightAtPosition = _config.IgnoreTerrain ? terrainHeight : groundHeight;
                heightFactorForHorizontalMove = MathF.Clamp((float)(1.0 + (cameraFrame.origin.z - (double)heightAtPosition - 0.5) / 2),
                    1, 30);
                heightFactorForVerticalMove = MathF.Clamp((float)(1.0 + (cameraFrame.origin.z - (double)heightAtPosition - 0.5) / 2),
                    1f, 20f);
            }
            else
            {
                heightFactorForHorizontalMove = 10;
                heightFactorForVerticalMove = 5;
            }

            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseCameraSpeed).IsKeyPressedInOrder())
                _cameraSpeedMultiplier *= 1.25f;
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseCameraSpeed).IsKeyPressedInOrder())
                _cameraSpeedMultiplier *= 0.8f;
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ResetCameraSpeed).IsKeyPressedInOrder())
                _cameraSpeedMultiplier = 1f;
            _cameraSpeed *= (float)(1.0 - 10 * (double)dt);
            //if (MissionScreen.InputManager.IsControlDown())
            //{
            //    float num = MissionScreen.SceneLayer.Input.GetDeltaMouseScroll() * 0.008333334f;
            //    if (num > 0.00999999977648258)
            //        _cameraSpeedMultiplier *= 1.25f;
            //    else if (num < -0.00999999977648258)
            //        _cameraSpeedMultiplier *= 0.8f;
            //}
            float cameraBasicSpeed = 3f * _cameraSpeedMultiplier * MovementSpeedFactor;
            if (MissionScreen.SceneLayer.Input.IsGameKeyDown(CombatHotKeyCategory.Zoom))
                cameraBasicSpeed *= _shiftSpeedMultiplier;
            if (!_cameraSmoothMode)
            {
                _cameraSpeed.x = 0.0f;
                _cameraSpeed.y = 0.0f;
                _cameraSpeed.z = 0.0f;
            }

            bool hasVerticalInput = false;
            //if (!MissionScreen.InputManager.IsControlDown() || !MissionScreen.InputManager.IsAltDown())
            {
                float keyInputVertical = 0;
                Vec2 keyInput = Vec2.Zero;
                Vec2 mouseInput = Vec2.Zero;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveForward).IsKeyDown())
                    ++keyInput.y;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveBackward).IsKeyDown())
                    --keyInput.y;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveLeft).IsKeyDown())
                    --keyInput.x;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveRight).IsKeyDown())
                    ++keyInput.x;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveUp).IsKeyDown())
                    ++keyInputVertical;
                if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveDown).IsKeyDown())
                    --keyInputVertical;

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

                if (mouseInput.LengthSquared > 0.0)
                    mouseInput.Normalize();

                if (!MissionScreen.MouseVisible && MissionScreen.SceneLayer.Input.GetIsControllerConnected())
                {
                    var x = MissionScreen.SceneLayer.Input.GetKeyState(InputKey.ControllerLStick).X;
                    var y = MissionScreen.SceneLayer.Input.GetKeyState(InputKey.ControllerLStick).Y;
                    if (MathF.Abs(x) < 0.2f)
                        x = 0.0f;
                    if (MathF.Abs(y) < 0.2f)
                        y = 0.0f;
                    keyInput.x += x;
                    keyInput.y += y;
                }
                if (keyInput.LengthSquared > 0.0)
                    keyInput.Normalize();

                hasVerticalInput = keyInputVertical != 0 || Input.GetDeltaMouseScroll() != 0;
                if (keyInput + mouseInput != Vec2.Zero || keyInputVertical != 0)
                {
                    FocusOnFormation(null);
                }
                _cameraSpeed += ((keyInput + mouseInput) * cameraBasicSpeed * heightFactorForHorizontalMove).ToVec3(keyInputVertical * cameraBasicSpeed * heightFactorForVerticalMove);
            }
            float horizontalLimit = heightFactorForHorizontalMove * cameraBasicSpeed;
            float verticalLimit = heightFactorForVerticalMove * cameraBasicSpeed * VerticalMovementSpeedFactor;
            _cameraSpeed.x = MBMath.ClampFloat(_cameraSpeed.x, -horizontalLimit, horizontalLimit);
            _cameraSpeed.y = MBMath.ClampFloat(_cameraSpeed.y, -horizontalLimit, horizontalLimit);
            _cameraSpeed.z = MBMath.ClampFloat(_cameraSpeed.z, -verticalLimit, verticalLimit);

            if (_config.CameraHeightFollowsTerrain)
            {
                if (_previousHeightToTerrain.HasValue && !hasVerticalInput)
                {
                    cameraFrame.origin.z = _previousHeightToTerrain.Value + terrainHeight;
                }
            }
            if (_forceMove)
                cameraFrame.origin += ForcedMoveTick(dt);

            if (_classicMode)
            {
                cameraFrame.origin += _cameraSpeed.x * cameraFrame.rotation.s.AsVec2.ToVec3().NormalizedCopy() * dt;
                ref Vec3 local = ref cameraFrame.origin;
                Vec3 vec3_2 = local;
                float y = _cameraSpeed.y;
                Vec3 vec3_1 = cameraFrame.rotation.u.AsVec2.ToVec3();
                Vec3 vec3_3 = vec3_1.NormalizedCopy();
                Vec3 vec3_4 = y * vec3_3 * dt;
                local = vec3_2 - vec3_4;
                cameraFrame.origin.z += _cameraSpeed.z * dt;
                float mouseScroll = Input.GetDeltaMouseScroll();
                float controllerHeightInput = 0;
                if (MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
                {
                    controllerHeightInput = MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
                    if (MathF.Abs(controllerHeightInput) < 0.2f)
                        controllerHeightInput = 0.0f;
                }
                var diffRatio = MathF.Min(1f, dt * 5f);
                if (FocusedFormation != null)
                {
                    _lookingDistanceToAdd -= (mouseScroll / 200.0f + controllerHeightInput) * cameraBasicSpeed * VerticalMovementSpeedFactor;
                    if (MathF.Abs(_lookingDistanceToAdd) > 1.0 / 1000.0)
                    {
                        _lookingDistance += _lookingDistanceToAdd * diffRatio;
                        _lookingDistanceToAdd *= 1f - diffRatio;
                    }
                    else
                    {
                        _lookingDistance += _lookingDistanceToAdd * MathF.Min(1f, dt);
                        _lookingDistanceToAdd = 0;
                    }
                    if (RTSCameraSkillBehavior.ShouldLimitCameraDistance(Mission))
                    {
                        var maxDistance = MathF.Min(RTSCameraSkillBehavior.CameraDistanceLimit, 50f);
                        if (_lookingDistance > maxDistance)
                        {
                            _lookingDistance -= (_lookingDistance - maxDistance) * diffRatio;
                        }
                    }
                    _lookingDistance = MathF.Max(_lookingDistance, 1f);
                }
                else
                {
                    bool isSpeedKeyDown = RTSCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseCameraSpeed).IsKeyDownInOrder() ||
                        RTSCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseCameraSpeed).IsKeyDownInOrder() ||
                        RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ResetCameraSpeed).IsKeyDownInOrder();
                    if (!isSpeedKeyDown)
                        _cameraHeightToAdd -= (mouseScroll / 2000.0f + controllerHeightInput / 100f) * verticalLimit;
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
                }
                if (MathF.Abs(_cameraHeightToAdd) > 1.0 / 1000.0)
                {
                    cameraFrame.origin.z += _cameraHeightToAdd * diffRatio;
                    _cameraHeightToAdd *= 1f - diffRatio;
                }            
                else
                {
                    cameraFrame.origin.z += _cameraHeightToAdd * MathF.Min(1f, dt);
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

            if (FocusedFormation == null && RTSCameraSkillBehavior.ShouldLimitCameraDistance(Mission))
            {
                LimitCameraDistance(ref cameraFrame, dt, cameraBasicSpeed);
            }

            if (_config.CameraHeightFollowsTerrain)
            {
                _previousHeightToTerrain = cameraFrame.origin.z - terrainHeight;
            }

            if (!MBEditor.IsEditModeOn)
            {
                if (!_config.IgnoreBoundaries && !Mission.IsPositionInsideBoundaries(cameraFrame.origin.AsVec2))
                    cameraFrame.origin.AsVec2 = Mission.GetClosestBoundaryPosition(cameraFrame.origin.AsVec2);
                if (!_config.IgnoreBoundaries && Mission.Mode == MissionMode.Deployment)
                {
                    IMissionDeploymentPlan deploymentPlan = Mission.DeploymentPlan;
                    if (deploymentPlan?.HasDeploymentBoundaries(Mission.PlayerTeam) ?? false)
                    {
                        Vec2 cameraFrameVec2 = cameraFrame.origin.AsVec2;
                        if (!deploymentPlan.IsPositionInsideDeploymentBoundaries(Mission.PlayerTeam, in cameraFrameVec2))
                        {
                            Vec2 boundaryPosition = deploymentPlan.GetClosestDeploymentBoundaryPosition(Mission.PlayerTeam,
                                    in cameraFrameVec2);
                            cameraFrame.origin.AsVec2 = boundaryPosition;
                        }
                    }
                }
                float heightAtPosition = Mission.Scene.GetGroundHeightAtPosition(cameraFrame.origin + new Vec3(0.0f, 0.0f, 100f));
                if (!MissionScreen.IsCheatGhostMode && !_config.IgnoreTerrain && heightAtPosition < 9999.0)
                    cameraFrame.origin.z = Math.Max(cameraFrame.origin.z, heightAtPosition + 0.5f);
                if (cameraFrame.origin.z > heightAtPosition + 80.0)
                    cameraFrame.origin.z = heightAtPosition + 80f;
                if (cameraFrame.origin.z < -100.0)
                    cameraFrame.origin.z = -100f;
            }

            UpdateCameraFrameAndDof(cameraFrame);
        }

        private Vec3 GetCameraLimitCalculationEndpoint()
        {
            if (Mission.MainAgent == null)
                return Vec3.Invalid;
            return Utility.GetCameraTargetPositionWhenLockedToAgent(MissionScreen, Mission.MainAgent) + Vec3.Up;
        }

        private void LimitCameraDistance(ref MatrixFrame cameraFrame, float dt, float speed)
        {
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.IncreaseCameraDistanceLimit).IsKeyDownInOrder())
            {
                RTSCameraSkillBehavior.UpdateCameraDistanceLimit(RTSCameraSkillBehavior.CameraDistanceLimit + dt * speed);
            }
            if (RTSCameraGameKeyCategory.GetKey(GameKeyEnum.DecreaseCameraDistanceLimit).IsKeyDownInOrder())
            {
                RTSCameraSkillBehavior.UpdateCameraDistanceLimit(RTSCameraSkillBehavior.CameraDistanceLimit - dt * speed);
            }
            var mainAgentPosition = GetCameraLimitCalculationEndpoint();
            if (!mainAgentPosition.IsValid)
                return;
            var distanceLimit = RTSCameraSkillBehavior.CameraDistanceLimit;
            var heightDiff = cameraFrame.origin.z - mainAgentPosition.z;
            Vec3 targetPosition = cameraFrame.origin;
            if (heightDiff >= distanceLimit * _cameraDistanceLimitVerticalScale)
            {
                targetPosition.z = distanceLimit * _cameraDistanceLimitVerticalScale + mainAgentPosition.z;
            }
            var centerPosition = mainAgentPosition.AsVec2.ToVec3(targetPosition.z);
            var distance = targetPosition.Distance(centerPosition);
            if (distance > distanceLimit)
            {
                targetPosition = Vec3.Lerp(centerPosition, targetPosition,
                    distanceLimit / MathF.Max(distance, 1f));
            }

            cameraFrame.origin = Vec3.Lerp(targetPosition, cameraFrame.origin, MathF.Pow(0.02f, dt));
        }

        private void UpdateCameraFrameAndDof(MatrixFrame matrixFrame)
        {
            CameraPosition = matrixFrame.origin;
            CombatCamera.Frame = matrixFrame;
            UpdateDof();
            UpdateViewAngle();
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
                    Mission.Scene.RayCastForClosestEntityOrTerrain(CombatCamera.Position, CombatCamera.Position + CombatCamera.Direction * 3000f, out var terrainCollisionDistance, out WeakGameEntity _, 0.5f, BodyFlags.CameraCollisionRayCastExludeFlags | BodyFlags.DontCollideWithCamera);
                    Mission.RayCastForClosestAgent(CameraPosition,
                        CombatCamera.Position + CombatCamera.Direction * 3000f, -1, 0.1f, out var agentCollisionDistance);
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
            MissionScreen.SceneView?.SetCamera(CombatCamera);
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
                if (!MissionScreen.SceneLayer.Input.GetIsMouseActive())
                {
                    double num3 = dt / 0.000600000028498471;
                    inputXRaw += (float)num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisX");
                    if (!MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
                    {
                        inputYRaw += (float)-num3 * MissionScreen.SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
                    }
                }
                else if (!MissionScreen.MouseVisible)
                {
                    inputXRaw = MissionScreen.SceneLayer.Input.GetMouseMoveX();
                    inputYRaw = MissionScreen.SceneLayer.Input.GetMouseMoveY();
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
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveForward).IsKeyDown() ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveBackward).IsKeyDown() ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveLeft).IsKeyDown() ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveRight).IsKeyDown() ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveUp).IsKeyDown() ||
                                RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveDown).IsKeyDown() ||
                                Input.GetIsControllerConnected() &&
                                (Input.GetKeyState(InputKey.ControllerLStick).y != 0.0 ||
                                 Input.GetKeyState(InputKey.ControllerLStick).x != 0.0)))
            {
                LeaveFromAgent();
            }
        }


        public bool RequestCameraGoTo(Vec3 position, Vec3 direction = new Vec3())
        {
            if (!_freeCameraLogic.IsSpectatorCamera)
            {
                _freeCameraLogic.SwitchCamera();
                _forceMove = false;
            }
            else if (LockToAgent)
            {
                LeaveFromAgent();
            }

            //Utility.SmoothMoveToPositionAndDirection(MissionScreen, position,
            //    MathF.Asin(Vec3.DotProduct(direction, Vec3.Up)), direction.AsVec2.AngleBetween(Vec2.Forward),
            //    direction.Length > 0.5, direction.Length > 0.5);
            BeginForcedMove(position - MissionScreen.CombatCamera.Position);

            return true;
        }

        public bool RequestCameraGoTo(Vec2 position, Vec2 direction = new Vec2())
        {
            if (!_freeCameraLogic.IsSpectatorCamera)
            {
                _freeCameraLogic.SwitchCamera();
                _forceMove = false;
            }
            else if (LockToAgent)
            {
                LeaveFromAgent();
            }

            //Utility.SmoothMoveToPositionAndDirection(MissionScreen,
            //    position.ToVec3(MissionScreen.CombatCamera.Position.Z), 0, direction.AngleBetween(Vec2.Forward), false,
            //    direction.Length > 0.5);

            BeginForcedMove(position.ToVec3(MissionScreen.CombatCamera.Position.Z) - MissionScreen.CombatCamera.Position);
            return true;
        }
    }
}
