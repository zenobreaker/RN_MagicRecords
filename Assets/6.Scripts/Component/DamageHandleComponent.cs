using System;
using UnityEngine;

public class DamageHandleComponent : MonoBehaviour
{
    public Action OnDamaged;
    [SerializeField] private float dmgFontOffsetY = 1.5f;

    private Character character;
    private HealthPointComponent health;
    private StatusComponent status;

    public Action<DamageEvent> OnDamagedEvent;

    private void Awake()
    {
        character = GetComponent<Character>();
        health = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();
    }

    public void OnDamage(GameObject attacker, DamageEvent damageEvent)
    {
        // DOT and legacy callers use the same character death/reaction path.
        if (character != null)
        {
            character.OnDamage(attacker, null, transform.position, damageEvent);
        }
        else
        {
            ApplyDamage(attacker, damageEvent);
        }
    }

    internal bool ApplyDamage(GameObject attacker, DamageEvent damageEvent)
    {
        if (damageEvent == null || health == null || health.Dead)
        {
            return false;
        }

        OnDamaged?.Invoke();
        BattleManager.Instance.SafeInvoke(v => v.NotifyAttackHit(attacker, this.gameObject, damageEvent));

        float value = DamageCalculator.CalcDamage(status, damageEvent);

        if (damageEvent.IsDOTEffect() == false && this.TryGetComponent<EffectComponent>(out var effectComp))
        {
            if (status.HasDebuffImmunity == false && effectComp.HasEffect("Curse") is CurseEffect curse)
            {
                value *= (1.0f + curse.GetDamageIncrease());
            }
        }

        float previousHealth = health.GetCurrentHP;
        StatusComponent attackerStatus = attacker != null ? attacker.GetComponent<StatusComponent>() : null;
        value *= Mathf.Max(0f, 1f + (attackerStatus != null ? attackerStatus.GetStatusValue(StatusType.DAMAGE_BONUS) : 0f));
        value *= Mathf.Max(0f, 1f + status.GetStatusValue(StatusType.INCOMING_DAMAGE_BONUS));
        value *= Mathf.Clamp01(1f - status.GetStatusValue(StatusType.DAMAGE_REDUCTION));
        if (status.HasDebuffImmunity && (damageEvent.IsDOTEffect() || damageEvent.IsStatusEffectDamage))
        {
            value = 0f;
        }
        if (TryGetComponent<EffectComponent>(out var shields))
        {
            foreach (var effect in new System.Collections.Generic.List<BaseEffect>(shields.ActiveEffects))
            {
                if (effect is RecordShieldEffect shield)
                {
                    value = shield.Absorb(value);
                }
            }
        }
        health.SafeInvoke(v => v.Damage(value));
        damageEvent.KilledTarget = previousHealth > 0f && health.GetCurrentHP <= 0f;
        if (attacker != null && attacker.TryGetComponent<ManaComponent>(out var mana))
        {
            mana.RecoverOnHit(damageEvent, previousHealth - health.GetCurrentHP, gameObject);
        }

        BattleManager.Instance.SafeInvoke(v => v.NotifyAttackHitFinish(attacker, this.gameObject, previousHealth - health.GetCurrentHP, damageEvent));
        ShowDamageText(value, damageEvent);

        // Damage notification only. Character applies the resolved reaction after death checking.
        if (damageEvent.IsDOTEffect() == false)
        {
            OnDamagedEvent?.Invoke(damageEvent);
        }

        return true;
    }

    private void ShowDamageText(float value, DamageEvent damageEvent)
    {
        Vector3 pos = transform.position + Vector3.up * dmgFontOffsetY;
        UIManager.Instance.SafeInvoke(v => v.DrawDamageText(pos, value, damageEvent));
    }
}
