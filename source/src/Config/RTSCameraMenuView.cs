namespace RTSCamera.Config
{
    public class RTSCameraMenuView : MissionMenuViewBase
    {
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public RTSCameraMenuView()
            : base(24, nameof(RTSCameraMenuView))
        {
            this.GetDataSource = () => new RTSCameraMenuVM(Mission, this.OnCloseMenu);
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            Utility.PrintUsageHint();
            Utility.PrintOrderHint();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (this.GauntletLayer.Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)))
                    DeactivateMenu();
            }
            else if (this.Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)))
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
