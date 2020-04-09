using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    public class DisableDeathLogic : MissionLogic
    {
        private EnhancedMissionConfig _config;

        public DisableDeathLogic(EnhancedMissionConfig config)
        {
            _config = config;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            SetDisableDeath(_config.DisableDeath);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this.Mission.InputManager.IsKeyPressed(InputKey.F11))
            {
                this._config.DisableDeath = !this._config.DisableDeath;
                SetDisableDeath(this._config.DisableDeath);
            }
        }

        public void SetDisableDeath(bool disableDeath)
        {
            Mission.DisableDying = disableDeath;
            PrintDeathStatus(disableDeath);
        }

        private void PrintDeathStatus(bool disableDeath)
        {
            Utility.DisplayLocalizedText(disableDeath ? "str_death_disabled" : "str_death_enabled");
        }
    }
}
