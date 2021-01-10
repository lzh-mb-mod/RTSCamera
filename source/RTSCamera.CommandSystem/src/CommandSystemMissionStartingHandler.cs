using System.Collections.Generic;
using MissionLibrary.Controller;
using MissionSharedLibrary.Controller;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.CommandSystem.Patch.CircularFormation;
using RTSCamera.CommandSystem.View;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemMissionStartingHandler : AMissionStartingHandler

    {
        public override void OnCreated(MissionView entranceView)
        {
            var config = CommandSystemConfig.Get();
            if (config.AttackSpecificFormation)
            {
                PatchChargeToFormation.Patch();
            }
            if (config.FixCircularArrangement)
            {
                PatchCircularFormation.Patch();
            }
            List<MissionBehaviour> list = new List<MissionBehaviour>
            {
                new FormationColorMissionView(),
                new CommandSystemOrderTroopPlacer(),
                new DragWhenCommandView()
            };

            foreach (var missionBehaviour in list)
            {
                MissionStartingManager.AddMissionBehaviour(entranceView, missionBehaviour);
            }
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
            var orderTroopPlacer = entranceView.Mission.GetMissionBehaviour<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
                entranceView.Mission.RemoveMissionBehaviour(orderTroopPlacer);
        }
    }
}
