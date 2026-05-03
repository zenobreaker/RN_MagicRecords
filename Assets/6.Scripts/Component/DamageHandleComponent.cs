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
        if (damageEvent == null) return; 

        OnDamaged?.Invoke();
        BattleManager.Instance?.NotifyAttackHit(attacker, this.gameObject, damageEvent);
        
        float value = DamageCalculator.CalcDamage(status, damageEvent);

        if(!damageEvent.IsDOTEffect() && this.TryGetComponent<EffectComponent>(out var effectComp))
        {
            if(effectComp.HasEffect("Curse") is CurseEffect curse)
                value *= (1.0f + curse.GetDamageIncrease());
        }

        health?.Damage(value);
        BattleManager.Instance?.NotifyAttackHitFinish(attacker, this.gameObject, value);
        ShowDamageText(value, damageEvent);

        // 도트 딜이면 리액션(상태 변경) 없이 종료
        if(damageEvent.hitData.DamageType == DamageType.DOT_POISON ||
            damageEvent.hitData.DamageType == DamageType.DOT_BURN ||
            damageEvent.hitData.DamageType == DamageType.DOT_BLEED)
        {
            return; 
        }

        OnDamagedEvent?.Invoke(damageEvent);
    }

    private void ShowDamageText(float value, DamageEvent damageEvent)
    {
        Vector3 pos = transform.position + Vector3.up * dmgFontOffsetY;
        UIManager.Instance?.DrawDamageText(pos, value, damageEvent);
    }
}