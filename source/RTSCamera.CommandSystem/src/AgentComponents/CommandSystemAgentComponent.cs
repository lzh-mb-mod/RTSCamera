using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Logic.SubLogic;
using RTSCamera.CommandSystem.QuerySystem;
using RTSCameraAgentComponent;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.AgentComponents
{
    public struct Highlight
    {
        public Highlight(uint? color, bool alwaysVisible)
        {
            Color = color;
            AlwaysVisible = alwaysVisible;
        }
        public uint? Color;
        public bool AlwaysVisible;
    }
    public class CommandSystemAgentComponent : AgentComponent
    {
        public static uint InvisibleColor = 0x00000000; // Transparent color for invisible contour
        private readonly Highlight[] _colors = new Highlight[(int)ColorLevel.NumberOfLevel];
        private int _currentLevel = -1;

        private uint? CurrentColor => _currentLevel < 0 ? null : _colors[_currentLevel].Color;
        private bool CurrentAlwaysVisible => _currentLevel < 0 || _colors[_currentLevel].AlwaysVisible;

        private bool _shouldUpdateColor = false;

        public float DistanceSquaredToTargetPosition = 0;
        private Timer _cachedDistanceUpdateTimer;
        private MetaMesh _mesh;
        private static Material _material;
        //private static Material _defaultMaterial;
        //private bool _materialCleared;

        private AgentAIInputHandler _agentAIInputHandler = new AgentAIInputHandler();
        public CommandSystemAgentComponent(Agent agent) : base(agent)
        {
            for (int i = 0; i < _colors.Length; ++i)
            {
                _colors[i] = new Highlight(null, false);
            }
            _cachedDistanceUpdateTimer = new Timer(agent.Mission.CurrentTime, 0.2f + MBRandom.RandomFloat * 0.1f);
        }

        public void Refresh()
        {
            if (_mesh != null)
            {
#if DEBUG
                // called from Agent.UpdateSpawnEquipmentAndRefreshVisuals
                // the mesh has been cleared.
                Utility.DisplayMessage($"agent mesh refreshed");
#endif
                Agent?.AgentVisuals?.GetEntity()?.RemoveComponent(_mesh);
                InitializeAux();
            }
        }

        private void TryInitialize()
        {
            if (_mesh == null)
            {
                InitializeAux();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Agent.SetHasOnAiInputSetCallback(true);
        }

        private void InitializeAux()
        {
            if (Agent.IsMount)
            {
                return;
            }

            _mesh = MetaMesh.GetCopy("rts_unit_marker");
            if (_material == null)
            {
                _material = _mesh.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                _material.Flags |= MaterialFlags.TwoSided;
            }
            _mesh.SetMaterial(_material);
            //ClearMaterial();
            UpdateMeshFrame(Agent.HasMount);
            Agent.AgentVisuals.GetEntity().AddMultiMesh(_mesh);
            _mesh.SetFactor1(InvisibleColor);
            _mesh.SetContourColor(InvisibleColor);
            _mesh.SetContourState(false);
            _mesh.SetVisibilityMask(0);
            Agent.AgentVisuals.LazyUpdateAgentRendererData();
        }

        //private void ClearMaterial()
        //{
        //    if (_defaultMaterial == null)
        //    {
        //        _defaultMaterial = Material.GetFromResource("default_empty");
        //    }
        //    _mesh.SetMaterial(_defaultMaterial);
        //    _materialCleared = true;
        //}
        
        //private void RecoverMaterial()
        //{
        //    _mesh.SetMaterial(_material);
        //    _materialCleared = false;
        //    Agent.SetRenderCheckEnabled(true);
        //    Agent.AgentVisuals.SetVisible(true);
        //}

        public void SetColor(int level, uint? color, bool alwaysVisible, bool updateInstantly)
        {
            if (SetColorWithoutUpdate(level, color, alwaysVisible))
            {
                _currentLevel = color.HasValue ? level : EffectiveLevel(level - 1);
                if (updateInstantly)
                {
                    _shouldUpdateColor = false;
                    SetColor();
                }
                else
                {
                    _shouldUpdateColor = true;
                }
            }
        }

        private bool SetColorWithoutUpdate(int level, uint? color, bool alwaysVisible)
        {
            if (level < 0 || level >= _colors.Length)
                return false;
            if (_colors[level].Color == color)
                return false;
            _colors[level].Color = color;
            _colors[level].AlwaysVisible = alwaysVisible;
            return _currentLevel <= level; // needs update.
        }

        private void UpdateColor()
        {
            _currentLevel = EffectiveLevel();
            _shouldUpdateColor = true;
        }

        public static void ClearColorForAgent(Agent agent)
        {
            var component = agent.GetComponent<CommandSystemAgentComponent>();
            if (component == null)
                return;

            component.ClearColor();
        }

        public void ClearColor()
        {
            try
            {
                for (int i = 0; i < _colors.Length; ++i)
                {
                    _colors[i].Color = null;
                }
                if (_mesh == null)
                {
                    InitializeAux();
                }
                if (_mesh == null)
                {
                    return;
                }
                for (int i = 0; i < _colors.Length; ++i)
                {
                    _colors[i].Color = null;
                }

                _mesh.SetFactor1(InvisibleColor);
                _mesh.SetContourColor(InvisibleColor);
                _mesh.SetContourState(false);
                _mesh.SetVisibilityMask(0);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public void ClearTargetOrSelectedFormationColor()
        {
            if (_mesh == null)
            {
                InitializeAux();
            }
            if (_mesh == null)
            {
                return;
            }
            bool needUpdate = SetColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            if (needUpdate)
                UpdateColor();
        }

        public void ClearFormationColor()
        {
            if (_mesh == null)
            {
                InitializeAux();
            }
            if (_mesh == null)
            {
                return;
            }
            bool needUpdate = SetColorWithoutUpdate((int)ColorLevel.TargetFormation, null, true);
            needUpdate |= SetColorWithoutUpdate((int)ColorLevel.SelectedFormation, null, true);
            needUpdate |= SetColorWithoutUpdate((int)ColorLevel.MouseOverFormation, null, true);
            if (needUpdate)
                UpdateColor();
        }

        public override void OnMount(Agent mount)
        {
            base.OnMount(mount);

            try
            {
                if (_mesh != null)
                {
                    UpdateMeshFrame(true);
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public override void OnDismount(Agent mount)
        {
            base.OnDismount(mount);

            try
            {
                if (_mesh != null)
                {
                    UpdateMeshFrame(false);
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public void TryUpdateColor()
        {
            if (_mesh == null)
            {
                InitializeAux();
            }
            if (_mesh == null)
            {
                return;
            }
            if (_shouldUpdateColor)
            {
                _shouldUpdateColor = false;
                SetColor();
            }
        }

        private int EffectiveLevel(int maxLevel = (int)ColorLevel.NumberOfLevel - 1)
        {
            for (int i = maxLevel; i > -1; --i)
            {
                if (_colors[i].Color.HasValue)
                    return i;
            }

            return -1;
        }

        private void SetColor()
        {
            try
            {
                //if (_materialCleared)
                //{
                //    RecoverMaterial();
                //}
                TryInitialize();
                var color = CurrentColor.HasValue ? CurrentColor.Value : InvisibleColor;
                _mesh.SetFactor1(color);
                _mesh.SetContourColor(color);
                _mesh.SetContourState(CurrentAlwaysVisible);
                _mesh.SetVisibilityMask(VisibilityMaskFlags.Final);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(e.ToString()));
            }
        }

        public override void OnAgentRemoved()
        {
            base.OnAgentRemoved();

            if (_mesh == null)
            {
                return;
            }
            ClearColor();
            Agent?.AgentVisuals?.GetEntity()?.RemoveComponent(_mesh);
            _mesh = null;
        }

        public void SetContourState(bool alwaysVisible)
        {
            TryInitialize();
            if (_mesh == null)
            {
                return;
            }
            if (alwaysVisible)
            {
                _mesh.SetContourState(true);
            }
            else
            {
                _mesh.SetContourState(false);
            }
        }

        private void UpdateMeshFrame(bool hasMount)
        {
            var frame = MatrixFrame.Identity;
            frame.origin = new Vec3(0f, 0.3f, 0.2f);
            frame.rotation = Mat3.CreateMat3WithForward(-Vec3.Forward);
            if (hasMount)
            {
                frame.Scale(new Vec3(1.8f, 1.8f, 1f));
            }
            else
            {
                frame.Scale(new Vec3(1f, 1f, 1f));
            }
            _mesh.Frame = frame;
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            if (Agent.Formation == null)
            {
                return;
            }

            if (!_cachedDistanceUpdateTimer.Check(Agent.Mission.CurrentTime))
            {
                return;
            }

            var query = CommandQuerySystem.GetQueryForFormation(Agent.Formation);
            if ((query?.NeedToUpdateTargetPositionDistance ?? false) == false)
            {
                return;
            }

            var worldPosition = Agent.Formation.GetOrderPositionOfUnit(Agent);
            if (worldPosition.IsValid)
            {
                var orderPosition = worldPosition.GetGroundVec3();
                var agentPosition = Agent.Position;
                var pos2SecsLater = agentPosition + Agent.Velocity * 2f;
                var vec1 = pos2SecsLater - agentPosition;
                var vec2 = orderPosition - agentPosition;
                if (vec1.LengthSquared < 0.1f)
                {
                    DistanceSquaredToTargetPosition = vec2.LengthSquared;
                    return;
                }
                var t = Vec3.DotProduct(vec1, vec2) / vec1.LengthSquared;
                if (t < 0)
                {
                    DistanceSquaredToTargetPosition = vec2.LengthSquared;
                }
                else if (t > 1)
                {
                    DistanceSquaredToTargetPosition = orderPosition.DistanceSquared(pos2SecsLater);
                }
                else
                {
                    DistanceSquaredToTargetPosition = orderPosition.DistanceSquared(agentPosition + t * vec1);
                }
            }
            else
            {
                DistanceSquaredToTargetPosition = 0;
            }
        }

        public VolleyMode GetVolleyMode()
        {
            return _agentAIInputHandler.VolleyMode;
        }

        public void SetVolleyMode(VolleyMode volleyMode)
        {
            _agentAIInputHandler.SetVolleyMode(Agent, volleyMode);
            if (volleyMode != VolleyMode.Disabled)
            {
                Agent.SetHasOnAiInputSetCallback(true);
            }
        }

        public bool ShootUnderVolley()
        {
            return _agentAIInputHandler.ShootUnderVolley(Agent);
        }

        public bool IsVolleySuspended()
        {
            return _agentAIInputHandler.IsVolleySuspended;
        }

        public bool IsCandidateForNextFireAutoVolley()
        {
            return _agentAIInputHandler.IsCandidateForNextFireInAutoVolley(Agent);
        }

        public WeaponClass GetCurrentlyUsingWeaponClass()
        {
            if (Agent.WieldedWeapon.IsEmpty)
            {
                return WeaponClass.Undefined;
            }
            else
            {
                return Agent.WieldedWeapon.CurrentUsageItem.WeaponClass;
            }
        }

        public bool IsUsingThrownWeapon()
        {
            if (Agent.WieldedWeapon.IsEmpty)
            {
                return false;
            }
            else
            {
                return Agent.WieldedWeapon.CurrentUsageItem.IsConsumable;
            }
        }

        public bool IsReadyForNextFire()
        {
            return _agentAIInputHandler.IsReadyForNextFire(Agent);
        }

        public override void OnAIInputSet(ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            base.OnAIInputSet(ref eventFlag, ref movementFlag, ref inputVector);

            _agentAIInputHandler.OnAIInputSet(Agent, ref eventFlag, ref movementFlag, ref inputVector);
        }

        public override void OnFormationSet()
        {
            base.OnFormationSet();

            _agentAIInputHandler.OnFormationSet(Agent);
        }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)
        {
            base.OnHit(affectorAgent, damage, affectorWeapon, b, collisionData);

            _agentAIInputHandler.OnHit(Agent, affectorAgent, damage, affectorWeapon, b, collisionData);

        }

        public void OnControllerChanged(AgentControllerType oldController)
        {
            _agentAIInputHandler.OnControllerChanged(Agent, oldController);
        }
    }
}
