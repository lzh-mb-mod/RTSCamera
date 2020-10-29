using MissionLibrary.HotKey;
using MissionLibrary.HotKey.Category;
using TaleWorlds.Core;

namespace RTSCamera.Config
{
    public class RTSCameraMenuView : MissionSharedLibrary.View.MissionMenuViewBase
    {

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
                if (GauntletLayer.Input.IsKeyReleased(MissionLibraryGameKeyCategory.GetKey(GeneralGameKey.OpenMenu)))
                    DeactivateMenu();
            }
            else if (Input.IsKeyReleased(MissionLibraryGameKeyCategory.GetKey((int)GeneralGameKey.OpenMenu)))
                ActivateMenu();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            RTSCameraConfig.Clear();
        }
    }
}
