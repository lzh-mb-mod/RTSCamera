using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace EnhancedMission
{
    [DefaultView]
    class AddAdditionalMissionBehaviourView : MissionView
    {
        public override void OnCreated()
        {
            base.OnCreated();
            var config = EnhancedMissionConfig.Get();
            List<MissionBehaviour> list = new List<MissionBehaviour>
            {
                new DisableDeathLogic(config),
                new MissionSpeedLogic(),
                new ControlTroopAfterPlayerDeadLogic(),
                new SwitchFreeCameraLogic(config),
                new MainAgentChangedLogic(),
                new CommanderLogic(),
                new HideHUDLogic(),

                new MissionMenuView(),
                new FlyCameraMissionView(),
                new GameKeyConfigView(),
                new EnhancedOrderTroopPlacer()
            };


            foreach (var missionBehaviour in list)
            {
                if (missionBehaviour is AddAdditionalMissionBehaviourView)
                    continue; // avoid accidentally add itself infinitely.
                AddMissionBehaviour(missionBehaviour);
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            var orderTroopPlacer = Mission.GetMissionBehaviour<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
                Mission.RemoveMissionBehaviour(orderTroopPlacer);
        }

        private void AddMissionBehaviour(MissionBehaviour behaviour)
        {
            behaviour.OnAfterMissionCreated();
            Mission.AddMissionBehaviour(behaviour);
        }
    }
}
