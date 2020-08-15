using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
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
            if (_config.DisableDeathHotkeyEnabled && this.Mission.InputManager.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.DisableDeath)))
            {
                this._config.DisableDeath = !this._config.DisableDeath;
                SetDisableDeath(this._config.DisableDeath);
            }
        }

        public void SetDisableDeath(bool disableDeath, bool atStart = false)
        {
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
