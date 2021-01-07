using MissionLibrary.Controller;
using MissionLibrary.Extension;
using MissionSharedLibrary.Controller;
using RTSCamera.Config;
using RTSCamera.Logic;
using RTSCamera.Patch;
using RTSCamera.Patch.CircularFormation;
using RTSCamera.View;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera
{
    public class MissionStartingHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {
            var config = RTSCameraConfig.Get();
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
                new RTSCameraSelectCharacterView(),

                new RTSCameraLogic(),

                new HideHUDView(),
                new RTSCameraMenuView(),
                new FlyCameraMissionView(),
                new RTSCameraGameKeyConfigView(),
                new FormationColorMissionView(),
                new RTSCameraOrderTroopPlacer()
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
            var orderTroopPlacer = entranceView.Mission.GetMissionBehaviour<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
                entranceView.Mission.RemoveMissionBehaviour(orderTroopPlacer);
        }
    }
}
