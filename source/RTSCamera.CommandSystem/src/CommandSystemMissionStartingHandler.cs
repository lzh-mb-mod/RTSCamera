using MissionLibrary.Controller;
using MissionSharedLibrary.Controller;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.View;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.CommandSystem
{
    public class CommandSystemMissionStartingHandler : AMissionStartingHandler

    {
        public override void OnCreated(MissionView entranceView)
        {
            List<MissionBehavior> list = new List<MissionBehavior>
            {
                new CommandSystemLogic(),
                new CommandQueuePreview(),
            };

            foreach (var MissionBehavior in list)
            {
                MissionStartingManager.AddMissionBehavior(entranceView, MissionBehavior);
            }
        }
        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
        }
    }
}
