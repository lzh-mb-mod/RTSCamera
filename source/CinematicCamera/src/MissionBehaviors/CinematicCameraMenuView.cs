using MissionSharedLibrary.View;

namespace CinematicCamera.MissionBehaviors
{
    public class CinematicCameraMenuView : MissionMenuViewBase
    {

        public CinematicCameraMenuView()
            : base(25, nameof(CinematicCameraMenuView))
        {
        }

        protected override MissionMenuVMBase GetDataSource()
        {
            return  new CinematicCameraMenuVM(this.OnCloseMenu);
        }
    }
}
