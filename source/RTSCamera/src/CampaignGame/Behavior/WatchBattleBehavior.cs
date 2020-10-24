using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RTSCamera.CampaignGame.Behavior
{
    public class WatchBattleBehavior : CampaignBehaviorBase
    {
        public static bool WatchMode;

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, starter =>
                {
                    AddMenuOption(Game.Current, starter);
                });
        }

        private static void AddMenuOption(Game game, CampaignGameStarter gameStarter)
        {
            try
            {
                gameStarter.AddGameMenuOption("encounter", "rts_camera_watch_battle",
                    game.GameTextManager.FindText("str_rts_camera_watch_battle").ToString(), args =>
                    {
                        try
                        {
                            CheckEnemyAttackableHonorably(args);
                            return EncounterAttackCondition(args);
                        }
                        catch (Exception e)
                        {
                            Utility.DisplayMessage(e.ToString());
                        }

                        return false;
                    }, args =>
                    {
                        try
                        {
                            WatchMode = true;
                            MenuHelper.EncounterAttackConsequence(args);
                        }
                        catch (Exception e)
                        {
                            WatchMode = false;
                            Utility.DisplayMessage(e.ToString());
                        }
                    });

                gameStarter.AddGameMenuOption("menu_siege_strategies", "rts_camera_watch_battle",
                    game.GameTextManager.FindText("str_rts_camera_watch_battle").ToString(), WatchSiegeCondition, WatchSiegeConsequence);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }

        }

        private static bool WatchSiegeCondition(MenuCallbackArgs args)
        {
            try
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                if (MobileParty.MainParty.BesiegedSettlement == null || !Hero.MainHero.IsWounded)
                {
                    args.IsEnabled = false;
                    return false;
                }
                if (MobileParty.MainParty.BesiegedSettlement != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent.BesiegerCamp != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent.BesiegerCamp.BesiegerParty == MobileParty.MainParty)
                {
                    Settlement settlement = PlayerEncounter.EncounteredParty != null ? PlayerEncounter.EncounteredParty.Settlement : PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
                    if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=bCuxzp1N}You need to wait for the siege equipment to be prepared.");
                        return true;
                    }

                    args.IsEnabled = true;
                    args.Tooltip = GameTexts.FindText("str_rts_camera_watch_mode_tool_tip");
                    return true;
                }
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }

            return false;
        }

        private static void WatchSiegeConsequence(MenuCallbackArgs args)
        {
            try
            {
                if (PlayerEncounter.IsActive)
                    PlayerEncounter.LeaveEncounter = false;
                else
                    TaleWorlds.CampaignSystem.Campaign.Current.HandleSettlementEncounter(MobileParty.MainParty, PlayerSiege.PlayerSiegeEvent.BesiegedSettlement);
                WatchMode = true;
                GameMenu.SwitchToMenu("assault_town");
            }
            catch (Exception e)
            {
                WatchMode = false;
                Utility.DisplayMessage(e.ToString());
            }
        }

        private static readonly TextObject EnemyNotAttackableTooltip = new TextObject("{=oYUAuLO7}You have sworn not to attack.");

        private static void CheckEnemyAttackableHonorably(MenuCallbackArgs args)
        {
            if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty || PlayerEncounter.PlayerIsDefender)
                return;
            IFaction mapFaction;
            if (PlayerEncounter.EncounteredMobileParty != null)
            {
                mapFaction = PlayerEncounter.EncounteredMobileParty.MapFaction;
            }
            else
            {
                if (PlayerEncounter.EncounteredParty == null)
                    return;
                mapFaction = PlayerEncounter.EncounteredParty.MapFaction;
            }
            if (mapFaction == null || !mapFaction.NotAttackableByPlayerUntilTime.IsFuture)
                return;
            args.IsEnabled = false;
            args.Tooltip = EnemyNotAttackableTooltip;
        }

        private static bool EncounterAttackCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            if (MapEvent.PlayerMapEvent == null)
                return false;
            MapEvent battle = PlayerEncounter.Battle;
            Settlement mapEventSettlement = battle?.MapEventSettlement;
            return (battle == null || mapEventSettlement == null || !mapEventSettlement.IsFortification || !battle.IsSiegeAssault || PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete) && battle != null && (battle.HasTroopsOnBothSides() || battle.IsSiegeAssault) && MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide) != null && Hero.MainHero.IsWounded;
        }
    }
}
