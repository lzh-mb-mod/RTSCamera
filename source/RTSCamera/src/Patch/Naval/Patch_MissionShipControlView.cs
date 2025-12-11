using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionShipControlView
    {
        private static Type _shipControllerMachineType = AccessTools.TypeByName("ShipControllerMachine");
        private static PropertyInfo _controllerMachineProperty;
        private static PropertyInfo _rangedSiegeWeaponProperty;
        private static PropertyInfo _attachedShipProperty;
        private static PropertyInfo _gameEntityProperty;
        private static PropertyInfo _AnyActiveFormationTroopOnShipProperty;
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("MissionShipControlView").Method("OnObjectUsed"),
                    prefix: new HarmonyMethod(typeof(Patch_MissionShipControlView).GetMethod(nameof(Prefix_OnObjectUsed), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_OnObjectUsed(object __instance, Agent userAgent, UsableMissionObject usedObject)
        {
            // The only change: the original condition is !userAgent.IsMainAgent || ...
            if (!(userAgent.IsPlayerControlled || userAgent.IsMainAgent && Mission.Current.Mode == TaleWorlds.Core.MissionMode.Deployment) || !(usedObject is StandingPoint standingPoint))
                return false;
            var usableMachine = GetUsableMachineFromPoint(standingPoint);
            if (!_shipControllerMachineType.IsAssignableFrom(usableMachine.GetType()))
                return false;
            _attachedShipProperty ??= AccessTools.Property("NavalDLC.Missions.Objects.UsableMachines.ShipControllerMachine:AttachedShip");
            var attachedShip = _attachedShipProperty.GetValue(usableMachine);
            _AnyActiveFormationTroopOnShipProperty ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:AnyActiveFormationTroopOnShip");
            var anyActiveFormationTroopOnShip = (bool)_AnyActiveFormationTroopOnShipProperty.GetValue(attachedShip);
            if (!anyActiveFormationTroopOnShip)
                return false;

            _controllerMachineProperty ??= AccessTools.Property("NavalDLC.View.MissionViews.MissionShipControlView:ControllerMachine");
            _controllerMachineProperty.SetValue(__instance, usableMachine);
            _gameEntityProperty ??= AccessTools.Property("NavalDLC.Missions.Objects.UsableMachines.ShipControllerMachine:GameEntity");
            var gameEntity = (WeakGameEntity)_gameEntityProperty.GetValue(usableMachine);
            RangedSiegeWeapon familyDescending = gameEntity.Root.GetFirstScriptInFamilyDescending<RangedSiegeWeapon>();
            if (familyDescending == null)
                return false;
            _rangedSiegeWeaponProperty ??= AccessTools.Property("NavalDLC.View.MissionViews.MissionShipControlView:RangedSiegeWeapon");
            _rangedSiegeWeaponProperty.SetValue(__instance, familyDescending);
            return false;
        }
        private static UsableMachine GetUsableMachineFromPoint(StandingPoint standingPoint)
        {
            WeakGameEntity weakGameEntity = standingPoint.GameEntity;
            while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
                weakGameEntity = weakGameEntity.Parent;
            if (weakGameEntity.IsValid)
            {
                UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
                if (firstScriptOfType != null)
                    return firstScriptOfType;
            }
            return (UsableMachine)null;
        }
    }
}
