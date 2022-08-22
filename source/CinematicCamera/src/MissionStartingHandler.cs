using CinematicCamera.MissionBehaviors;
using System.Collections.Generic;
using MissionLibrary.Controller;
using MissionSharedLibrary.Controller;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace CinematicCamera
{
    public class MissionStartingHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {
            List<MissionBehavior> list = new List<MissionBehavior>
            {
                new SetPlayerHealthLogic(),
                new CinematicCameraMenuView()
            };


            foreach (var missionBehavior in list)
            {
                MissionStartingManager.AddMissionBehaviour(entranceView, missionBehavior);
            }
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
        }
    }
}