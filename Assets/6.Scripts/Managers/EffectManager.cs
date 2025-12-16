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
    

    public void RegisterEffect(GameObject target, GameObject appliedBy, BaseEffect effect)
    {
        if (!activeEffects.ContainsKey(target))
        {
            var buff = target.GetComponent<EffectComponent>();
            activeEffects[target] = buff;

            if (target.TryGetComponent<Character>(out var character))
            {
                character.OnDead += UnregisterAllEffects;
            }
        }

        activeEffects[target]?.ApplyEffect(effect, target, appliedBy);
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

    public void RegisterEffect_Burn(GameObject target, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Burn"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Burn", "", duration, effectObj, tick, basePower);

        RegisterEffect(target, appliedBy, effect);
    }

    public void RegisterEffect_Bleed(GameObject target, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Bleed"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Bleed", "", duration, effectObj, tick, basePower);

        RegisterEffect(target, appliedBy, effect);
    }

    public void RegisterEffect_Poison(GameObject target, GameObject appliedBy, float duration, float basePower)
    {
        var effectObj = effectsDict["Poison"];
        if (effectObj == null) return;

        float tick = effectObj.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Poison", "", duration, effectObj, tick, basePower);

        RegisterEffect(target, appliedBy, effect);
    }

    // 저주 상태이상을 등록하는 메서드
    public void RegisterEffect_Curse(GameObject target, GameObject appliedBy, float duration)
    {
        // 1. 데이터(SO_BaseEffect) 조회 (만약 저주도 SO로 관리한다면)
        var effectObj = effectsDict["Curse"];
        if (effectObj == null) return;

        // 2. CurseEffect 객체 생성 (BaseEffect를 상속받은 CurseEffect 클래스를 인스턴스화)
        BaseEffect effect = EffectFactory.CreateEffect("Curse", "", duration, effectObj);
        
        // 3. 중앙 등록 함수 호출
        RegisterEffect(target, appliedBy, effect);
    }

    public void RegisterEffect_Hatred(GameObject target, GameObject appliedBy, float duration, float basePower)
    {
        if (effectsDict.ContainsKey("Curse_Hatred") == false)
            return;

        SO_BaseEffect so = effectsDict["Curse_Hatred"];
        float tick = so.tickInterval;
        BaseEffect effect = EffectFactory.CreateDotStatusEffect("Curse_Hatred", "", duration,
            so, tick, basePower);

        RegisterEffect(target, appliedBy, effect);
    }

}
