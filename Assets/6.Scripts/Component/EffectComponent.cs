using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectComponent : MonoBehaviour
{
    private Character owner;

    private Dictionary<string, BaseEffect> activeEffects = new Dictionary<string, BaseEffect>();
    private List<BaseEffect> expiredEffects = new();

    private SO_HUDHandler handler;
    private StatusEffectComponent statusEffect;

    private void Awake()
    {
        owner = GetComponent<Character>();
        Debug.Assert(owner != null);

        statusEffect = owner.GetComponent<StatusEffectComponent>();

        if(owner is Player)
            handler = Resources.Load<SO_HUDHandler>("SO_HUDHandler");
    }

    private void Update()
    {
        if (activeEffects.Count == 0) return;

        expiredEffects.Clear();

        foreach (BaseEffect effect in activeEffects.Values)
        {
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
            activeEffects.Add(newEffect.ID, newEffect);
            newEffect.OnApply(target, appliedBy);
        }

        handler?.OnApplyEffect(newEffect);
    }

    public void RemoveEffect(BaseEffect effect)
    {
        if (effect == null) return;
        RemoveEffect(effect.ID);
    }

    public void RemoveEffect(string buffID)
    {
        if (string.IsNullOrEmpty(buffID)) return;

        if (activeEffects.TryGetValue(buffID, out BaseEffect baseEffect))
        {
            baseEffect.OnRemove();

            // 상태 플래그 동기화 처리
            if(baseEffect is CrowdControlEffect cc)
            {
                SynchronizeStatusFlag(cc.EffectFlag, isAdding: false);
            }


            activeEffects.Remove(buffID);
        }
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