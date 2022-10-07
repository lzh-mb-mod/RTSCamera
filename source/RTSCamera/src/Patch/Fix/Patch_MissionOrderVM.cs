using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionOrderVM
    {
        private static readonly PropertyInfo LastSelectedOrderSetType =
            typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderSetType),
                BindingFlags.Instance | BindingFlags.Public);
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionOrderVM));
        private static bool _patched;

        public static bool AllowEscape = true;

        public static void Patch()
        {
            try
            {
                if (_patched)
                    return;
                _patched = true;

                Harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("CheckCanBeOpened",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_CheckCanBeOpened), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("AfterInitialize", BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_AfterInitialize),
                        BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionOrderVM).GetMethod(nameof(MissionOrderVM.OnEscape),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnEscape),
                        BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        public static bool Prefix_CheckCanBeOpened(MissionOrderVM __instance, bool displayMessage, ref bool __result)
        {
            if (Agent.Main != null && !Agent.Main.IsPlayerControlled && Mission.Current
                .GetMissionBehavior<RTSCameraLogic>().SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __result = true;
                return false;
            }

            return true;
        }

        public static void Postfix_AfterInitialize(MissionOrderVM __instance)
        {
            LastSelectedOrderSetType.SetValue(__instance, (object)OrderSetType.None);
            AllowEscape = true;
        }

        public static bool Prefix_OnEscape()
        {
            return AllowEscape;
        }
    }
}
