using System.Collections.Generic;
using MissionSharedLibrary.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch
{
	public class Patch_MissionScreen
	{
		public static bool OnMissionModeChange_Prefix(MissionScreen __instance, MissionMode oldMissionMode, bool atStart)
		{
			if (__instance.Mission.Mode == MissionMode.Battle && oldMissionMode == MissionMode.Deployment)
			{
				Utility.SmoothMoveToAgent(__instance, true);
				return false;
			}

			return true;
		}

		public static bool UpdateSceneTimeSpeed_Prefix(Mission __instance, ref List<Mission.TimeSpeedRequest> ____timeSpeedRequests)
		{
			bool flag = !(__instance.Scene != null);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				float num = 10f;
				int num2 = -1;
				for (int i = 0; i < ____timeSpeedRequests.Count; i++)
				{
					bool flag2 = ____timeSpeedRequests[i].RequestedTimeSpeed < num;
					if (flag2)
					{
						num = ____timeSpeedRequests[i].RequestedTimeSpeed;
						num2 = ____timeSpeedRequests[i].RequestID;
					}
				}
				bool flag3 = !__instance.Scene.TimeSpeed.ApproximatelyEqualsTo(num, 1E-05f);
				if (flag3)
				{
					bool flag4 = num2 != -1;
					if (flag4)
					{
						Debug.Print(string.Format("Updated mission time speed with request ID:{0}, time speed{1}", num2, num), 0, Debug.DebugColor.White, 17592186044416UL);
						__instance.Scene.TimeSpeed = num;
					}
					else
					{
						Debug.Print(string.Format("Reverted time speed back to default({0})", 1), 0, Debug.DebugColor.White, 17592186044416UL);
						__instance.Scene.TimeSpeed = 1f;
					}
				}
				result = false;
			}
			return result;
		}

		public static bool CheckCanBeOpened_Prefix(ref MissionOrderVM __instance, ref bool __result, bool displayMessage = false)
		{
			bool flag = Agent.Main == null;
			bool result;
			if (flag)
			{
				if (displayMessage)
				{
					InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=GMhOZGnb}Cannot issue order while dead.", null).ToString()));
				}
				__result = false;
				result = false;
			}
			else
			{
				bool flag2 = !Mission.Current.PlayerTeam.HasBots || !__instance.PlayerHasAnyTroopUnderThem || (!Mission.Current.PlayerTeam.IsPlayerGeneral && !Mission.Current.PlayerTeam.IsPlayerSergeant);
				if (flag2)
				{
					if (displayMessage)
					{
						InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=DQvGNQ0g}There isn't any unit under command.", null).ToString()));
					}
					__result = false;
					result = false;
				}
				else
				{
					bool isMissionEnding = Mission.Current.IsMissionEnding;
					if (isMissionEnding)
					{
						__result = Mission.Current.CheckIfBattleInRetreat();
						result = false;
					}
					else
					{
						__result = true;
						result = false;
					}
				}
			}
			return result;
		}
	}
}
