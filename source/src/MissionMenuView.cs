using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera
{
    public class MissionMenuView : MissionMenuViewBase
    {
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public MissionMenuView()
            : base(24, nameof(MissionMenuView))
        {
            this.GetDataSource = () => new MissionMenuVM(Mission, this.OnCloseMenu);
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
    }
}
