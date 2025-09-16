using Helpers;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Skills;
using RTSCamera.Config;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CampaignGame.Behavior
{
    public class RTSCameraSkillBehavior : CampaignBehaviorBase
    {
        public static float CameraDistanceMaxLimit { get; private set; } = -1;

        public static float CameraDistanceLimit { get; set; } = -1;
        public static float ScoutingSkillGainMaxDuration { get; set; } = 10;
        public static float TacticsSkillGainMaxDuration { get; set; } = 10;

        public static float ScoutingSkillGainFactor { get; set; } = 3f;

        public static float TacticsSkillGainFactor { get; set; } = 3f;

        public static bool ShouldLimitCameraDistance(Mission mission)
        {
            return  mission.Mode != MissionMode.Deployment &&
                RTSCameraConfig.Get().LimitCameraDistance && mission.MainAgent != null &&
                CameraDistanceLimit >= 0;
        }

        public static void UpdateCameraDistanceLimit(float limit)
        {
            CameraDistanceLimit = MathF.Clamp(limit, 0, CameraDistanceMaxLimit);
            RTSCameraConfig.Get().CameraDistanceLimitFactor = CameraDistanceLimit / CameraDistanceMaxLimit;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnHeroGainedSkill);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        }

        private void OnMissionStarted(IMission mission)
        {
            UpdateCameraMaxDistance();
        }

        private void OnHeroGainedSkill(Hero hero, SkillObject skill, int change, bool shouldShowNotify)
        {
            if (Mission.Current == null || Campaign.Current == null)
                return;

            if (hero == GetHeroForTacticLevel() || hero == GetHeroForScoutingLevel())
            {
                UpdateCameraMaxDistance();
            }
        }

        private static void SetCameraMaxDistance(float distance)
        {
            var factor = MathF.Clamp(RTSCameraConfig.Get().CameraDistanceLimitFactor, 0, 1);
            CameraDistanceMaxLimit = distance;
            CameraDistanceLimit = CameraDistanceMaxLimit * factor;
        }

        public static ExplainedNumber UpdateCameraMaxDistance(bool includeDescription = false)
        {
            try
            {
                var explainedNumber = new ExplainedNumber(0, includeDescription);
                explainedNumber.Add(10f, GameTexts.FindText("str_rts_camera_base_distance"));
                if (Campaign.Current == null)
                {
                    explainedNumber.Add(1000f, GameTexts.FindText("str_rts_camera_out_of_campaign"));
                }
                else if (Campaign.Current.MainParty == null)
                {
                    if (!(Game.Current?.PlayerTroop is CharacterObject) || Hero.MainHero?.CharacterObject == null)
                    {
                        explainedNumber.Add(1000f, GameTexts.FindText("str_rts_camera_no_main_hero"));
                    }
                    else
                    {
                        SkillHelper.AddSkillBonusForCharacter(RTSCameraSkillEffects.TacticRTSCameraMaxDistance,
                            Hero.MainHero.CharacterObject, ref explainedNumber);
                        SkillHelper.AddSkillBonusForCharacter(RTSCameraSkillEffects.ScoutingRTSCameraMaxDistance,
                            Hero.MainHero.CharacterObject, ref explainedNumber);
                    }
                }
                else
                {
                    SkillHelper.AddSkillBonusForParty(RTSCameraSkillEffects.TacticRTSCameraMaxDistance,
                        Campaign.Current.MainParty, ref explainedNumber);
                    SkillHelper.AddSkillBonusForParty(RTSCameraSkillEffects.ScoutingRTSCameraMaxDistance,
                        Campaign.Current.MainParty, ref explainedNumber);
                }

                SetCameraMaxDistance(explainedNumber.ResultNumber);
                return explainedNumber;
            }
            catch (System.Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                return new ExplainedNumber();
            }
        }
        

        public static Hero GetHeroForTacticLevel()
        {
            return PartyBase.MainParty?.MobileParty?.GetEffectiveRoleHolder(PartyRole.PartyLeader) ??
                   Hero.MainHero;
        }

        public static Hero GetHeroForScoutingLevel()
        {
            return PartyBase.MainParty?.MobileParty?.GetEffectiveRoleHolder(PartyRole.Scout) ??
                   Hero.MainHero;
        }

        private static void AddToStat(
            ref ExplainedNumber stat,
            EffectIncrementType effectIncrementType,
            float number,
            TextObject text)
        {
            if (effectIncrementType == EffectIncrementType.Add)
            {
                stat.Add(number, text);
            }
            else
            {
                if (effectIncrementType != EffectIncrementType.AddFactor)
                    return;
                stat.AddFactor(number * 0.01f, text);
            }
        }

        private static TextObject GetEffectDescriptionForSkillLevel(SkillEffect effect, int level)
        {
            var text = effect.Description.CopyTextObject();
            text.SetTextVariable("a0",
                effect.GetSkillEffectValue(level).ToString("0.0"));
            return text;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
