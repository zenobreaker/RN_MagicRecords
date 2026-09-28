using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectComponent : MonoBehaviour
{
    public int DebuffCount { get; private set; } = 0;

    private Character owner;

    private Dictionary<string, BaseEffect> activeEffects = new Dictionary<string, BaseEffect>();
    private List<BaseEffect> expiredEffects = new();
    private readonly List<BaseEffect> updatingEffects = new();

    private SO_HUDHandler handler;
    private StatusEffectComponent statusEffect;

    private void Awake()
    {
        owner = GetComponent<Character>();
        Debug.Assert(owner != null);

        statusEffect = owner.GetComponent<StatusEffectComponent>();

        if (owner is Player || (owner is Enemy enemy && enemy.Boss))
            handler = Resources.Load<SO_HUDHandler>("SO_HUDHandler");
    }

    private void Update()
    {
        if (activeEffects.Count == 0) return;

        expiredEffects.Clear();
        updatingEffects.Clear();
        updatingEffects.AddRange(activeEffects.Values);

        foreach (BaseEffect effect in updatingEffects)
        {
            if (HasEffect(effect.ID) != effect) continue;
            if (effect.IsExpired == false)
                effect.Update(Time.deltaTime);

            if (effect.IsExpired)
                expiredEffects.Add(effect);
        }

        for (int i = expiredEffects.Count - 1; i >= 0; --i)
        {
            BaseEffect effect = expiredEffects[i];
            RemoveEffect(effect);
        }
    }

    public void ApplyEffect(BaseEffect newEffect, GameObject target, GameObject appliedBy)
    {
        if (newEffect == null) return;

        if (activeEffects.TryGetValue(newEffect.ID, out var existingEffect))
        {
            switch (existingEffect.StackPolicy)
            {
                case BuffStackPolicy.REFRESH_ONLY:
                    existingEffect.ResetDuration();
                    break;
                case BuffStackPolicy.STACKABLE:
                    existingEffect.AddStack();
                    break;
                case BuffStackPolicy.IGNOREIFEXSIST:
                    return;
            }
            newEffect = existingEffect;
        }
        else
        {
            if (newEffect.Type == EffectType.DEBUFF)
                DebuffCount++;

            activeEffects.Add(newEffect.ID, newEffect);
            newEffect.OnApply(target, appliedBy);
        }

        NotifyEffectUI(newEffect);
    }

    public bool TryConsumeStacks(string effectID, int amount)
    {
        var effect = HasEffect(effectID);
        if (effect == null || !effect.TryConsumeStacks(amount)) return false;

        if (effect.StackCount == 0) RemoveEffect(effect);
        else NotifyEffectUI(effect);
        return true;
    }

    private void NotifyEffectUI(BaseEffect newEffect)
    {
        if (owner is Player)
            handler.SafeInvoke(ui => ui.OnApplyEffect(newEffect));
        else if (owner is Enemy enemy && enemy.Boss)
            handler.SafeInvoke(ui => ui.OnChangedBossEffect(owner, newEffect));
    }

    public void RemoveEffect(BaseEffect effect)
    {
        if (effect == null || HasEffect(effect.ID) != effect) return;

        RemoveEffect(effect.ID);
    }

    public void RemoveEffect(string buffID)
    {
        if (string.IsNullOrEmpty(buffID)) return;

        if (activeEffects.TryGetValue(buffID, out BaseEffect baseEffect))
        {
            activeEffects.Remove(buffID);
            if (baseEffect.Type == EffectType.DEBUFF) DebuffCount--;
            baseEffect.OnRemove();

            // 상태 플래그 동기화 처리
            if (baseEffect is CrowdControlEffect cc)
            {
                SynchronizeStatusFlag(cc.EffectFlag, isAdding: false);
            }


        }
    }

    public void ClearEffects()
    {
        foreach (var effect in new List<BaseEffect>(activeEffects.Values))
            RemoveEffect(effect);
    }

    private void OnDisable()
    {
        ClearEffects();
        EffectManager.Instance.SafeInvoke(manager => manager.UnregisterAllEffects(owner));
    }

    public BaseEffect HasEffect(string effectName)
    {
        if (activeEffects.TryGetValue(effectName, out BaseEffect value))
            return value;
        else
            return null;
    }

    private void SynchronizeStatusFlag(StatusEffectType type, bool isAdding)
    {
        if (statusEffect == null) return;

        if (isAdding)
            statusEffect.AddStatusEffect(type);
        else
            statusEffect.RemoveStatusEffect(type);
    }
}
