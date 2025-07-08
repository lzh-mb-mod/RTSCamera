using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Skills;
using RTSCamera.Config;
using System;
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
        public static float ScoutingSkillGainInterval { get; set; } = 10;
        public static float TacticsSkillGainInterval { get; set; } = 10;

        public static float ScoutingSkillGainFactor { get; set; } = 3f;

        public static float TacticsSkillGainFactor { get; set; } = 3f;

        public static bool ShouldLimitCameraDistance(Mission mission)
        {
            return  Campaign.Current != null && mission.Mode != MissionMode.Deployment &&
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
                    return explainedNumber;
                }
                if (Campaign.Current.MainParty == null)
                {
                    if (!(Game.Current?.PlayerTroop is CharacterObject) || Hero.MainHero?.CharacterObject == null)
                    {
                        explainedNumber.Add(1000f, GameTexts.FindText("str_rts_camera_no_main_hero"));
                        return explainedNumber;
                    }

                    AddSkillBonusForCharacter(DefaultSkills.Tactics, RTSCameraSkillEffects.TacticRTSCameraMaxDistance,
                        Hero.MainHero.CharacterObject, ref explainedNumber);
                    AddSkillBonusForCharacter(DefaultSkills.Scouting,
                        RTSCameraSkillEffects.ScoutingRTSCameraMaxDistance,
                        Hero.MainHero.CharacterObject, ref explainedNumber);
                    return explainedNumber;
                }

                AddSkillBonusForParty(DefaultSkills.Tactics, RTSCameraSkillEffects.TacticRTSCameraMaxDistance,
                    Campaign.Current.MainParty, ref explainedNumber);
                AddSkillBonusForParty(DefaultSkills.Scouting, RTSCameraSkillEffects.ScoutingRTSCameraMaxDistance,
                    Campaign.Current.MainParty, ref explainedNumber);
                SetCameraMaxDistance(explainedNumber.ResultNumber);
                return explainedNumber;
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
                return new ExplainedNumber();
            }
        }
        

        public static Hero GetHeroForTacticLevel()
        {
            return PartyBase.MainParty?.MobileParty?.GetEffectiveRoleHolder(SkillEffect.PerkRole.PartyLeader) ??
                   Hero.MainHero;
        }

        public static Hero GetHeroForScoutingLevel()
        {
            return PartyBase.MainParty?.MobileParty?.GetEffectiveRoleHolder(SkillEffect.PerkRole.Scout) ??
                   Hero.MainHero;
        }

        private static void AddToStat(
            ref ExplainedNumber stat,
            SkillEffect.EffectIncrementType effectIncrementType,
            float number,
            TextObject text)
        {
            if (effectIncrementType == SkillEffect.EffectIncrementType.Add)
            {
                stat.Add(number, text);
            }
            else
            {
                if (effectIncrementType != SkillEffect.EffectIncrementType.AddFactor)
                    return;
                stat.AddFactor(number * 0.01f, text);
            }
        }

        private static void AddSkillBonusForParty(
          SkillObject skill,
          SkillEffect skillEffect,
          MobileParty party,
          ref ExplainedNumber stat)
        {
            Hero leaderHero = party.LeaderHero;
            if (leaderHero == null || skillEffect == null)
                return;

            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.PartyLeader || skillEffect.SecondaryRole == SkillEffect.PerkRole.PartyLeader)
            {
                int skillValue = leaderHero.GetSkillValue(skill);
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.PartyLeader;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillValue, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillValue)
                    : skillEffect.GetSecondaryValue(skillValue);
                AddToStat(ref stat, skillEffect.IncrementType, effectValue, description);
            }
            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.Engineer || skillEffect.SecondaryRole == SkillEffect.PerkRole.Engineer)
            {
                Hero effectiveEngineer = party.EffectiveEngineer;
                if (effectiveEngineer != null)
                {
                    int skillValue = effectiveEngineer.GetSkillValue(skill);
                    bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Engineer;
                    var description = GetEffectDescriptionForSkillLevel(skillEffect, skillValue, isPrimaryRole);
                    float effectValue = isPrimaryRole
                        ? skillEffect.GetPrimaryValue(skillValue)
                        : skillEffect.GetSecondaryValue(skillValue);
                    AddToStat(ref stat, skillEffect.IncrementType, effectValue, description);
                }
            }
            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout || skillEffect.SecondaryRole == SkillEffect.PerkRole.Scout)
            {
                Hero effectiveScout = party.EffectiveScout;
                if (effectiveScout != null)
                {
                    int skillValue = effectiveScout.GetSkillValue(skill);
                    bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout;
                    var description = GetEffectDescriptionForSkillLevel(skillEffect, skillValue, isPrimaryRole);
                    float effectValue = isPrimaryRole
                        ? skillEffect.GetPrimaryValue(skillValue)
                        : skillEffect.GetSecondaryValue(skillValue);
                    AddToStat(ref stat, skillEffect.IncrementType, effectValue, description);
                }
            }
            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.Surgeon || skillEffect.SecondaryRole == SkillEffect.PerkRole.Surgeon)
            {
                Hero effectiveSurgeon = party.EffectiveSurgeon;
                if (effectiveSurgeon != null)
                {
                    int skillValue = effectiveSurgeon.GetSkillValue(skill);
                    bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout;
                    var description = GetEffectDescriptionForSkillLevel(skillEffect, skillValue, isPrimaryRole);
                    float effectValue = isPrimaryRole
                        ? skillEffect.GetPrimaryValue(skillValue)
                        : skillEffect.GetSecondaryValue(skillValue);
                    AddToStat(ref stat, skillEffect.IncrementType, effectValue, description);
                }
            }

            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.Quartermaster ||
                skillEffect.SecondaryRole == SkillEffect.PerkRole.Quartermaster)
            {
                Hero effectiveQuartermaster = party.EffectiveQuartermaster;
                if (effectiveQuartermaster != null)
                {
                    int skillValue = effectiveQuartermaster.GetSkillValue(skill);
                    bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout;
                    var description = GetEffectDescriptionForSkillLevel(skillEffect, skillValue, isPrimaryRole);
                    float effectValue = isPrimaryRole
                        ? skillEffect.GetPrimaryValue(skillValue)
                        : skillEffect.GetSecondaryValue(skillValue);
                    AddToStat(ref stat, skillEffect.IncrementType, effectValue, description);
                }
            }
        }

        private static void AddSkillBonusForCharacter(
          SkillObject skill,
          SkillEffect skillEffect,
          CharacterObject character,
          ref ExplainedNumber stat,
          int baseSkillOverride = -1,
          bool isBonusPositive = true,
          int extraSkillValue = 0)
        {
            int skillLevel = (baseSkillOverride >= 0 ? baseSkillOverride : character.GetSkillValue(skill)) + extraSkillValue;
            int sign = isBonusPositive ? 1 : -1;
            if (skillEffect.PrimaryRole == SkillEffect.PerkRole.Personal ||
                skillEffect.SecondaryRole == SkillEffect.PerkRole.Personal)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Personal;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }

            Hero heroObject = character.HeroObject;
            if (heroObject == null)
                return;
            if ((skillEffect.PrimaryRole == SkillEffect.PerkRole.Engineer ||
                 skillEffect.SecondaryRole == SkillEffect.PerkRole.Engineer) && character.IsHero &&
                heroObject.PartyBelongedTo?.EffectiveEngineer == heroObject)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Engineer;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }

            if ((skillEffect.PrimaryRole == SkillEffect.PerkRole.Quartermaster ||
                 skillEffect.SecondaryRole == SkillEffect.PerkRole.Quartermaster) && character.IsHero &&
                heroObject.PartyBelongedTo?.EffectiveQuartermaster == heroObject)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Quartermaster;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }

            if ((skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout ||
                 skillEffect.SecondaryRole == SkillEffect.PerkRole.Scout) && character.IsHero &&
                heroObject.PartyBelongedTo?.EffectiveScout == heroObject)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Scout;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }

            if ((skillEffect.PrimaryRole == SkillEffect.PerkRole.Surgeon ||
                 skillEffect.SecondaryRole == SkillEffect.PerkRole.Surgeon) && character.IsHero &&
                heroObject.PartyBelongedTo?.EffectiveSurgeon == heroObject)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Surgeon;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }

            if ((skillEffect.PrimaryRole == SkillEffect.PerkRole.PartyLeader ||
                 skillEffect.SecondaryRole == SkillEffect.PerkRole.PartyLeader) && character.IsHero &&
                heroObject.PartyBelongedTo?.LeaderHero == heroObject)
            {
                bool isPrimaryRole = skillEffect.PrimaryRole == SkillEffect.PerkRole.Surgeon;
                var description = GetEffectDescriptionForSkillLevel(skillEffect, skillLevel, isPrimaryRole);
                float effectValue = isPrimaryRole
                    ? skillEffect.GetPrimaryValue(skillLevel)
                    : skillEffect.GetSecondaryValue(skillLevel);
                AddToStat(ref stat, skillEffect.IncrementType, (float)sign * effectValue, description);
            }
        }
        private static TextObject GetEffectDescriptionForSkillLevel(SkillEffect effect, int level, bool isPrimaryRole)
        {
            var text = effect.Description.CopyTextObject();
            text.SetTextVariable("a0",
                isPrimaryRole
                    ? effect.GetPrimaryValue(level).ToString("0.0")
                    : effect.GetSecondaryValue(level).ToString("0.0"));
            return text;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
