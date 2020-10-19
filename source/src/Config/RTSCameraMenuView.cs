using RTSCamera.View.Basic;
using TaleWorlds.Core;

namespace RTSCamera.Config
{
    public class RTSCameraMenuView : MissionMenuViewBase
    {
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public RTSCameraMenuView()
            : base(24, nameof(RTSCameraMenuView))
        {
            GetDataSource = () => new RTSCameraMenuVM(Mission, OnCloseMenu);
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            if (Mission.Mode == MissionMode.Battle)
            {
                Utility.PrintUsageHint();
                Utility.PrintOrderHint();
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (GauntletLayer.Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)))
                    DeactivateMenu();
            }
            else if (Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)))
                ActivateMenu();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            GameKeyConfig.Clear();
            RTSCameraConfig.Clear();
        }
    }
}
