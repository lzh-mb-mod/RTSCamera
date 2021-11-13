using MissionLibrary.Controller;
using MissionLibrary.Extension;
using MissionSharedLibrary.Controller;
using RTSCamera.Logic;
using RTSCamera.View;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.MissionStartingHandler
{
    public class MissionStartingHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {

            List<MissionBehavior> list = new List<MissionBehavior>
            {
                new RTSCameraSelectCharacterView(),

                new RTSCameraLogic(),

                new HideHUDView(),
                new FlyCameraMissionView()
            };


            foreach (var missionBehaviour in list)
            {
                MissionStartingManager.AddMissionBehaviour(entranceView, missionBehaviour);
            }

            foreach (var extension in RTSCameraExtension.Extensions)
            {
                foreach (var missionBehaviour in extension.CreateMissionBehaviours(entranceView.Mission))
                {
                    MissionStartingManager.AddMissionBehaviour(entranceView, missionBehaviour);
                }
            }

            foreach (var extension in MissionExtensionCollection.Extensions)
            {
                foreach (var missionBehaviour in extension.CreateMissionBehaviours(entranceView.Mission))
                {
                    MissionStartingManager.AddMissionBehaviour(entranceView, missionBehaviour);
                }
            }
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
        }
    }
}
