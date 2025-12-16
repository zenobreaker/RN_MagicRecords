using UnityEngine;


/// <summary>
/// 패시브 금기-파멸하는 격노
/// 치명타 피해량 증가 
/// 대상이 가진 상태이상/디버프 수 만큼 피해량 증가 
/// </summary>
public class Passive_RuinousWrath : PassiveSkill
{
    private float critDamageBonus = 0.1f;
    private float damageAmpPerEffect = 0.1f; // 디버프 당 피해 증가량
    private StatusComponent status;
    private StatModifier critDmgModifier;

    public Passive_RuinousWrath(SO_SkillData data) : base(data)
    {
    }

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        this.status = status;
        status?.ApplyBuff(critDmgModifier = ModifierFactory.CreateStatModifier(StatusType.CRIT_DMG, critDamageBonus, ModifierValueType.FIXED));
    }

    public override void OnAcquire(GameObject owner)
    {
        this.owner = owner;

        CalcLevel();
        if (BattleManager.Instance == null) return;

        BattleManager.Instance.OnAnyAttackHit -= OnTargetHit;
        BattleManager.Instance.OnAnyAttackHit += OnTargetHit;
    }

    public override void OnLose()
    {
        status?.RemoveBuff(critDmgModifier);
    }

    private void CalcLevel()
    {
        critDamageBonus = skillLevel switch
        {
            1 => 0.1f,
            2 => 0.15f,
            3 => 0.2f,
            _ => 0.1f
        };
    }

    private void OnTargetHit(GameObject attacker, GameObject target, DamageEvent evt)
    {
        if (target == null) return; 
        if (attacker == null) return;

        if (target.TryGetComponent<EffectComponent>(out var effect)) 
        {
            // 대상이 가진 디버프/상태이상의 개수 가져오기 
            int debuffCount = effect.DebuffCount;

            evt.DamageAmp = 1.0f + (debuffCount * damageAmpPerEffect);
        }
    }
}
