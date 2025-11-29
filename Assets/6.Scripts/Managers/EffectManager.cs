using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 효과를 발동시키는 매니저 
/// </summary>
public class EffectManager : Singleton<EffectManager>
{
    public List<SO_BaseEffect> effects;

    private Dictionary<string, SO_BaseEffect> effectsDict = new(); 
    private Dictionary<GameObject, EffectComponent> activeEffects = new();

    protected override void Awake()
    {
        base.Awake();

        foreach (var effect in effects)
        {
            effectsDict.Add(effect.id, effect);
        }
    }
    

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

    public void RegisterEffect_Burn(GameObject owner, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Burn"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Burn", "", duration, tick, basePower);
        effect.FxObject = effectObj.vfxObject;
        effect.FxIcon = effectObj.icon;

        RegisterEffect(owner, appliedBy, effect);
    }

    public void RegisterEffect_Bleed(GameObject owner, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Bleed"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Bleed", "", duration, tick, basePower);
        effect.FxObject = effectObj.vfxObject;
        effect.FxIcon = effectObj.icon;

        RegisterEffect(owner, appliedBy, effect);
    }

    public void RegisterEffect_Poison(GameObject owner, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Poison"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Poison", "", duration, tick, basePower);
        effect.FxObject = effectObj.vfxObject;
        effect.FxIcon = effectObj.icon;

        RegisterEffect(owner, appliedBy, effect);
    }
}
