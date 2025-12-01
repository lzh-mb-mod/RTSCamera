using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval.SubLogic
{
     public class NavalLogic
    {
        private readonly RTSCameraLogic _logic;
        private MissionBehavior _navalShipLogic;
        private Delegate _handler;

        public NavalLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void AfterStart()
        {
            if (Mission.Current.IsNavalBattle)
            {
                _navalShipLogic = Utilities.Utility.GetNavalShipsLogic(Mission.Current);
                var eventInfo = _navalShipLogic.GetType().GetEvent("ShipControllerChanged");
                _handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, typeof(NavalLogic).GetMethod(nameof(OnShipControllerChanged), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
                eventInfo.AddEventHandler(_navalShipLogic, _handler);
            }
        }

        public void OnRemoveBehaviour()
        {
            if (Mission.Current.IsNavalBattle)
            {
                var eventInfo = _navalShipLogic.GetType().GetEvent("ShipControllerChanged");
                eventInfo.RemoveEventHandler(_navalShipLogic, _handler);
            }
        }

        public void OnShipControllerChanged(MissionObject ship)
        {
            //var formation = Utilities.Utility.GetShipFormation(ship);
            //if (Agent.Main == null || formation == null || formation != Agent.Main.Formation)
            //    return;
            //if (Utilities.Utility.IsShipAIControlled(ship) || Utilities.Utility.IsShipPlayerControlled(ship))
            //    return;
            //var steeringMode = RTSCameraConfig.Get().SteeringModeWhenPlayerStopsPiloting;
            //if (steeringMode == SteeringMode.None)
            //    return;

            //if (steeringMode == SteeringMode.None)
            //{
            //    Patch_MissionShip.ShouldAIControlPlayerShipInPlayerMode = false;
            //    Utility.DisplayLocalizedText("str_rts_camera_soldiers_stop_controlling_ship");
            //}
            //else if (!(RTSCameraSubModule.IsHelmsmanInstalled && formation.FormationIndex == FormationClass.Infantry))
            //{
            //    Patch_MissionShip.ShouldAIControlPlayerShipInPlayerMode = true;
            //    Utility.DisplayLocalizedText("str_rts_camera_soldiers_start_controlling_ship");
            //}
            //if (steeringMode == SteeringMode.DelegateCommand)
            //{
            //    formation.SetControlledByAI(true);
            //}
            //if (Mission.Current.IsOrderMenuOpen)
            //{
            //    RTSCameraLogic.Instance.SwitchFreeCameraLogic.RefreshOrders();
            //}
        }
    }
}
