using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 효과를 발동시키는 매니저 
/// </summary>
public class EffectManager : Singleton<EffectManager>
{
    private Dictionary<GameObject, EffectComponent> activeEffects = new();

    public void RegisterEffect(GameObject owner, GameObject appliedBy, BaseEffect effect)
    {
        if (!activeEffects.ContainsKey(owner))
        {
            var buff = owner.GetComponent<EffectComponent>();
            activeEffects[owner] = buff;

            if (owner.TryGetComponent<Character>(out var character))
            {
                character.OnDead += UnregisterAllEffects;
            }
        }
        
        activeEffects[owner]?.ApplyEffect(effect, owner, appliedBy);
    }

    public void UnregisterEffect(Character target, BaseEffect effect)
    {
        if (!activeEffects.ContainsKey(target)) return; 

        activeEffects[target].RemoveEffect(effect);
       // effect.Remove(target);
    }

    public void UnregisterAllEffects(Character target)
    {
        if (activeEffects.ContainsKey(target))
        {
            activeEffects.Remove(target);
        }
    }
}
