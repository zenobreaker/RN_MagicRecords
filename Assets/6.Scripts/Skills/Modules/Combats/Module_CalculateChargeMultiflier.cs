using UnityEngine;

[ModuleCategory("Utility/Calculate Charge Multiplier")]
public class Module_CalculateChargeMultiplier : SkillModule
{
    [Tooltip("X축: 차지 시간, Y축: 데미지 배율")]
    public AnimationCurve damageCurve = AnimationCurve.Linear(0f, 1f, 3f, 2f);

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        float chargeTime = 0f;
        chargeTime = (float)(skill?.Runtime.Cast.ChargedTime);

        // 커브로 배율 계산
        float multiplier = damageCurve.Evaluate(chargeTime);
        skill.Runtime.Combat.BonusMultipiler = multiplier;
    }
}