using RTSCamera.Config;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic
{
    public class DisableDeathLogic : MissionLogic
    {
        private RTSCameraConfig _config;
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public DisableDeathLogic(RTSCameraConfig config)
        {
            _config = config;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            SetDisableDeath(_config.DisableDeath, true);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!NativeConfig.CheatMode)
                return;
            if (_config.DisableDeathHotkeyEnabled && Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.DisableDeath)))
            {
                _config.DisableDeath = !_config.DisableDeath;
                SetDisableDeath(_config.DisableDeath);
            }
        }

        public void SetDisableDeath(bool disableDeath, bool atStart = false)
        {
            if (!NativeConfig.CheatMode)
                return;
            Mission.DisableDying = disableDeath;
            if (atStart && !disableDeath)
                return;
            PrintDeathStatus(disableDeath);
        }

        private void PrintDeathStatus(bool disableDeath)
        {
            Utility.DisplayLocalizedText(disableDeath ? "str_rts_camera_death_disabled" : "str_rts_camera_death_enabled");
        }
    }
}
