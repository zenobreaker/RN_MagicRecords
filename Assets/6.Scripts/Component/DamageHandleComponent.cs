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
            character.OnDamage(attacker, null, transform.position, damageEvent);
        else
            ApplyDamage(attacker, damageEvent);
    }

    internal bool ApplyDamage(GameObject attacker, DamageEvent damageEvent)
    {
        if (damageEvent == null || health == null || health.Dead) return false;

        OnDamaged?.Invoke();
        BattleManager.Instance.SafeInvoke(v => v.NotifyAttackHit(attacker, this.gameObject, damageEvent));
        
        float value = DamageCalculator.CalcDamage(status, damageEvent);

        if(!damageEvent.IsDOTEffect() && this.TryGetComponent<EffectComponent>(out var effectComp))
        {
            if(effectComp.HasEffect("Curse") is CurseEffect curse)
                value *= (1.0f + curse.GetDamageIncrease());
        }

        health.SafeInvoke(v => v.Damage(value));
        BattleManager.Instance.SafeInvoke(v => v.NotifyAttackHitFinish(attacker, this.gameObject, value));
        ShowDamageText(value, damageEvent);

        // Damage notification only. Character applies the resolved reaction after death checking.
        if (!damageEvent.IsDOTEffect()) OnDamagedEvent?.Invoke(damageEvent);
        return true;
    }

    private void ShowDamageText(float value, DamageEvent damageEvent)
    {
        Vector3 pos = transform.position + Vector3.up * dmgFontOffsetY;
        UIManager.Instance.SafeInvoke(v => v.DrawDamageText(pos, value, damageEvent));
    }
}
