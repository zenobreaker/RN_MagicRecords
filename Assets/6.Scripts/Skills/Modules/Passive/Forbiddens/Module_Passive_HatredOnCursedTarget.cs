using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Curse/저주 대상 증오 지속 피해")]
public sealed class Module_Passive_HatredOnCursedTarget : PassiveContextModule
{
    [Min(0f)] public float[] cooldownByLevel = { 12f, 10f, 8f };
    [Min(0f)] public float fallbackCooldown = 12f;
    [Min(0f)] public float duration = 4f;
    [Min(0f)] public float attackRatio = 0.2f;

    [NonSerialized] private float lastTriggerTime = float.NegativeInfinity;
    [NonSerialized] private int lastTriggerAttackID = -1;
    [NonSerialized] private bool hasTriggered;

    public Module_Passive_HatredOnCursedTarget() => triggerTime = PassiveTriggerTime.OnHit;

    public override void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent)
    {
        if (target == null || damageEvent == null) return;
        // 원본의 조건을 유지: EffectComponent가 있는 대상은 Curse 효과가 있어야 합니다.
        if (target.TryGetComponent(out EffectComponent effects) &&
            (effects == null || effects.HasEffect("Curse") == null)) return;

        float cooldown = GetLevelValue(cooldownByLevel, fallbackCooldown);
        bool ready = Time.time >= lastTriggerTime + cooldown;
        bool sameAttack = hasTriggered && damageEvent.AttackInstanceID == lastTriggerAttackID;
        if (!ready && !sameAttack) return;
        if (ready)
        {
            lastTriggerTime = Time.time;
            lastTriggerAttackID = damageEvent.AttackInstanceID;
            hasTriggered = true;
        }

        float power = 0f;
        if (attacker != null && attacker.TryGetComponent(out StatusComponent status))
            power = status.GetStatusValue(StatusType.ATTACK) * attackRatio;
        EffectManager.Instance?.RegisterEffect_Hatred(target, attacker, duration, power);
    }

    public override void OnLose()
    {
        base.OnLose();
        lastTriggerTime = float.NegativeInfinity;
        lastTriggerAttackID = -1;
        hasTriggered = false;
    }
}
