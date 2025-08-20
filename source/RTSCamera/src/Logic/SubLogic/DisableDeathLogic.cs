using MissionLibrary.HotKey;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class DisableDeathLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private readonly AGameKeyCategory _gameKeyCategory = RTSCameraGameKeyCategory.Category;

        public Mission Mission => _logic.Mission;

        public DisableDeathLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnMissionTick(float dt)
        {
            if (!NativeConfig.CheatMode)
                return;
            if (_config.DisableDeathHotkeyEnabled && _gameKeyCategory.GetGameKeySequence((int)GameKeyEnum.DisableDeath).IsKeyPressed())
            {
                SetDisableDeath(!Mission.Current.DisableDying);
            }
        }

        public bool GetDisableDeath()
        {
            return Mission.DisableDying;
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
