using HarmonyLib;
using Helpers;
using MissionSharedLibrary.Utilities;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera.CampaignGame.Behavior
{
    public class CommandBattleBehavior : CampaignBehaviorBase
    {
        public static bool CommandMode;

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, starter =>
                {
                    AddMenuOption(starter);
                });
        }

        // Use harmony to add menu option to "encounter" menu because the original way (through OnSessionLauncherEvent doesn't work any more)
        public static void Patch(Harmony harmony)
        {
            try
            {
                //harmony.Patch(
                //    typeof(EncounterGameMenuBehavior).GetMethod("AddGameMenus",
                //        BindingFlags.Instance | BindingFlags.NonPublic),
                //    postfix: new HarmonyMethod(typeof(WatchBattleBehavior).GetMethod(nameof(Postfix),
                //        BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        public static void Postfix(CampaignGameStarter gameSystemInitializer)
        {
            AddMenuOption(gameSystemInitializer);
        }


        private static void AddMenuOption(CampaignGameStarter gameStarter)
        {
            try
            {
                var commandBattleString = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_command_battle")?.ToString() ?? new TextObject("{=RTSCamera_command_battle}(RTS Camera)Command the battle.").ToString();
                gameStarter.AddGameMenuOption("encounter", "rts_camera_command_battle",
                    commandBattleString, args =>
                    {
                        try
                        {
                            // TODO: optimize this section of code, by referencing the latest official code
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
                            CommandMode = true;
                            MenuHelper.EncounterAttackConsequence(args);
                        }
                        catch (Exception e)
                        {
                            CommandMode = false;
                            Utility.DisplayMessage(e.ToString());
                        }
                    });

                gameStarter.AddGameMenuOption("menu_siege_strategies", "rts_camera_command_battle",
                    commandBattleString, CommandSiegeCondition, CommandSiegeConsequence);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }

        }

        // TODO: need update by referencing the latest official code
        private static bool CommandSiegeCondition(MenuCallbackArgs args)
        {
            try
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                if (MobileParty.MainParty.BesiegedSettlement == null || !Hero.MainHero.IsWounded)
                {
                    args.IsEnabled = false;
                    return false;
                }
                if (MobileParty.MainParty.BesiegedSettlement != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent.BesiegerCamp != null && MobileParty.MainParty.BesiegedSettlement.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
                {
                    Settlement settlement = PlayerEncounter.EncounteredParty != null ? PlayerEncounter.EncounteredParty.Settlement : PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
                    if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=bCuxzp1N}You need to wait for the siege equipment to be prepared.");
                        return true;
                    }

                    args.IsEnabled = true;
                    args.Tooltip = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_command_mode_tool_tip") ?? new TextObject("{=RTSCamera_command_mode_tool_tip}You are injured. You may be able to command your troops but you cannot directly participate in the battle.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }

            return false;
        }

        private static void CommandSiegeConsequence(MenuCallbackArgs args)
        {
            try
            {
                if (PlayerEncounter.IsActive)
                    PlayerEncounter.LeaveEncounter = false;
                else
                    EncounterManager.StartSettlementEncounter(MobileParty.MainParty, PlayerSiege.PlayerSiegeEvent.BesiegedSettlement);
                CommandMode = true;
                GameMenu.SwitchToMenu("assault_town");
            }
            catch (Exception e)
            {
                CommandMode = false;
                Utility.DisplayMessage(e.ToString());
            }
        }

        private static readonly TextObject EnemyNotAttackableTooltip = new TextObject("{=oYUAuLO7}You have sworn not to attack.");

        private static void CheckEnemyAttackableHonorably(MenuCallbackArgs args)
        {
            if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty || PlayerEncounter.PlayerIsDefender)
                return;
            IFaction mapFaction;
            // TODO: optimize this section of code, by looking at latest official code
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
