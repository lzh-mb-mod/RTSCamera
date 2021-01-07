using MissionLibrary.HotKey;
using MissionSharedLibrary.HotKey.Category;
using MissionSharedLibrary.View;
using TaleWorlds.Core;

namespace RTSCamera.Config
{
    public class RTSCameraMenuView : MissionSharedLibrary.View.MissionMenuViewBase
    {

        public RTSCameraMenuView()
            : base(24, nameof(RTSCameraMenuView))
        {
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
                if (GauntletLayer.Input.IsKeyReleased(GeneralGameKeyCategories.GetKey(GeneralGameKey.OpenMenu)))
                    DeactivateMenu();
            }
            else if (Input.IsKeyReleased(GeneralGameKeyCategories.GetKey(GeneralGameKey.OpenMenu)))
                ActivateMenu();
        }

        protected override MissionMenuVMBase GetDataSource()
        {
            return new RTSCameraMenuVM(Mission, OnCloseMenu);
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            RTSCameraConfig.Clear();
        }
    }
}
