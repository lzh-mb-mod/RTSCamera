using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace EnhancedMission
{
    public class AgentStatModel
    {
        public static void SetAgentAIStat(Agent agent, AgentDrivenProperties agentDrivenProperties, int combatAI)
        {
            if (!agent.IsHuman)
                return;
            float num1 = combatAI / 100f;
            float num2 = combatAI / 100f;
            float amount = MBMath.ClampFloat(num1, 0.0f, 1f);
            float num3 = MBMath.ClampFloat(num2, 0.0f, 1f);
            agentDrivenProperties.AiRangedHorsebackMissileRange = (float)(0.300000011920929 + 0.400000005960464 * (double)num3);
            agentDrivenProperties.AiFacingMissileWatch = (float)((double)amount * 0.0599999986588955 - 0.959999978542328);
            agentDrivenProperties.AiFlyingMissileCheckRadius = (float)(8.0 - 6.0 * (double)amount);
            agentDrivenProperties.AiShootFreq = (float)(0.200000002980232 + 0.800000011920929 * (double)num3);
            agentDrivenProperties.AiWaitBeforeShootFactor = agent._propertyModifiers.resetAiWaitBeforeShootFactor ? 0.0f : (float)(1.0 - 0.5 * (double)num3);
            agentDrivenProperties.AIBlockOnDecideAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)((Math.Pow((double)MBMath.Lerp(-10f, 10f, amount, 1E-05f), 3.0) + 1000.0) * 0.000500000023748726), 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AiTryChamberAttackOnDecide = (float)(((double)amount - 0.150000005960464) * 0.100000001490116);
            agentDrivenProperties.AIAttackOnParryChance = 0.3f;
            agentDrivenProperties.AiAttackOnParryTiming = (float)(0.300000011920929 * (double)amount - 0.200000002980232);
            agentDrivenProperties.AIDecideOnAttackChance = 0.0f;
            agentDrivenProperties.AIParryOnAttackAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f);
            agentDrivenProperties.AiKick = (float)(((double)amount > 0.400000005960464 ? 0.400000005960464 : (double)amount) - 0.100000001490116);
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor = amount;
            agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = (float)(-0.25 * (1.0 - (double)amount));
            agentDrivenProperties.AiDecideOnAttackContinueAction = (float)(-0.5 * (1.0 - (double)amount));
            agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * amount;
            agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 4.0) * 0.0001f, 0.0f, 1f), 1E-05f);
            agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 5.0) * 1E-05f, 0.0f, 1f);
            agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = 0.5f * MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 5.0) * 1E-05f, 0.0f, 1f);
            agentDrivenProperties.AiAttackingShieldDefenseChance = (float)(0.200000002980232 + 0.300000011920929 * (double)amount);
            agentDrivenProperties.AiAttackingShieldDefenseTimer = (float)(0.300000011920929 * (double)amount - 0.300000011920929);
            agentDrivenProperties.AiRandomizedDefendDirectionChance = (float)(1.0 - Math.Log((double)amount * 7.0 + 1.0, 2.0) * 0.333330005407333);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.ClampFloat((float)Math.Pow((double)MBMath.Lerp(0.0f, 10f, amount, 1E-05f), 2.0) * 0.01f, 0.05f, 0.95f);
            agentDrivenProperties.AiRaiseShieldDelayTimeBase = (float)(0.5 * (double)amount - 0.75);
            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = (float)(0.100000001490116 + (double)amount * 0.200000002980232);
            agentDrivenProperties.AiCheckMovementIntervalFactor = (float)(0.00499999988824129 * (1.0 - (double)amount));
            agentDrivenProperties.AiMovemetDelayFactor = (float)(4.0 / (3.0 + (double)amount));
            agentDrivenProperties.AiParryDecisionChangeValue = (float)(0.0500000007450581 + 0.699999988079071 * (double)amount);
            agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = (float)(0.300000011920929 + 0.699999988079071 * (double)amount);
            agentDrivenProperties.AiMoveEnemySideTimeValue = (float)(0.5 * (double)amount - 2.5);
            agentDrivenProperties.AiMinimumDistanceToContinueFactor = (float)(2.0 + 0.300000011920929 * (3.0 - (double)amount));
            agentDrivenProperties.AiStandGroundTimerValue = (float)(0.5 * ((double)amount - 1.0));
            agentDrivenProperties.AiStandGroundTimerMoveAlongValue = (float)(0.5 * (double)amount - 1.0);
            agentDrivenProperties.AiHearingDistanceFactor = 1f + amount;
            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = (float)(1.5 * (3.0 - (double)amount));
            float num4 = 1f - MBMath.ClampFloat(0.004f * (float)agent.Character.GetSkillValue(DefaultSkills.Bow), 0.0f, 0.99f);
            agentDrivenProperties.AiRangerLeadErrorMin = num4 * 0.2f;
            agentDrivenProperties.AiRangerLeadErrorMax = num4 * 0.3f;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier = num4 * 0.1f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier = num4 * ((float)Math.PI / 90f);
            agentDrivenProperties.AIAttackOnDecideChance = AgentStatCalculateModel.CalculateAIAttackOnDecideMaxValue;
        }

        public static void SetUseRealisticBlocking(AgentDrivenProperties agentDrivenProperties, bool useRealisticBlocking)
        {
            agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, useRealisticBlocking ? 1f : 0.0f);
        }
    }
}
