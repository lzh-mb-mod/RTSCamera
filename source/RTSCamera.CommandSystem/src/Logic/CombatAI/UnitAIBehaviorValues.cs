using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic.CombatAI
{
    public class UnitAIBehaviorValues
    {
        public static void SetUnitAIBehaviorWhenChargeToFormation(Agent unit)
        {
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 5f, 20f, 0.01f);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0, 7f, 0, 20f, 0);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 10f, 30f, 6f, 40f, 0.05f);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
            //unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.0f, 12f, 0.0f, 30f, 0.0f);

            unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3, 10, 5, 50, 12);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 7f, 10, 5, 20, 0.1f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.01f, 10, 5, 20, 15f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 11, 10, 10.7f, 60, 9);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.01f, 7, 5, 8, 15);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 0.5f, 12f, 0.6f, 30f, 0.4f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
        }
    }
}
