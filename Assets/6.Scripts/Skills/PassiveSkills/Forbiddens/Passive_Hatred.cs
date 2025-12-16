using UnityEngine;

/// <summary>
/// 패시브 - 금기-가증스러운 증오
/// 피해를 입은 대상에게 4초 동안 DoT 데미지 부여
/// 피해량은 입힌 피해의 
/// </summary>
public class Passive_AbominableHatred : PassiveSkill
{
    private float cooldown = 12.0f;
    private float duration = 4.0f;

    private float lastTriggerTime;
    private int lastTriggerAttackID = -1;

    public Passive_AbominableHatred(SO_SkillData data) : base(data)
    {
        lastTriggerTime = cooldown * -1.0f;
    }


    public override void OnAcquire(GameObject owner)
    {
        this.owner = owner;

        if (BattleManager.Instance == null) return;

        BattleManager.Instance.OnAnyAttackHit -= OnTargetHit;
        BattleManager.Instance.OnAnyAttackHit += OnTargetHit;

        CalcCooldown();
    }

    private void CalcCooldown()
    {
        cooldown = skillLevel switch
        {
            1 => 12.0f,
            2 => 10.0f,
            3 => 8.0f,
            _ => 12.0f
        };
    }

    private float CalcPower(GameObject attacker)
    {
        if (attacker == null) return 0; 

        if(attacker.TryGetComponent(out StatusComponent status))
        {
            float attack = status.GetStatusValue(StatusType.ATTACK);
            return attack * 0.2f;
        }

        return 0.0f; 
    }

    private void OnTargetHit(GameObject attacker, GameObject target, DamageEvent evt)
    {
        // 저주가 부여된 적에게만 발동된다.
        if(target.TryGetComponent(out EffectComponent effect))
        {
            if (effect == null) return;
            if (effect.HasEffect("Curse") == null) return; 
        }

        bool isReady = Time.time >= lastTriggerTime + cooldown;

        bool isSameAttack = (evt.AttackInstanceID == lastTriggerAttackID);

        if (isReady || isSameAttack)
        {
            if(isReady)
            {
                lastTriggerTime = Time.time;
                lastTriggerAttackID = evt.AttackInstanceID;
            }

            EffectManager.Instance.RegisterEffect_Hatred(target, attacker, duration, CalcPower(attacker));
        }

    }
}
