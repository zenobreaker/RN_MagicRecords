using UnityEngine;

[ModuleCategory("Utility/Calculate Charge Multiplier")]
public class Module_CalculateChargeMultiplier : SkillModule
{
    [Tooltip("X축: 차지 시간, Y축: 데미지 배율")]
    public AnimationCurve damageCurve = AnimationCurve.Linear(0f, 1f, 3f, 2f);

    [Tooltip("계산된 배율을 블랙보드에 저장할 키 이름")]
    public string resultKeyName = "DamageMultiplier";

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        float chargeTime = 0f;
        if (skill.Blackboard.TryGetValue("ChargedTime", out object timeObj))
        {
            chargeTime = (float)timeObj;
        }

        // 커브로 배율 계산
        float multiplier = damageCurve.Evaluate(chargeTime);

        // 💡 계산된 결과를 블랙보드에 꽂아둡니다!
        skill.Blackboard[resultKeyName] = multiplier;
    }
}