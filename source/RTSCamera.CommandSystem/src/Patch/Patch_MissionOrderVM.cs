using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config.HotKey;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionOrderVM
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("ApplySelectedOrder",
                        BindingFlags.Public | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_ApplySelectedOrder), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
        }

        public static bool Prefix_ApplySelectedOrder(MissionOrderVM __instance, bool displayMessage, ref bool __result)
        {
            if (!(ScreenManager.TopScreen is MissionScreen missionScreen))
                return true;

            var input = missionScreen.SceneLayer.Input;
            if (CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder(input))
            {
                return false;
            }

            return true;
        }
    }
}
