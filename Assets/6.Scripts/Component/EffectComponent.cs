using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectComponent : MonoBehaviour
{
    [SerializeField]
    private float tickInterval = 0.1f;

    private Character owner;

    private Dictionary<string, BaseEffect> activeEffects= new Dictionary<string, BaseEffect>();
    private List<BaseEffect> expiredEffects = new();

    private void Awake()
    {
        owner = GetComponent<Character>();
        Debug.Assert(owner != null);
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

    public void ApplyEffect(BaseEffect newEffect, GameObject owner, GameObject appliedBy)
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
        }
        else
        {
            activeEffects.Add(newEffect.ID, newEffect);
            //newEffect.TickInterval = tickInterval;
            newEffect.OnApply(owner, appliedBy);
        }
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
            activeEffects.Remove(buffID);
        }
    }
}
