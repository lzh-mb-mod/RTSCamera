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
                DefaultSkills.Tactics, PartyRole.PartyLeader, 0.2f, EffectIncrementType.Add);

            _effectScoutingRTSCameraMaxDistance.Initialize(GameTexts.FindText("str_rts_camera_scouting_skill_effect_description"), 
                DefaultSkills.Scouting , PartyRole.Scout, 0.75f, EffectIncrementType.AddFactor);
        }
    }
}
