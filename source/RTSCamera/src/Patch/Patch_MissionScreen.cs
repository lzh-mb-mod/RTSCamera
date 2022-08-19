using System.Collections.Generic;
using MissionSharedLibrary.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.MountAndBlade.ViewModelCollection;

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
