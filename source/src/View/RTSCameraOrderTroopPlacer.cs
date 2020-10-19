using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RTSCamera.Config;
using RTSCamera.Event;
using RTSCamera.Logic;
using RTSCamera.QuerySystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.View
{
    public class RTSCameraOrderTroopPlacer : MissionView
    {
        private FormationColorMissionView _contourView;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private void RegisterReload()
        {
            MissionEvent.PostSwitchTeam += OnPostSwitchTeam;
        }
        private void OnPostSwitchTeam()
        {
            InitializeInADisgustingManner();
        }
        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            RegisterReload();
            _contourView = Mission.GetMissionBehaviour<FormationColorMissionView>();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            MissionEvent.PostSwitchTeam -= OnPostSwitchTeam;
        }

        private CursorState _currentCursorState = CursorState.Invisible;
        private UiQueryData<CursorState> _cachedCursorState;
        private bool _suspendTroopPlacer;
        private bool _isMouseDown;
        private List<GameEntity> _orderPositionEntities;
        private List<GameEntity> _orderRotationEntities;
        private bool _formationDrawingMode;
        private Formation _mouseOverFormation;
        private Formation _clickedFormation;
        private Vec2 _lastMousePosition;
        private Vec2 _deltaMousePosition;
        private int _mouseOverDirection;
        private WorldPosition? _formationDrawingStartingPosition;
        private Vec2? _formationDrawingStartingPointOfMouse;
        private float? _formationDrawingStartingTime;
        private OrderController PlayerOrderController;
        private Team PlayerTeam;
        public bool Initialized;
        private Timer formationDrawTimer;
        public bool IsDrawingForced;
        public bool IsDrawingFacing;
        public bool IsDrawingForming;
        public bool IsDrawingAttaching;
        private bool _wasDrawingForced;
        private bool _wasDrawingFacing;
        private bool _wasDrawingForming;
        private GameEntity attachArrow;
        private float attachArrowLength;
        private GameEntity widthEntityLeft;
        private GameEntity widthEntityRight;
        private bool isDrawnThisFrame;
        private bool wasDrawnPreviousFrame;
        private static Material _meshMaterial;

        public bool SuspendTroopPlacer
        {
            get => _suspendTroopPlacer;
            set
            {
                _suspendTroopPlacer = value;
                if (value)
                    HideOrderPositionEntities();
                else
                    _formationDrawingStartingPosition = new WorldPosition?();
                Reset();
            }
        }

        public Formation AttachTarget { get; private set; }

        public MovementOrder.Side AttachSide { get; private set; }

        public WorldPosition AttachPosition { get; private set; }

        public override void AfterStart()
        {
            base.AfterStart();
            _formationDrawingStartingPosition = new WorldPosition?();
            _formationDrawingStartingPointOfMouse = new Vec2?();
            _formationDrawingStartingTime = new float?();
            _orderRotationEntities = new List<GameEntity>();
            _orderPositionEntities = new List<GameEntity>();
            formationDrawTimer = new Timer(MBCommon.GetTime(MBCommon.TimeType.Application), 0.03333334f);
            attachArrow = GameEntity.CreateEmpty(Mission.Scene);
            attachArrow.AddComponent(MetaMesh.GetCopy("order_arrow_a"));
            attachArrow.SetVisibilityExcludeParents(false);
            BoundingBox boundingBox = attachArrow.GetMetaMesh(0).GetBoundingBox();
            attachArrowLength = boundingBox.max.y - boundingBox.min.y;
            widthEntityLeft = GameEntity.CreateEmpty(Mission.Scene);
            widthEntityLeft.AddComponent(MetaMesh.GetCopy("order_arrow_a"));
            widthEntityLeft.SetVisibilityExcludeParents(false);
            widthEntityRight = GameEntity.CreateEmpty(Mission.Scene);
            widthEntityRight.AddComponent(MetaMesh.GetCopy("order_arrow_a"));
            widthEntityRight.SetVisibilityExcludeParents(false);
        }

        private void InitializeInADisgustingManner()
        {
            PlayerTeam = Mission.PlayerTeam;
            PlayerOrderController = PlayerTeam.PlayerOrderController;
            _cachedCursorState = new UiQueryData<CursorState>(GetCursorState, 0.05f);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Initialized)
                return;
            MissionPeer missionPeer = GameNetwork.IsMyPeerReady
                ? GameNetwork.MyPeer.GetComponent<MissionPeer>()
                : null;
            if (Mission.PlayerTeam == null && (missionPeer == null ||
                                                    missionPeer.Team != Mission.AttackerTeam &&
                                                    missionPeer.Team != Mission.DefenderTeam))
                return;
            InitializeInADisgustingManner();
            Initialized = true;
        }

        public void UpdateAttachVisuals(bool isVisible)
        {
            if (AttachTarget == null)
                isVisible = false;
            attachArrow.SetVisibilityExcludeParents(isVisible);
            if (isVisible)
            {
                Vec2 vec2 = AttachTarget.Direction;
                switch (AttachSide)
                {
                    case MovementOrder.Side.Front:
                        vec2 *= -1f;
                        break;
                    case MovementOrder.Side.Left:
                        vec2 = vec2.RightVec();
                        break;
                    case MovementOrder.Side.Right:
                        vec2 = vec2.LeftVec();
                        break;
                }

                float rotationInRadians = vec2.RotationInRadians;
                Mat3 identity1 = Mat3.Identity;
                identity1.RotateAboutUp(rotationInRadians);
                MatrixFrame identity2 = MatrixFrame.Identity;
                identity2.rotation = identity1;
                identity2.origin = AttachPosition.GetGroundVec3();
                identity2.Advance(-attachArrowLength);
                attachArrow.SetFrame(ref identity2);
            }

            if (!isVisible)
                return;
            MissionScreen.GetOrderFlagPosition();
            AddAttachPoints();
        }

        private void UpdateFormationDrawingForFacingOrder(bool giveOrder)
        {
            isDrawnThisFrame = true;
            PlayerOrderController.SimulateNewFacingOrder(
                OrderController.GetOrderLookAtDirection(PlayerOrderController.SelectedFormations,
                    MissionScreen.GetOrderFlagPosition().AsVec2), out var simulationAgentFrames);
            int entityIndex = 0;
            HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForDestination(bool giveOrder)
        {
            isDrawnThisFrame = true;
            PlayerOrderController.SimulateDestinationFrames(out var simulationAgentFrames);
            int entityIndex = 0;
            HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, 0.7f);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForFormingOrder(bool giveOrder)
        {
            isDrawnThisFrame = true;
            MatrixFrame orderFlagFrame = MissionScreen.GetOrderFlagFrame();
            Vec3 origin1 = orderFlagFrame.origin;
            Vec2 asVec2 = orderFlagFrame.rotation.f.AsVec2;
            float orderFormCustomWidth =
                OrderController.GetOrderFormCustomWidth(PlayerOrderController.SelectedFormations, origin1);
            PlayerOrderController.SimulateNewCustomWidthOrder(orderFormCustomWidth, out var simulationAgentFrames);
            Formation formation =
                PlayerOrderController.SelectedFormations.MaxBy(
                    f => f.CountOfUnits);
            int entityIndex = 0;
            HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame1) in simulationAgentFrames)
            {
                var worldFrame = frame1;
                AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder);
                ++entityIndex;
            }

            float unitDiameter = formation.UnitDiameter;
            float interval = formation.Interval;
            int num1 = Math.Max(0,
                (int)((orderFormCustomWidth - (double)unitDiameter) /
                    (interval + (double)unitDiameter) + 9.99999974737875E-06)) + 1;
            float num2 = (num1 - 1) * (interval + unitDiameter);
            for (int index = 0; index < num1; ++index)
            {
                Vec2 a = new Vec2(
                    (float)(index * (interval + (double)unitDiameter) - num2 / 2.0), 0.0f);
                Vec2 parentUnitF = asVec2.TransformToParentUnitF(a);
                WorldPosition origin2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, origin1, false);
                origin2.SetVec2(origin2.AsVec2 + parentUnitF);
                WorldFrame frame2 = new WorldFrame(orderFlagFrame.rotation, origin2);
                AddOrderPositionEntity(entityIndex++, ref frame2, false);
            }
        }

        private bool IsDraggingFormation()
        {
            if (_formationDrawingStartingPointOfMouse.HasValue)
            {
                Vec2 vec2 = _formationDrawingStartingPointOfMouse.Value - Input.GetMousePositionPixel();
                if (Math.Abs(vec2.x) >= 10.0 || Math.Abs(vec2.y) >= 10.0)
                {
                    return true;
                }
            }

            if (_formationDrawingStartingTime.HasValue &&
                MBCommon.GetTime(MBCommon.TimeType.Application) -
                _formationDrawingStartingTime.Value >= 0.300000011920929)
            {
                return true;
            }

            return false;
        }

        private void UpdateFormationDrawing(bool giveOrder)
        {
            isDrawnThisFrame = true;
            HideOrderPositionEntities();
            if (!_formationDrawingStartingPosition.HasValue)
                return;
            WorldPosition formationRealEndingPosition;

            if (!IsDraggingFormation())
            {
                formationRealEndingPosition = _formationDrawingStartingPosition.Value;
            }
            else
            {
                Vec3 rayBegin;
                Vec3 rayEnd;
                MissionScreen.ScreenPointToWorldRay(GetScreenPoint(), out rayBegin, out rayEnd);
                float collisionDistance;
                if (!Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance, 0.3f))
                    return;
                Vec3 vec3 = rayEnd - rayBegin;
                double num = vec3.Normalize();
                formationRealEndingPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero,
                    rayBegin + vec3 * collisionDistance, false);
            }

            WorldPosition worldPosition;
            if (_mouseOverDirection == 1)
            {
                worldPosition = formationRealEndingPosition;
                formationRealEndingPosition = _formationDrawingStartingPosition.Value;
            }
            else
                worldPosition = _formationDrawingStartingPosition.Value;
            if (!OrderFlag.IsPositionOnValidGround(worldPosition))
                return;
            bool isFormationLayoutVertical = !Input.IsControlDown();
            if ((!InputKey.LeftMouseButton.IsDown() || _formationDrawingStartingPointOfMouse.HasValue) &&
                IsDrawingAttaching)
                UpdateFormationDrawingForAttachOrder(giveOrder, isFormationLayoutVertical);
            else if (true)
                UpdateFormationDrawingForMovementOrder(giveOrder, worldPosition, formationRealEndingPosition,
                    isFormationLayoutVertical);
            _deltaMousePosition *= Math.Max((float)(1.0 - (Input.GetMousePositionRanged() - _lastMousePosition).Length * 10.0), 0.0f);
            _lastMousePosition = Input.GetMousePositionRanged();
        }

        private void UpdateFormationDrawingForMovementOrder(
            bool giveOrder,
            WorldPosition formationRealStartingPosition,
            WorldPosition formationRealEndingPosition,
            bool isFormationLayoutVertical)
        {
            isDrawnThisFrame = true;
            PlayerOrderController.SimulateNewOrderWithPositionAndDirection(formationRealStartingPosition,
                formationRealEndingPosition, out var simulationAgentFrames, isFormationLayoutVertical);
            if (giveOrder)
            {
                if (!isFormationLayoutVertical)
                    PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegmentWithHorizontalLayout,
                        formationRealStartingPosition, formationRealEndingPosition);
                else
                    PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment,
                        formationRealStartingPosition, formationRealEndingPosition);
            }

            int entityIndex = 0;
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForAttachOrder(
            bool giveOrder,
            bool isFormationLayoutVertical)
        {
            isDrawnThisFrame = true;
            int entityIndex = 0;
            foreach (Formation selectedFormation in PlayerOrderController.SelectedFormations)
            {
                WorldPosition attachPosition =
                    MovementOrder.GetAttachPosition(selectedFormation, AttachTarget, AttachSide);
                Vec2 vec2 = AttachTarget.Direction.LeftVec() * (selectedFormation.Width / 2f);
                WorldPosition formationLineBegin = attachPosition;
                formationLineBegin.SetVec2(formationLineBegin.AsVec2 + vec2);
                WorldPosition formationLineEnd = attachPosition;
                formationLineEnd.SetVec2(formationLineEnd.AsVec2 - vec2);
                OrderController.SimulateNewOrderWithPositionAndDirection(
                    Enumerable.Repeat(selectedFormation, 1), PlayerOrderController.simulationFormations,
                    formationLineBegin, formationLineEnd, out var simulationAgentFrames, isFormationLayoutVertical);
                foreach ((Agent _, WorldFrame frame2) in simulationAgentFrames)
                {
                    var worldFrame = frame2;
                    AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder);
                    ++entityIndex;
                }
            }

            if (!giveOrder)
                return;
            PlayerOrderController.SetOrderWithFormationAndNumber(OrderType.Attach, AttachTarget,
                (int)AttachSide);
        }

        private void BeginFormationDraggingOrClicking()
        {
            Vec3 rayBegin;
            Vec3 rayEnd;
            MissionScreen.ScreenPointToWorldRay(GetScreenPoint(), out rayBegin, out rayEnd);
            float collisionDistance;
            if (Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance,
                0.3f))
            {
                Vec3 vec3 = rayEnd - rayBegin;
                double num = vec3.Normalize();
                _formationDrawingStartingPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, rayBegin + vec3 * collisionDistance,
                    false);
                _formationDrawingStartingPointOfMouse = Input.GetMousePositionPixel();
                _formationDrawingStartingTime = MBCommon.GetTime(MBCommon.TimeType.Application);
                return;
            }

            _formationDrawingStartingPosition = new WorldPosition?();
            _formationDrawingStartingPointOfMouse = new Vec2?();
            _formationDrawingStartingTime = new float?();
        }

        private void HandleMousePressed()
        {
            if (PlayerOrderController.SelectedFormations.IsEmpty() || _clickedFormation != null)
                return;
            switch (_currentCursorState)
            {
                case CursorState.Enemy:
                    if (_config.AttackSpecificFormation && Input.IsKeyDown(InputKey.MiddleMouseButton))
                    {
                        _clickedFormation = _mouseOverFormation;
                    }
                    else
                    {
                        _formationDrawingMode = true;
                    }
                    BeginFormationDraggingOrClicking();
                    break;
                case CursorState.Friend:
                    if (_config.ClickToSelectFormation && Input.IsKeyDown(InputKey.MiddleMouseButton))
                    {
                        if (_mouseOverFormation != null && PlayerOrderController.IsFormationSelectable(_mouseOverFormation))
                        {
                            _clickedFormation = _mouseOverFormation;
                        }
                    }
                    else
                    {
                        _formationDrawingMode = true;
                    }
                    BeginFormationDraggingOrClicking();
                    break;
                case CursorState.Normal:
                    if (_config.ShouldHighlightWithOutline() && Input.IsKeyDown(InputKey.MiddleMouseButton))
                        return;
                    _formationDrawingMode = true;
                    BeginFormationDraggingOrClicking();
                    break;
                case CursorState.Rotation:
                    if (_mouseOverFormation.CountOfUnits <= 0)
                        break;
                    HideNonSelectedOrderRotationEntities(_mouseOverFormation);
                    PlayerOrderController.ClearSelectedFormations();
                    PlayerOrderController.SelectFormation(_mouseOverFormation);
                    _formationDrawingMode = true;
                    WorldPosition orderPosition = _mouseOverFormation.OrderPosition;
                    Vec2 direction = _mouseOverFormation.Direction;
                    direction.RotateCCW(-1.570796f);
                    _formationDrawingStartingPosition = orderPosition;
                    _formationDrawingStartingPosition.Value.SetVec2(
                        _formationDrawingStartingPosition.Value.AsVec2 + direction *
                        (_mouseOverDirection == 1 ? 0.5f : -0.5f) * _mouseOverFormation.Width);
                    WorldPosition worldPosition = orderPosition;
                    worldPosition.SetVec2(worldPosition.AsVec2 + direction *
                        (_mouseOverDirection == 1 ? -0.5f : 0.5f) *
                        _mouseOverFormation.Width);
                    _deltaMousePosition =
                        MissionScreen.SceneView.WorldPointToScreenPoint(worldPosition.GetGroundVec3()) -
                        GetScreenPoint();
                    _lastMousePosition = Input.GetMousePositionRanged();
                    break;
            }
        }

        private void TryTransformFromClickingToDragging()
        {
            if (PlayerOrderController.SelectedFormations.IsEmpty())
                return;
            switch (_currentCursorState)
            {
                case CursorState.Enemy:
                case CursorState.Friend:
                    if (IsDraggingFormation())
                    {
                        _formationDrawingMode = true;
                        _clickedFormation = null;
                    }

                    break;
            }
        }

        private void HandleMouseUp()
        {
            var cursorState = _currentCursorState;
            if (_clickedFormation != null && Input.IsKeyDown(InputKey.MiddleMouseButton))
            {
                if (_clickedFormation.CountOfUnits > 0)
                {
                    bool isEnemy = Utility.IsEnemy(_clickedFormation);
                    if (!isEnemy)
                    {
                        HideNonSelectedOrderRotationEntities(_clickedFormation);

                        if (PlayerOrderController.IsFormationSelectable(_clickedFormation))
                        {
                            if (!Input.IsControlDown())
                            {
                                PlayerOrderController.ClearSelectedFormations();
                                PlayerOrderController.SelectFormation(_clickedFormation);
                            }
                            else if (PlayerOrderController.IsFormationListening(_clickedFormation))
                            {
                                PlayerOrderController.DeselectFormation(_clickedFormation);
                            }
                            else
                            {
                                PlayerOrderController.SelectFormation(_clickedFormation);
                            }
                        }
                    }
                    else if (_config.AttackSpecificFormation)
                    {
                        PlayerOrderController.SetOrderWithFormation(OrderType.ChargeWithTarget, _clickedFormation);
                    }
                }

                _clickedFormation = null;
            }
            else if (cursorState == CursorState.Ground)
            {
                if (IsDrawingFacing || _wasDrawingFacing)
                    UpdateFormationDrawingForFacingOrder(true);
                else if (IsDrawingForming || _wasDrawingForming)
                    UpdateFormationDrawingForFormingOrder(true);
                else
                    UpdateFormationDrawing(true);
                if (IsDeployment)
                    SoundEvent.PlaySound2D("event:/ui/mission/deploy");
            }

            _formationDrawingMode = false;
            _formationDrawingStartingPosition = null;
            _formationDrawingStartingPointOfMouse = null;
            _formationDrawingStartingTime = null;
            _deltaMousePosition = Vec2.Zero;
        }

        private Vec2 GetScreenPoint()
        {
            return !MissionScreen.MouseVisible
                ? new Vec2(0.5f, 0.5f) + _deltaMousePosition
                : Input.GetMousePositionRanged() + _deltaMousePosition;
        }

        private CursorState GetCursorState()
        {
            CursorState cursorState = CursorState.Invisible;
            AttachTarget = null;
            if (!PlayerOrderController.SelectedFormations.IsEmpty() && _clickedFormation == null)
            {
                MissionScreen.ScreenPointToWorldRay(GetScreenPoint(), out var rayBegin, out var rayEnd);
                if (!Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out var collisionDistance,
                    out var collidedEntity, 0.3f))
                    collisionDistance = 1000f;
                if (cursorState == CursorState.Invisible && collisionDistance < 1000.0)
                {
                    if (!_formationDrawingMode && collidedEntity == null)
                    {
                        for (int index = 0; index < _orderRotationEntities.Count; ++index)
                        {
                            GameEntity orderRotationEntity = _orderRotationEntities[index];
                            if (orderRotationEntity.IsVisibleIncludeParents() &&
                                collidedEntity == orderRotationEntity)
                            {
                                _mouseOverFormation =
                                    PlayerOrderController.SelectedFormations.ElementAt(index / 2);
                                _mouseOverDirection = 1 - (index & 1);
                                cursorState = CursorState.Rotation;
                                break;
                            }
                        }
                    }

                    if (cursorState == CursorState.Invisible)
                    {
                        if (MissionScreen.OrderFlag.FocusedOrderableObject != null)
                            cursorState = CursorState.OrderableEntity;
                        else if (_config.ShouldHighlightWithOutline())
                        {
                            var formation = GetMouseOverFormation(collisionDistance);
                            _mouseOverFormation = formation;
                            if (formation != null)
                            {
                                if (formation.Team.IsEnemyOf(Mission.PlayerTeam))
                                {
                                    if (_config.AttackSpecificFormation)
                                    {
                                        cursorState = CursorState.Enemy;
                                    }
                                }
                                else
                                {
                                    if (_config.ClickToSelectFormation)
                                    {
                                        cursorState = CursorState.Friend;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (_clickedFormation != null) // click on formation and hold.
            {
                cursorState = _currentCursorState;
            }
            if (cursorState == CursorState.Invisible &&
                !(Input.IsKeyDown(InputKey.MiddleMouseButton) && _config.ShouldHighlightWithOutline()) || // press middle mouse button to avoid accidentally click on ground.
                _formationDrawingMode)
            {
                cursorState = IsCursorStateGroundOrNormal();
                UpdateAttachData();
            }

            if (cursorState != CursorState.Ground &&
                cursorState != CursorState.Rotation)
                _mouseOverDirection = 0;
            return cursorState;
        }

        private CursorState IsCursorStateGroundOrNormal()
        {
            return !_formationDrawingMode
                ? CursorState.Normal
                : CursorState.Ground;
        }

        private void UpdateAttachData()
        {
            if (!IsDrawingForced)
                return;
            Vec3 orderFlagPosition = MissionScreen.GetOrderFlagPosition();
            foreach (Formation formation in PlayerTeam.Formations.Where(
                f => !PlayerOrderController.IsFormationListening(f)))
            {
                WorldPosition worldPosition;
                Vec2 asVec2;
                if (AttachTarget != null)
                {
                    worldPosition = formation.RearAttachmentPoint;
                    asVec2 = worldPosition.AsVec2;
                    double num1 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                    worldPosition = AttachPosition;
                    asVec2 = worldPosition.AsVec2;
                    double num2 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                    if (num1 >= num2)
                        goto label_7;
                }

                AttachTarget = formation;
                AttachSide = MovementOrder.Side.Rear;
                AttachPosition = formation.RearAttachmentPoint;
            label_7:
                worldPosition = formation.LeftAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num3 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num4 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num3 < num4)
                {
                    AttachTarget = formation;
                    AttachSide = MovementOrder.Side.Left;
                    AttachPosition = formation.LeftAttachmentPoint;
                }

                worldPosition = formation.RightAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num5 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num6 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num5 < num6)
                {
                    AttachTarget = formation;
                    AttachSide = MovementOrder.Side.Right;
                    AttachPosition = formation.RightAttachmentPoint;
                }

                worldPosition = formation.FrontAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num7 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num8 = asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num7 < num8)
                {
                    AttachTarget = formation;
                    AttachSide = MovementOrder.Side.Front;
                    AttachPosition = formation.FrontAttachmentPoint;
                }
            }
        }

        private void AddOrderPositionEntity(
            int entityIndex,
            ref WorldFrame frame,
            bool fadeOut,
            float alpha = -1f)
        {
            while (_orderPositionEntities.Count <= entityIndex)
            {
                GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_flag_small");
                if (_meshMaterial == null)
                {
                    _meshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    _meshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }

                copy.SetMaterial(_meshMaterial);
                copy.SetContourColor(new Color(0, 0.6f, 1).ToUnsignedInteger());
                copy.SetContourState(true);
                empty.AddComponent(copy);
                empty.SetVisibilityExcludeParents(false);
                _orderPositionEntities.Add(empty);
            }

            GameEntity orderPositionEntity = _orderPositionEntities[entityIndex];
            Vec3 rayBegin;
            MissionScreen.ScreenPointToWorldRay(Vec2.One * 0.5f, out rayBegin, out Vec3 _);
            float rotationZ = MatrixFrame.CreateLookAt(rayBegin, frame.Origin.GetGroundVec3(), Vec3.Up).rotation.f
                .RotationZ;
            frame.Rotation = Mat3.Identity;
            frame.Rotation.RotateAboutUp(rotationZ);
            MatrixFrame groundMatrixFrame = frame.ToGroundMatrixFrame();
            orderPositionEntity.SetFrame(ref groundMatrixFrame);
            if (alpha != -1.0)
            {
                orderPositionEntity.SetVisibilityExcludeParents(true);
                orderPositionEntity.SetAlpha(alpha);
            }
            else if (fadeOut)
                orderPositionEntity.FadeOut(0.3f, false);
            else
                orderPositionEntity.FadeIn();
        }

        private void HideNonSelectedOrderRotationEntities(Formation formation)
        {
            for (int index = 0; index < _orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = _orderRotationEntities[index];
                if (orderRotationEntity == null &&
                    orderRotationEntity.IsVisibleIncludeParents() &&
                    PlayerOrderController.SelectedFormations.ElementAt(index / 2) != formation)
                {
                    orderRotationEntity.SetVisibilityExcludeParents(false);
                    orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
                }
            }
        }

        private void HideOrderPositionEntities()
        {
            if (MissionState.Current.Paused)
            {
                foreach (GameEntity orderPositionEntity in _orderPositionEntities)
                {
                    orderPositionEntity.FadeIn();
                    orderPositionEntity.HideIfNotFadingOut();
                }
            }
            else
            {
                foreach (GameEntity orderPositionEntity in _orderPositionEntities)
                    orderPositionEntity.HideIfNotFadingOut();
            }
            for (int index = 0; index < _orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = _orderRotationEntities[index];
                orderRotationEntity.SetVisibilityExcludeParents(false);
                orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
            }
        }

        [Conditional("DEBUG")]
        private void DebugTick(float dt)
        {
            int num = Initialized ? 1 : 0;
        }

        private void Reset()
        {
            _isMouseDown = false;
            _formationDrawingMode = false;
            _formationDrawingStartingPosition = new WorldPosition?();
            _formationDrawingStartingPointOfMouse = new Vec2?();
            _formationDrawingStartingTime = new float?();
            _mouseOverFormation = null;
            _clickedFormation = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (!Initialized)
                return;
            base.OnMissionScreenTick(dt);
            if (!PlayerOrderController.SelectedFormations.Any())
                return;
            isDrawnThisFrame = false;
            if (SuspendTroopPlacer)
                return;

            _currentCursorState = _cachedCursorState.Value;
            //Utility.DisplayMessage(_currentCursorState.ToString());
            // use middle mouse button to select formation
            if (Input.IsKeyPressed(InputKey.LeftMouseButton) || _config.ShouldHighlightWithOutline() && Input.IsKeyPressed(InputKey.MiddleMouseButton))
            {
                _isMouseDown = true;
                HandleMousePressed();
                //Utility.DisplayMessage("key pressed");
            }

            if ((Input.IsKeyReleased(InputKey.LeftMouseButton) ||
                 _config.ShouldHighlightWithOutline() && Input.IsKeyPressed(InputKey.MiddleMouseButton) &&
                 !_formationDrawingMode) && _isMouseDown)
            {
                _isMouseDown = false;
                HandleMouseUp();
                //Utility.DisplayMessage("key up");
            }
            else if (Input.IsKeyDown(InputKey.LeftMouseButton) && _isMouseDown)
            {
                //Utility.DisplayMessage("key down");
                if (formationDrawTimer.Check(MBCommon.GetTime(MBCommon.TimeType.Application)) &&
                    !IsDrawingFacing &&
                    !IsDrawingForming)
                {
                    //Utility.DisplayMessage("try transform");
                    TryTransformFromClickingToDragging();
                    if (_currentCursorState == CursorState.Ground)
                        UpdateFormationDrawing(false);
                }
            }
            else if (IsDrawingForced)
            {
                //Utility.DisplayMessage("drawing forced");
                Reset();
                _formationDrawingMode = true;
                BeginFormationDraggingOrClicking();
                //HandleMousePressed();
                UpdateFormationDrawing(false);
            }
            else if (IsDrawingFacing || _wasDrawingFacing)
            {
                if (IsDrawingFacing)
                {
                    Reset();
                    UpdateFormationDrawingForFacingOrder(false);
                }
            }
            else if (IsDrawingForming || _wasDrawingForming)
            {
                if (IsDrawingForming)
                {
                    Reset();
                    UpdateFormationDrawingForFormingOrder(false);
                }
            }
            else if (_wasDrawingForced)
                Reset();
            else
            {
                UpdateFormationDrawingForDestination(false);
            }
            UpdateInputForContour();



            foreach (GameEntity orderPositionEntity in _orderPositionEntities)
                orderPositionEntity.SetPreviousFrameInvalid();
            foreach (GameEntity orderRotationEntity in _orderRotationEntities)
                orderRotationEntity.SetPreviousFrameInvalid();
            _wasDrawingForced = IsDrawingForced;
            _wasDrawingFacing = IsDrawingFacing;
            _wasDrawingForming = IsDrawingForming;
            wasDrawnPreviousFrame = isDrawnThisFrame;
        }

        private void UpdateInputForContour()
        {
            _contourView?.MouseOver(_mouseOverFormation);
        }

        private Agent RayCastForAgent(float distance)
        {
            MissionScreen.ScreenPointToWorldRay(GetScreenPoint(), out var rayBegin, out var rayEnd);
            var agent = Mission.RayCastForClosestAgent(rayBegin, rayEnd, out var agentDistance,
                MissionScreen.LastFollowedAgent?.Index ?? -1, 0.5f);
            return agentDistance > distance ? null : agent;
        }

        private Formation GetMouseOverFormation(float collisionDistance)
        {
            var agent = RayCastForAgent(collisionDistance);
            if (agent != null && agent.IsMount)
                agent = agent.RiderAgent;
            if (agent == null)
                return null;
            if (_config.ShouldHighlightWithOutline() && !IsDrawingForced && !_formationDrawingMode && agent?.Formation != null &&
                !(PlayerOrderController.SelectedFormations.Count == 1 &&
                  PlayerOrderController.SelectedFormations.Contains(agent.Formation)))
            {
                return agent.Formation;
            }

            return null;
        }

        private bool IsDeployment => Mission.GetMissionBehaviour<SiegeDeploymentHandler>() != null;

        private void AddAttachPoints()
        {
            foreach (Formation formation in PlayerTeam.FormationsIncludingSpecial.Except(
                PlayerOrderController.SelectedFormations))
            {
                WorldPosition rearAttachmentPoint = formation.RearAttachmentPoint;
                WorldPosition frontAttachmentPoint = formation.FrontAttachmentPoint;
                WorldPosition leftAttachmentPoint = formation.LeftAttachmentPoint;
                WorldPosition rightAttachmentPoint = formation.RightAttachmentPoint;
            }

            if (AttachTarget == null)
                return;
            WorldPosition attachPosition = AttachPosition;
        }
        protected enum CursorState
        {
            Invisible,
            Normal,
            Ground,
            Enemy,
            Friend,
            Rotation,
            Count,
            OrderableEntity
        }
    }
}
