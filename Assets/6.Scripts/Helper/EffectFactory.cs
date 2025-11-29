using System;
using UnityEngine;

public static class EffectFactory
{
    public static BaseEffect CreateStatBuffEffect(string id, float duration, StatModifier modifier, ModifierValueType valueType)
    {
        return new StatBuffEffect(id, duration, modifier, valueType);
    }

    public static BaseEffect CreateDotStatusEffect(string id, string desc, float duration, float tick , float power )
    {
        BaseEffect effect = id switch
        {
            "Burn" => new BurnEffect(id, desc, duration, tick, power),
            "Bleed" => new BleedEffect(id, desc, duration, tick, power),
            "Poison" => new PoisonEffect(id, desc, duration, tick, power),
            _ => null
        };
        return effect;
    }
}
