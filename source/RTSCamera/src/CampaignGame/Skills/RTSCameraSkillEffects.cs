using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RTSCamera.CampaignGame.Skills
{
    public class RTSCameraSkillEffects
    {
        private SkillEffect _effectTacticsRTSCameraMaxDistance;
        private SkillEffect _effectScoutingRTSCameraMaxDistance;

        public static RTSCameraSkillEffects Instance { get; private set; }

        public static SkillEffect TacticRTSCameraMaxDistance => Instance._effectTacticsRTSCameraMaxDistance;

        public static SkillEffect ScoutingRTSCameraMaxDistance => Instance._effectScoutingRTSCameraMaxDistance;

        public static void Initialize()
        {
            Instance = new RTSCameraSkillEffects();
        }

        public RTSCameraSkillEffects() => this.RegisterAll();

        private void RegisterAll()
        {
            _effectTacticsRTSCameraMaxDistance = this.Create("TacticsRTSCameraMaxDistance");
            _effectScoutingRTSCameraMaxDistance = this.Create("ScoutingRTSCameraMaxDistance");
            InitializeAll();
        }
        private SkillEffect Create(string stringId) => Game.Current.ObjectManager.RegisterPresumedObject<SkillEffect>(new SkillEffect(stringId));

        private void InitializeAll()
        {
            _effectTacticsRTSCameraMaxDistance.Initialize(GameTexts.FindText("str_rts_camera_tactics_skill_effect_description"),
                new SkillObject[1]
                {
                    DefaultSkills.Tactics
                }, SkillEffect.PerkRole.PartyLeader, 0.2f,
                SkillEffect.PerkRole.Personal, 0.15f, SkillEffect.EffectIncrementType.Add);

            _effectScoutingRTSCameraMaxDistance.Initialize(GameTexts.FindText("str_rts_camera_scouting_skill_effect_description"),
                new SkillObject[1]
                {
                    DefaultSkills.Scouting
                }, SkillEffect.PerkRole.Scout, 0.75f,
                SkillEffect.PerkRole.Personal, 0.6f, SkillEffect.EffectIncrementType.AddFactor);
        }
    }
}
