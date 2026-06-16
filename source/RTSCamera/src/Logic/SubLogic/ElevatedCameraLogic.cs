using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.TwoDimension;

namespace RTSCamera.Logic.SubLogic
{
    public class ElevatedCameraLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private Vec3 _targetLocalOffset;
        private Vec3 _localOffset;
        private float _scale = 10f;

        public bool IsElevatedCameraEnabled;

        public Mission Mission => _logic.Mission;

        public ElevatedCameraLogic(RTSCameraLogic rTSCameraLogic)
        {
            this._logic = rTSCameraLogic;
        }

        public void OnBehaviourInitialize()
        {
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }

        public void OnRemoveBehaviour()
        {
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggledOrderView);
        }
        private void OnToggledOrderView(MissionPlayerToggledOrderViewEvent e)
        {
            if (e.IsOrderEnabled)
            {
#if DEBUG
                Utility.DisplayMessage("Toggled on order view");
#endif
                if (ShouldElevatedCameraOnOrder())
                {
                    TurnOnElevatedCamera();
                }
            }
            else
            {
#if DEBUG
                Utility.DisplayMessage("Toggled off order view");
#endif
                TurnOffElevatedCamera();
            }
        }

        private bool ShouldElevatedCameraOnOrder()
        {
            if (_config.CameraModeOnOrdering == CameraModeOnOrdering.Elevated)
            {
                if (Mission.IsNavalBattle && Utilities.Utility.GetPlayerControlledShip(Mission) != null)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public void TurnOnElevatedCamera()
        {
            if (IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Turned on overlook camera");
#endif
            IsElevatedCameraEnabled = true;
        }

        public void TurnOffElevatedCamera()
        {
            if (!IsElevatedCameraEnabled)
                return;
#if DEBUG
            Utility.DisplayMessage("Turned off overlook camera");
#endif
            IsElevatedCameraEnabled = false;
        }

        internal void OnPreMissionTick(float dt)
        {
            if (IsElevatedCameraEnabled && Agent.Main != null)
            {
                HandleInput(dt);
                var height = _scale * Agent.Main.AgentScale * 0.2f;
                var offset = _scale * Agent.Main.AgentScale * 0.7f;
                _targetLocalOffset = MBMath.Lerp(_targetLocalOffset, new Vec3(0, 0, height), dt * 5f, 1f / 1000f);
                _localOffset = MBMath.Lerp(_localOffset, new Vec3(0, -offset, offset * 0.4f), dt * 3f, 1 / 1000f);
                _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
            }
            else
            {
                _targetLocalOffset = MBMath.Lerp(_targetLocalOffset, Vec3.Zero, dt * 5f, 1f / 1000f);
                _localOffset = MBMath.Lerp(_localOffset, Vec3.Zero, dt * 3f, 1f / 1000f);
                _logic.Mission.SetCustomCameraTargetLocalOffset(_targetLocalOffset);
                _logic.Mission.SetCustomCameraLocalOffset(_localOffset);
            }
        }

        private void HandleInput(float dt)
        {
            var missionScreen = Utility.GetMissionScreen();
            var scroll = missionScreen.SceneLayer.Input.GetDeltaMouseScroll();
            _scale = MBMath.ClampFloat(_scale - scroll / 100f  * TaleWorlds.Library.MathF.Log10(Mathf.Max(_scale * 10f, 10)), 3f, 50f);
        }
    }
}
