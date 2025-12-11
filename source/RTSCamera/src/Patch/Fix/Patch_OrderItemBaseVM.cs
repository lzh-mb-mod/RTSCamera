using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_OrderItemBaseVM
    {
        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Fix the issue that order message may be shown twice in free camera mode.
                harmony.Patch(
                    typeof(OrderItemBaseVM).GetMethod("ExecuteAction",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_OrderItemBaseVM).GetMethod(nameof(Prefix_ExecuteAction),
                            BindingFlags.Static | BindingFlags.Public)));
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
        
        public static void Prefix_ExecuteAction()
        {
            var missionOrderVM = Utility.GetMissionOrderVM(Mission.Current);
            AccessTools.Property(typeof(MissionOrderVM), "DisplayedOrderMessageForLastOrder").SetValue(missionOrderVM, false);
        }

    }
}
