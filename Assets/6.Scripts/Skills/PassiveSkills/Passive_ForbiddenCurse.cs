using UnityEngine;

/// <summary>
///  패시브 - 금지된 마술 - 저주 
///  피해를 입힌 대상에게 저주 부여 
///  공격력과 치명타 확률 증가 
/// </summary>
public class Passive_ForbiddenCurse : PassiveSkill
{
    // 1. 정적 효과 
    private float atkBonus = 10.0f;
    private float critRateBonus = 0.05f;

    // 2. 저주 설정 
    private float curseChance = 1.0f;
    private float curseDuration = 5.0f;

    public Passive_ForbiddenCurse(SO_SkillData data)
        : base(data)
    {
    }

    public Passive_ForbiddenCurse(int skillID, string skillName, string skillDesc, Sprite skillIcon)
        : base(skillID, skillName, skillDesc, skillIcon)
    {
    }

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        status?.ApplyBuff(new StatModifier(StatusType.ATTACK, atkBonus, ModifierValueType.FIXED));
        status?.ApplyBuff(new StatModifier(StatusType.CRIT_RATIO, critRateBonus, ModifierValueType.FIXED));
        Debug.Log($" Crit Ratio : {status?.GetStatusValue(StatusType.CRIT_RATIO)}");
    }

    public override void OnAcquire(GameObject owner)
    {
        this.owner = owner; 

        if (BattleManager.Instance == null) return;

        BattleManager.Instance.OnAnyAttackHit -= OnTargetHit;
        BattleManager.Instance.OnAnyAttackHit += OnTargetHit;
    }

    private void OnTargetHit(GameObject attacker, GameObject target, DamageEvent evt)
    {
        if (Random.value > curseChance) return;

        // 타겟에게 저주 상태이상 부여
        EffectManager.Instance?.RegisterEffect_Curse(target, owner, curseDuration);
    }
}
