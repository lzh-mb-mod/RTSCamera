using HarmonyLib;
using MissionLibrary.Event;
using MissionSharedLibrary.QuerySystem;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.CampaignGame;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Logic.SubLogic;
using RTSCamera.CommandSystem.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using CursorState = TaleWorlds.MountAndBlade.View.MissionViews.Order.OrderTroopPlacer.CursorState;

namespace RTSCamera.CommandSystem.Patch
{
    public enum CurrentCursorState
    {
        Invisible,
        Normal,
        Ground,
        Rotation,
        Count,
        OrderableEntity,
        Friend,
        Enemy,
    }
    public class Patch_OrderTroopPlacer
    {
        public static uint OrderPositionEntityColor = new Color(0.15f, 0.65f, 0.15f).ToUnsignedInteger();
        public static float OrderPositionEntityPreviewAlpha = 1f;
        public static float OrderPositionEntityDestinationAlpha = 0.5f;
        private static float _cachedTimeOfDay = 0;
        private static bool _patched;

        private static readonly FieldInfo _dataSource =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo _activeCursorState = typeof(OrderTroopPlacer).GetProperty("ActiveCursorState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _cursorState = typeof(OrderTroopPlacer).GetMethod("GetCursorState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _handleMouseDown = typeof(OrderTroopPlacer).GetMethod("HandleMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _handleMouseUp = typeof(OrderTroopPlacer).GetMethod("HandleMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForFacingOrder = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFacingOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForFormingOrder = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFormingOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForDestination = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForDestination", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _getGroundVec3 = typeof(OrderTroopPlacer).GetMethod("GetGroundedVec3", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _addOrderPositionEntity = typeof(OrderTroopPlacer).GetMethod("AddOrderPositionEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _reset = typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _isInitialized = false;
        private static CurrentCursorState _currentCursorState = CurrentCursorState.Invisible;
        private static Formation _clickedFormation = null;
        private static UiQueryData<CurrentCursorState> _cachedCursorState;
        private static FormationColorSubLogicV2 _outlineView;
        private static FormationColorSubLogicV2 _groundMarkerView;
        private static OrderTroopPlacer _orderTroopPlacer;
        private static MissionFormationTargetSelectionHandler _targetSelectionHandler;
        private static MBReadOnlyList<Formation> _focusedFormationsCache;
        public static bool IsFreeCamera;
        private static MovementTargetHighlightStyle _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        private static List<GameEntity> _originalOrderPositionEntities;
        private static List<GameEntity> _newModelOrderPositionEntities;
        private static List<GameEntity> _alwaysVisibleOrderPositionEntities;
        private static Material _currentMaterial;
        private static Material _originalMaterial;
        private static Material _newModelMaterial;
        private static Material _alwaysVisibleMaterial;
        private static bool _skipDrawingForDestinationForOneTick;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("OnMissionTick",
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(
                        nameof(Postfix_OnMissionTick), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("HandleMouseDown",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_HandleMouseDown),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("GetCursorState",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_GetCursorState),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("AddOrderPositionEntity",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_AddOrderPositionEntity),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod(nameof(OrderTroopPlacer.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_OnMissionScreenTick),
                        BindingFlags.Static | BindingFlags.Public)));
                // For command queue
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForMovementOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_UpdateFormationDrawingForMovementOrder),
                    BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFacingOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_UpdateFormationDrawingForFacingOrder),
                    BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }
        }

        public static void Postfix_OnMissionTick(OrderTroopPlacer __instance, bool ____initialized)
        {
            if (!____initialized)
                return;
            if (_isInitialized)
                return;
            _isInitialized = true;
            _orderTroopPlacer = __instance;
            _cachedCursorState = new UiQueryData<CurrentCursorState>(GetCursorState, 0.05f);
            _clickedFormation = null;
            _outlineView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().OutlineColorSubLogic;
            _groundMarkerView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().GroundMarkerColorSubLogic;

            typeof(Input).GetProperty(nameof(Input.DebugInput), BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, __instance.Input);

            _cachedTimeOfDay = __instance.Mission.Scene.TimeOfDay;
        }

        public static void OnBehaviorInitialize()
        {
            _targetSelectionHandler = Mission.Current.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
            if (_targetSelectionHandler != null)
            {
                _targetSelectionHandler.OnFormationFocused += OnFormationFocused;
            }
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
            IsFreeCamera = false;
            _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        }

        public static void OnRemoveBehavior()
        {
            _cachedTimeOfDay = 0;
            _orderTroopPlacer = null;
            _isInitialized = false;
            _outlineView = null;
            _groundMarkerView = null;
            _cachedCursorState = null;
            _clickedFormation = null;
            _focusedFormationsCache = null;
            if (_targetSelectionHandler != null)
            {
                _targetSelectionHandler.OnFormationFocused -= OnFormationFocused;
            }
            _targetSelectionHandler = null;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            IsFreeCamera = false;
            _originalOrderPositionEntities = _newModelOrderPositionEntities = _alwaysVisibleOrderPositionEntities = null;
            _currentMaterial = _originalMaterial = _newModelMaterial = _alwaysVisibleMaterial = null;
            _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        }

        private static void OnFormationFocused(MBReadOnlyList<Formation> focusedFormations)
        {
            _focusedFormationsCache = focusedFormations;
        }

        private static void OnToggleFreeCamera(bool isFreeCamera)
        {
            IsFreeCamera = isFreeCamera;
            RTSCommandVisualOrder.OrderToSelectTarget = SelectTargetMode.None;
        }

        public static bool IsDraggingFormation(OrderTroopPlacer __instance, Vec2? ____formationDrawingStartingPointOfMouse, float? ____formationDrawingStartingTime)
        {
            if (____formationDrawingStartingPointOfMouse.HasValue)
            {
                Vec2 vec2 = ____formationDrawingStartingPointOfMouse.Value - __instance.Input.GetMousePositionPixel();
                if (Math.Abs(vec2.x) >= 10.0 || Math.Abs(vec2.y) >= 10.0)
                {
                    return true;
                }
            }

            //if (____formationDrawingStartingTime.HasValue &&
            //    __instance.Mission.CurrentTime -
            //    ____formationDrawingStartingTime.Value >= 0.300000011920929)
            //{
            //    return true;
            //}

            return false;
        }

        public static CurrentCursorState GetCursorState()
        {
            _cursorState.Invoke(_orderTroopPlacer, new object[] { });
            return _currentCursorState;
        }
        private static Vec2 GetScreenPoint(OrderTroopPlacer __instance, ref Vec2 ____deltaMousePosition)
        {
            return !__instance.MissionScreen.MouseVisible
                ? new Vec2(0.5f, 0.5f) + ____deltaMousePosition
                : __instance.Input.GetMousePositionRanged() + ____deltaMousePosition;
        }

        private static void BeginFormationDraggingOrClicking(OrderTroopPlacer __instance, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            if (TryGetScreenMiddleToWorldPosition(__instance, ref ____deltaMousePosition, out var worldPosition, out var _, out var _))
            {
                ____formationDrawingStartingPosition = worldPosition;
                ____formationDrawingStartingPointOfMouse = __instance.Input.GetMousePositionPixel();
                // Fix the issue that can't drag when slow motion is enabled and mouse is visible.
                ____formationDrawingStartingTime = 0;
                return;
            }

            ____formationDrawingStartingPosition = new WorldPosition?();
            ____formationDrawingStartingPointOfMouse = new Vec2?();
            ____formationDrawingStartingTime = new float?();
        }

        private static bool TryGetScreenMiddleToWorldPosition(
            OrderTroopPlacer __instance,
            ref Vec2 ____deltaMousePosition,
            out WorldPosition worldPosition,
            out float collisionDistance,
            out WeakGameEntity collidedEntity)
        {
            if (!__instance.Mission.IsNavalBattle)
            {
                Vec3 rayBegin;
                Vec3 rayEnd;
                __instance.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(__instance, ref ____deltaMousePosition), out rayBegin, out rayEnd);
                float collisionDistance1;
                WeakGameEntity collidedEntity1;
                if (__instance.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance1, out collidedEntity1, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora))
                {
                    Vec3 vec3 = rayEnd - rayBegin;
                    double num = (double)vec3.Normalize();
                    collisionDistance = collisionDistance1;
                    collidedEntity = collidedEntity1;
                    worldPosition = new WorldPosition(__instance.Mission.Scene, UIntPtr.Zero, rayBegin + vec3 * collisionDistance, false);
                    return true;
                }
                worldPosition = WorldPosition.Invalid;
                collisionDistance = 0.0f;
                collidedEntity = WeakGameEntity.Invalid;
                return false;
            }
            Vec3 waterPosition;
            if (__instance.MissionScreen.GetProjectedMousePositionOnWater(out waterPosition))
            {
                worldPosition = new WorldPosition(__instance.Mission.Scene, waterPosition);
                collisionDistance = (waterPosition - __instance.Mission.GetCameraFrame().origin).Length;
                collidedEntity = WeakGameEntity.Invalid;
                return true;
            }
            worldPosition = WorldPosition.Invalid;
            collisionDistance = 0.0f;
            collidedEntity = WeakGameEntity.Invalid;
            return false;
        }

        public static bool Prefix_HandleMouseDown(OrderTroopPlacer __instance, ref Formation ____mouseOverFormation,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime, bool ____isMouseDown)
        {
            if (__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.IsEmpty() || _clickedFormation != null)
                return false;
            switch (__instance.Mission.IsNavalBattle ? (CurrentCursorState)_activeCursorState.GetValue(__instance) : _currentCursorState)
            {
                case CurrentCursorState.Normal:
                case CurrentCursorState.Enemy:
                case CurrentCursorState.Friend:
                    ____formationDrawingMode = true;
                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
                case CurrentCursorState.Rotation:
                    return true;
            }

            return false;
        }


        private static void HideNonSelectedOrderRotationEntities(OrderController ___PlayerOrderController, List<GameEntity> ____orderRotationEntities, Formation formation)
        {
            for (int index = 0; index < ____orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = ____orderRotationEntities[index];
                if (orderRotationEntity == null && orderRotationEntity.IsVisibleIncludeParents() && ___PlayerOrderController.SelectedFormations.ElementAt(index / 2) != formation)
                {
                    orderRotationEntity.SetVisibilityExcludeParents(false);
                    orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
                }
            }
        }


        private static void TryTransformFromClickingToDragging(OrderTroopPlacer __instance, Vec2? ____formationDrawingStartingPointOfMouse, float? ____formationDrawingStartingTime, OrderController ___PlayerOrderController,
            ref Formation ____clickedFormation, ref bool ____formationDrawingMode, bool ____isMouseDown)
        {
            if (___PlayerOrderController.SelectedFormations.IsEmpty())
                return;
            switch (_currentCursorState)
            {
                case CurrentCursorState.Enemy:
                case CurrentCursorState.Friend:
                    if (IsDraggingFormation(__instance, ____formationDrawingStartingPointOfMouse, ____formationDrawingStartingTime))
                    {
                        if ((__instance.Input.IsKeyDown(InputKey.LeftMouseButton) ||
                             __instance.Input.IsKeyDown(InputKey.ControllerRTrigger)) && ____isMouseDown)
                        {
                            ____formationDrawingMode = true;
                            ____clickedFormation = null;
                        }
                    }

                    break;
            }
        }

        public static bool Prefix_GetCursorState(OrderTroopPlacer __instance, ref CursorState __result, ref Formation ____mouseOverFormation,
            List<GameEntity> ____orderRotationEntities, ref Vec2 ____deltaMousePosition, ref bool ____formationDrawingMode, ref int ____mouseOverDirection, bool ____isMouseDown)
        {
            var activeCursorState = (CursorState)_activeCursorState.GetValue(__instance);
            CursorState cursorState = CursorState.Invisible;
            CurrentCursorState myCursorState = CurrentCursorState.Invisible;
            if (!__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.IsEmpty() && _clickedFormation == null)
            {
                float collisionDistance;
                WeakGameEntity collidedEntity;
                if (!TryGetScreenMiddleToWorldPosition(__instance, ref ____deltaMousePosition, out WorldPosition _, out collisionDistance, out collidedEntity))
                    collisionDistance = 1000f;
                if (cursorState == CursorState.Invisible && collisionDistance < 1000.0)
                {
                    if (!____formationDrawingMode && !collidedEntity.IsValid)
                    {
                        for (int index = 0; index < ____orderRotationEntities.Count; ++index)
                        {
                            GameEntity orderRotationEntity = ____orderRotationEntities[index];
                            if (orderRotationEntity.IsVisibleIncludeParents() &&
                                collidedEntity == orderRotationEntity)
                            {
                                ____mouseOverFormation =
                                    __instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.ElementAt(index / 2);
                                ____mouseOverDirection = 1 - (index & 1);
                                cursorState = CursorState.Rotation;
                                myCursorState = CurrentCursorState.Rotation;
                                break;
                            }
                        }
                    }

                    if (cursorState == CursorState.Invisible)
                    {
                        if (__instance.MissionScreen.OrderFlag.FocusedOrderableObject != null)
                        {
                            cursorState = CursorState.OrderableEntity;
                            myCursorState = CurrentCursorState.OrderableEntity;
                        }
                        else if (CommandSystemConfig.Get().IsMouseOverEnabled())
                        {
                            var formation = GetMouseOverFormation(__instance, collisionDistance,
                                __instance.Mission.PlayerTeam.PlayerOrderController, ref ____deltaMousePosition,
                                ____formationDrawingMode);
                            ____mouseOverFormation = formation;
                            if (formation != null)
                            {
                                if (formation.Team.IsEnemyOf(__instance.Mission.PlayerTeam))
                                {
                                    if (CommandSystemConfig.Get().AttackSpecificFormation)
                                    {
                                        myCursorState = CurrentCursorState.Enemy;
                                    }
                                }
                                else
                                {
                                    if (CommandSystemConfig.Get().ClickToSelectFormation)
                                    {
                                        myCursorState = CurrentCursorState.Friend;
                                    }
                                }
                            }
                        }
                    }
                    if (cursorState == CursorState.Invisible)
                    {
                        cursorState = IsCursorStateGroundOrNormal(____formationDrawingMode);
                    }
                    if (myCursorState == CurrentCursorState.Invisible /*&& !(CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).IsKeyDown(__instance.Input) && CommandSystemConfig.Get().IsMouseOverEnabled()) */|| // press middle mouse button to avoid accidentally click on ground.
                        ____formationDrawingMode)
                    {
                        myCursorState = (CurrentCursorState)cursorState;
                    }
                }
            }
            else if (_clickedFormation != null) // click on formation and hold.
            {
                cursorState = activeCursorState;
                myCursorState = (CurrentCursorState)_currentCursorState;
            }

            if (cursorState != CursorState.Ground &&
                cursorState != CursorState.Rotation)
                ____mouseOverDirection = 0;
            _currentCursorState = myCursorState;
            __result = cursorState;
            return false;
        }

        private static CursorState IsCursorStateGroundOrNormal(bool ____formationDrawingMode)
        {
            return !____formationDrawingMode
                ? CursorState.Normal
                : CursorState.Ground;
        }

        private static Agent RayCastForAgent(OrderTroopPlacer __instance, float distance, ref Vec2 ____deltaMousePosition)
        {
            __instance.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(__instance, ref ____deltaMousePosition), out var rayBegin, out var rayEnd);
            var agent = __instance.Mission.RayCastForClosestAgent(rayBegin, rayEnd,
                __instance.MissionScreen.LastFollowedAgent?.Index ?? -1, 0.3f, out var agentDistance);
            if (agentDistance > distance || agent == null)
            {
                agent = __instance.Mission.RayCastForClosestAgent(rayBegin, rayEnd,
                    __instance.MissionScreen.LastFollowedAgent?.Index ?? -1, 0.8f, out agentDistance);
            }
            return agentDistance > distance ? null : agent;
        }

        private static Formation GetMouseOverFormation(OrderTroopPlacer __instance, float collisionDistance, OrderController ___PlayerOrderController, ref Vec2 ____deltaMousePosition, bool ____formationDrawingMode)
        {

            if (_focusedFormationsCache != null && _focusedFormationsCache.Count > 0)
            {
                return _focusedFormationsCache[0];
            }
            var agent = RayCastForAgent(__instance, collisionDistance, ref ____deltaMousePosition);
            if (agent != null && agent.IsMount)
                agent = agent.RiderAgent;
            if (agent == null)
                return null;
            if (CommandSystemConfig.Get().IsMouseOverEnabled() && !__instance.IsDrawingForced && !____formationDrawingMode && agent?.Formation != null &&
                !(___PlayerOrderController.SelectedFormations.Count == 1 &&
                  ___PlayerOrderController.SelectedFormations.Contains(agent.Formation)))
            {
                return agent.Formation;
            }

            return null;
        }

        public static bool Prefix_AddOrderPositionEntity(OrderTroopPlacer __instance, int entityIndex,
            ref Vec3 groundPosition, bool fadeOut, float alpha,
            ref List<GameEntity> ____orderPositionEntities)
        {
            var config = CommandSystemConfig.Get();
            var currentMovementTargetHighlightStyle = IsFreeCamera ? config.MovementTargetHighlightStyleInRTSMode : config.MovementTargetHighlightStyleInCharacterMode;

            switch (currentMovementTargetHighlightStyle)
            {
                case MovementTargetHighlightStyle.Original:
                    if (_originalOrderPositionEntities == null)
                    {
                        _originalOrderPositionEntities = new List<GameEntity>();
                        _originalMaterial = null;
                    }
                    break;
                case MovementTargetHighlightStyle.NewModelOnly:  
                    if (_newModelOrderPositionEntities == null)
                    {
                        _newModelOrderPositionEntities = new List<GameEntity>();
                        _newModelMaterial = Material.GetFromResource("vertex_color_blend_mat").CreateCopy();
                    }
                    break;
                case MovementTargetHighlightStyle.AlwaysVisible:
                    if (_alwaysVisibleOrderPositionEntities == null)
                    {
                        _alwaysVisibleOrderPositionEntities = new List<GameEntity>();
                        _alwaysVisibleMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                        _alwaysVisibleMaterial.Flags |= MaterialFlags.NoDepthTest;
                        // since v1.3.4, the AlwaysDepthTest flag may exists with NoDepthTest and the mesh will be invisible. We need to remove the flag here.
                        _alwaysVisibleMaterial.Flags &= ~MaterialFlags.AlwaysDepthTest;
                        
                    }
                    break;
            }
            if (_previousMovementTargetHightlightStyle != currentMovementTargetHighlightStyle)
            {
                switch (_previousMovementTargetHightlightStyle)
                {
                    case MovementTargetHighlightStyle.Original:
                        _originalMaterial = _currentMaterial;
                        foreach (GameEntity orderPositionEntity in _originalOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.HideIfNotFadingOut();
                        }
                        break;
                    case MovementTargetHighlightStyle.NewModelOnly:
                        foreach (GameEntity orderPositionEntity in _newModelOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.HideIfNotFadingOut();
                        }
                        break;
                    case MovementTargetHighlightStyle.AlwaysVisible:
                        foreach (GameEntity orderPositionEntity in _alwaysVisibleOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.HideIfNotFadingOut();
                        }
                        break;
                }
                _previousMovementTargetHightlightStyle = currentMovementTargetHighlightStyle;

                switch (currentMovementTargetHighlightStyle)
                {
                    case MovementTargetHighlightStyle.Original:
                        ____orderPositionEntities = _originalOrderPositionEntities;
                        _currentMaterial = _originalMaterial;
                        break;
                    case MovementTargetHighlightStyle.NewModelOnly:
                        ____orderPositionEntities = _newModelOrderPositionEntities;
                        _currentMaterial = _newModelMaterial;
                        break;
                    case MovementTargetHighlightStyle.AlwaysVisible:
                        ____orderPositionEntities = _alwaysVisibleOrderPositionEntities;
                        _currentMaterial = _alwaysVisibleMaterial;
                        break;
                }
            }
            if (currentMovementTargetHighlightStyle != MovementTargetHighlightStyle.Original)
            {
                while (____orderPositionEntities.Count <= entityIndex)
                {
                    GameEntity gameEntity = GameEntity.CreateEmpty(__instance.Mission.Scene);
                    gameEntity.EntityFlags |= EntityFlags.NotAffectedBySeason;
                    MetaMesh copy = MetaMesh.GetCopy("barrier_sphere");
                    //MetaMesh copy = MetaMesh.GetCopy("pyhsics_test_box");
                    //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                    if (_currentMaterial == null)
                    {
                        //____meshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                        _currentMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                        //____meshMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                        //____meshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                    }
                    copy.SetMaterial(_currentMaterial);
                    copy.SetFactor1(OrderPositionEntityColor);
                    //copy.SetContourColor(OrderPositionEntityColor);
                    //copy.SetContourState(true);
                    gameEntity.AddComponent(copy);
                    //gameEntity.SetContourColor(OrderPositionEntityColor, true);
                    gameEntity.SetVisibilityExcludeParents(false);
                    ____orderPositionEntities.Add(gameEntity);
                }
                GameEntity orderPositionEntity = ____orderPositionEntities[entityIndex];
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, groundPosition + (Vec3.Up * 1.0f));
                orderPositionEntity.SetFrame(ref frame);
                if (fadeOut)
                    orderPositionEntity.FadeOut(CommandSystemConfig.Get().MovementTargetFadeOutDuration, false);
                else if (alpha != -1.0)
                {
                    alpha = OrderPositionEntityDestinationAlpha;
                    orderPositionEntity.SetVisibilityExcludeParents(true);
                    orderPositionEntity.SetAlpha(alpha);
                    //orderPositionEntity.FadeIn();
                }
                else
                {
                    //alpha = OrderPositionEntityPreviewAlpha;
                    //orderPositionEntity.SetVisibilityExcludeParents(true);
                    //orderPositionEntity.SetAlpha(alpha);
                    orderPositionEntity.FadeIn();
                }

                return false;
            }

            return true;
        }

        private static void HandleSelectFormationKeyDown(OrderTroopPlacer __instance, ref Formation ____clickedFormation, ref Formation ____mouseOverFormation,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            if (__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.IsEmpty() || ____clickedFormation != null)
                return;
            switch (_currentCursorState)
            {
                case CurrentCursorState.Enemy:
                    ____formationDrawingMode = false;
                    ____clickedFormation = ____mouseOverFormation;

                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
                case CurrentCursorState.Friend:
                    ____formationDrawingMode = false;
                    if (____mouseOverFormation != null && __instance.Mission.PlayerTeam.PlayerOrderController.IsFormationSelectable(____mouseOverFormation))
                    {
                        ____clickedFormation = ____mouseOverFormation;
                    }
                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
            }
        }

        private static void HandleSelectFormationKeyUp(OrderTroopPlacer __instance, ref Formation ____clickedFormation, OrderController ___PlayerOrderController, List<GameEntity> ____orderRotationEntities,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            if (!____formationDrawingMode)
            {
                if (_focusedFormationsCache != null && _focusedFormationsCache.Count > 0)
                {
                    ____clickedFormation = _focusedFormationsCache.FirstOrDefault();
                }
                if (____clickedFormation != null)
                {
                    if (____clickedFormation.CountOfUnits > 0)
                    {
                        bool isEnemy = Utility.IsEnemy(____clickedFormation);
                        if (!isEnemy)
                        {
                            HideNonSelectedOrderRotationEntities(___PlayerOrderController, ____orderRotationEntities, ____clickedFormation);

                            if (___PlayerOrderController.IsFormationSelectable(____clickedFormation))
                            {
                                //SelectFormationFromUI(__instance, ____clickedFormation);
                                SelectFormationFromController(__instance, ___PlayerOrderController, ____clickedFormation);
                            }
                        }
                        else if (CommandSystemConfig.Get().AttackSpecificFormation)
                        {
                            bool keepMovementOrder = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepMovementOrder).IsKeyDownInOrder(__instance.Input);
                            if (keepMovementOrder)
                            {
                                if (Campaign.Current == null || CommandSystemSkillBehavior.CanIssueChargeToFormationOrder)
                                {
                                    Utilities.Utility.FocusOnFormation(___PlayerOrderController, ____clickedFormation);
                                }
                                else
                                {
                                    Utility.DisplayMessage(GameTexts
                                        .FindText("str_rts_camera_command_system_tactic_level_required")
                                        .SetTextVariable("level",
                                            CommandSystemSkillBehavior.RequiredTacticsLevelToIssueChargeToFormationOrder)
                                        .ToString());
                                }
                            }
                            else
                            {
                                Utilities.Utility.ChargeToFormation(___PlayerOrderController, ____clickedFormation);
                            }
                        }
                    }

                    ____clickedFormation = null;

                    ____formationDrawingMode = false;
                    ____formationDrawingStartingPosition = null;
                    ____formationDrawingStartingPointOfMouse = null;
                    ____formationDrawingStartingTime = null;
                    ____deltaMousePosition = Vec2.Zero;
                }
            }
        }

        public static bool Prefix_OnMissionScreenTick(OrderTroopPlacer __instance, ref bool ____initialized,
            ref bool ____isDrawnThisFrame, ref bool ____isMouseDown, ref Timer ___formationDrawTimer, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime,
            ref bool ____formationDrawingMode, Formation ____mouseOverFormation,
            ref List<GameEntity> ____orderPositionEntities, ref List<GameEntity> ____orderRotationEntities,
            ref bool ____wasDrawingForced, ref bool ____wasDrawingFacing, ref bool ____wasDrawingForming, ref bool ____wasDrawnPreviousFrame, ref WorldPosition? ____formationDrawingStartingPosition,
            ref Vec2 ____deltaMousePosition)
        {
            if (!____initialized)
                return false;


            _activeCursorState.SetValue(__instance,
                (CursorState)_cursorState.Invoke(_orderTroopPlacer, new object[] { }));
            if (!__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.Any())
                return false;
            ____isDrawnThisFrame = false;
            if (__instance.SuspendTroopPlacer)
                return false;
            
            bool isSelectFormationKeyPressed = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                .IsKeyPressed(__instance.Input);
            bool isSelectFormationKeyReleased = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                                CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                    .IsKeyReleased(__instance.Input);
            bool isSelectFormationKeyDown = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                .IsKeyDown(__instance.Input);
            //_currentCursorState = _cachedCursorState.Value;
            bool isLeftButtonPressed = __instance.Input.IsKeyPressed(InputKey.LeftMouseButton) ||
                              __instance.Input.IsKeyPressed(InputKey.ControllerRTrigger);

            if (isLeftButtonPressed)
            {
                ____isMouseDown = true;
                _handleMouseDown?.Invoke(__instance, new object[] { });
            }
            if (isSelectFormationKeyPressed)
            {
                HandleSelectFormationKeyDown(__instance, ref _clickedFormation, ref ____mouseOverFormation,
                    ref ____formationDrawingMode, ref ____deltaMousePosition, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime);
            }
            if (isSelectFormationKeyReleased)
            {
                HandleSelectFormationKeyUp(__instance, ref _clickedFormation, __instance.Mission.PlayerTeam.PlayerOrderController,
                    ____orderRotationEntities, ref ____formationDrawingMode, ref ____deltaMousePosition,
                    ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                    ref ____formationDrawingStartingTime);
            }
            if ((__instance.Input.IsKeyReleased(InputKey.LeftMouseButton) || __instance.Input.IsKeyReleased(InputKey.ControllerRTrigger)) && ____isMouseDown || isSelectFormationKeyReleased)
            {
                ____isMouseDown = false;
                // Formation.GetOrderPositionOfUnit is wrong in the next tick after movement order is issued.
                // we skip updating from the wrong position for 1 tick.
                _skipDrawingForDestinationForOneTick = true;
                _handleMouseUp?.Invoke(__instance, new object[] { });
            }
            else if (____isMouseDown && isSelectFormationKeyDown)
            {
                if (!__instance.IsDrawingFacing && !__instance.IsDrawingForming)
                {
                    TryTransformFromClickingToDragging(__instance, ____formationDrawingStartingPointOfMouse,
                        ____formationDrawingStartingTime, __instance.Mission.PlayerTeam.PlayerOrderController, ref _clickedFormation,
                        ref ____formationDrawingMode, ____isMouseDown);
                }
            }
            if ((__instance.Input.IsKeyDown(InputKey.LeftMouseButton) || __instance.Input.IsKeyDown(InputKey.ControllerRTrigger)) && ____isMouseDown)
            {
                if (___formationDrawTimer.Check(MBCommon.GetApplicationTime()) &&
                    !__instance.IsDrawingFacing &&
                    !__instance.IsDrawingForming)
                {
                    if (_currentCursorState == CurrentCursorState.Ground)
                        __instance.UpdateFormationDrawing(false);
                }
            }
            else if (__instance.IsDrawingForced)
            {
                if (___formationDrawTimer.Check(MBCommon.GetApplicationTime()))
                {
                    //Utilities.DisplayMessage("drawing forced");
                    Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                    ref ____mouseOverFormation, ref _clickedFormation);
                    ____formationDrawingMode = true;
                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    //HandleMousePressed();

                    __instance.UpdateFormationDrawing(false);
                }
            }
            else if (__instance.IsDrawingFacing || ____wasDrawingFacing)
            {
                if (__instance.IsDrawingFacing)
                {
                    Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                        ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                        ref ____mouseOverFormation, ref _clickedFormation);

                    _updateFormationDrawingForFacingOrder.Invoke(__instance, new object[] { false });
                }
            }
            else if (__instance.IsDrawingForming || ____wasDrawingForming)
            {
                if (__instance.IsDrawingForming)
                {
                    Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                        ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                        ref ____mouseOverFormation, ref _clickedFormation);

                    _updateFormationDrawingForFormingOrder.Invoke(__instance, new object[] { false });
                }
            }
            else if (____wasDrawingForced)
            {
                Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                    ref ____mouseOverFormation, ref _clickedFormation);
            }
            else if (_skipDrawingForDestinationForOneTick)
            {
                _skipDrawingForDestinationForOneTick = false;
            }
            else
            {
                _updateFormationDrawingForDestination.Invoke(__instance, new object[] { false });
            }

            UpdateMouseOverFormation(____mouseOverFormation);
            foreach (GameEntity orderPositionEntity in ____orderPositionEntities)
                orderPositionEntity.SetPreviousFrameInvalid();
            foreach (GameEntity orderRotationEntity in ____orderRotationEntities)
                orderRotationEntity.SetPreviousFrameInvalid();
            ____wasDrawingForced = __instance.IsDrawingForced;
            ____wasDrawingFacing = __instance.IsDrawingFacing;
            ____wasDrawingForming = __instance.IsDrawingForming;
            ____wasDrawnPreviousFrame = ____isDrawnThisFrame;

            return false;
        }

        private static void UpdateMouseOverFormation(Formation ____mouseOverFormation)
        {
            _outlineView?.MouseOver(____mouseOverFormation);
            _groundMarkerView?.MouseOver(____mouseOverFormation);
        }
        private static void Reset(ref bool ____isMouseDown, ref bool ____formationDrawingMode, ref WorldPosition? ____formationDrawingStartingPosition,
            ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime,
            ref Formation ____mouseOverFormation, ref Formation ____clickedFormation)
        {
            ____isMouseDown = false;
            ____formationDrawingMode = false;
            ____formationDrawingStartingPosition = new WorldPosition?();
            ____formationDrawingStartingPointOfMouse = new Vec2?();
            ____formationDrawingStartingTime = new float?();
            ____mouseOverFormation = (Formation)null;
            ____clickedFormation = (Formation)null;
        }

        //public static void SelectFormationFromUI(OrderTroopPlacer __instance, Formation ____clickedFormation)
        //{
        //    var uiHandler = __instance.Mission.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
        //    if (uiHandler == null)
        //        return;
        //    var dataSource = (MissionOrderVM)_dataSource.GetValue(uiHandler);
        //    if (dataSource == null)
        //    {
        //        return;
        //    }

        //    dataSource.OnSelect(____clickedFormation.Index);
        //}

        public static void SelectFormationFromController(OrderTroopPlacer __instance, OrderController ___PlayerOrderController, Formation ____clickedFormation)
        {
            if (!CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepFormationWidth).IsKeyDownInOrder(__instance.Input))
            {
                ___PlayerOrderController.ClearSelectedFormations();
                ___PlayerOrderController.SelectFormation(____clickedFormation);
            }
            else if (___PlayerOrderController.IsFormationListening(____clickedFormation))
            {
                ___PlayerOrderController.DeselectFormation(____clickedFormation);
            }
            else
            {
                ___PlayerOrderController.SelectFormation(____clickedFormation);
            }
        }

        public static bool Prefix_UpdateFormationDrawingForMovementOrder(OrderTroopPlacer __instance,
            bool giveOrder,
            WorldPosition formationRealStartingPosition,
            WorldPosition formationRealEndingPosition,
            bool isFormationLayoutVertical, ref bool ____isDrawnThisFrame)
        {
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations));
            }
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout, __instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations, null, null, null);
            ____isDrawnThisFrame = true;
            List<WorldPosition> simulationAgentFrames = null;
            bool isLineShort = false;
            IEnumerable<Formation> formations = __instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.Where((f => f.CountOfUnitsWithoutDetachedOnes > 0));
            if (!formations.Any())
                return true;
            Patch_OrderController.SimulateNewOrderWithPositionAndDirection(formations, __instance.Mission.PlayerTeam.PlayerOrderController.simulationFormations, formationRealStartingPosition, formationRealEndingPosition, true, out simulationAgentFrames, giveOrder && queueCommand, out var formationChanges, out isLineShort, isFormationLayoutVertical, true);
            if (simulationAgentFrames == null)
            {
                return true;
            }
            bool shouldLimitFormationSpeedToLowest = Utilities.Utility.ShouldLockFormation();
            if (giveOrder)
            {
                if (!queueCommand)
                {
                    if (!isFormationLayoutVertical)
                        __instance.Mission.PlayerTeam.PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegmentWithHorizontalLayout, formationRealStartingPosition, formationRealEndingPosition);
                    else
                        __instance.Mission.PlayerTeam.PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment, formationRealStartingPosition, formationRealEndingPosition);
                    CommandQueueLogic.TryPendingOrder(formations, new OrderInQueue
                    {
                        SelectedFormations = formations.ToList(),
                        OrderType = isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout,
                        IsLineShort = isLineShort,
                        PositionBegin = formationRealStartingPosition,
                        PositionEnd = formationRealEndingPosition,
                        VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations),
                        ShouldAdjustFormationSpeed = shouldLimitFormationSpeedToLowest
                    });
                }
                else
                {
                    CommandQueueLogic.AddOrderToQueue(new OrderInQueue
                    {
                        SelectedFormations = formations.ToList(),
                        OrderType = isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout,
                        IsLineShort = isLineShort,
                        ActualFormationChanges = formationChanges,
                        PositionBegin = formationRealStartingPosition,
                        PositionEnd = formationRealEndingPosition,
                        VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations),
                        ShouldAdjustFormationSpeed = shouldLimitFormationSpeedToLowest
                    });
                    // This is required to keep MissionOrderVM open in rts mode and close it in player mode.
                    Utilities.Utility.MissionOrderVM_OnOrderExecutedWithId("order_movement_move");
                }
            }
            AddOrderPositionEntity(simulationAgentFrames, giveOrder);
            return false;
        }
        private static Vec3 GetGroundedVec3(Mission mission, WorldPosition worldPosition)
        {
            if (!mission.IsNavalBattle)
                return worldPosition.GetGroundVec3();
            Vec2 asVec2 = worldPosition.AsVec2;
            return new Vec3(asVec2.X, asVec2.Y, mission.Scene.GetWaterLevelAtPosition(asVec2, true, true));
        }

        public static bool Prefix_UpdateFormationDrawingForFacingOrder(OrderTroopPlacer __instance,
            bool giveOrder)
        {
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations));
            }
            return true;
        }

        public static void SetIsDrawingFacing(bool isDrawingFacing)
        {
            if (_orderTroopPlacer == null)
                return;
            _orderTroopPlacer.IsDrawingFacing = isDrawingFacing;
        }

        public static void Reset()
        {
            if (_orderTroopPlacer == null)
                return;
            _reset.Invoke(_orderTroopPlacer, new object[] { });
        }

        public static void AddOrderPositionEntity(List<WorldPosition> agentFrames, bool fadeOut, int startIndex = 0)
        {
            foreach (WorldPosition worldPosition in agentFrames)
            {
                _addOrderPositionEntity.Invoke(_orderTroopPlacer,
                    new object[]
                    {
                        startIndex, GetGroundedVec3(_orderTroopPlacer.Mission, worldPosition), fadeOut, -1f
                    });
                ++startIndex;
            }
        }
    }
}
