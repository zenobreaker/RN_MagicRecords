using System;
using UnityEngine;

public static class EffectFactory
{
    public static BaseEffect CreateStatBuffEffect(string id, float duration, StatModifier modifier, ModifierValueType valueType)
    {
        return new StatBuffEffect(id, duration, modifier, valueType);
    }

    public static BaseEffect CreateDotStatusEffect(string id, string desc, float duration, SO_BaseEffect so = null, float tick = 0f, float power = 0f)
    {
        BaseEffect effect = id switch
        {
            "Burn" => new BurnEffect(id, desc, duration, tick, power),
            "Bleed" => new BleedEffect(id, desc, duration, tick, power),
            "Poison" => new PoisonEffect(id, desc, duration, tick, power),
            "Curse_Hatred" => new HatredEffect(id, desc, duration, tick, power),
            "Curse" => new CurseEffect(id, desc, duration),
            _ => null
        };

        if (so != null)
        {
            effect.FxObject = so.vfxObject;
            effect.FxIcon = so.icon;
        }

        return effect;
    }

    public static BaseEffect CreateEffect(string id, string desc, float duration, SO_BaseEffect so = null)
    {
        BaseEffect effect = id switch
        {
            "Curse" => new CurseEffect(id, desc, duration),
            _ => null
        };

        if (so != null)
        {
            effect.FxObject = so.vfxObject;
            effect.FxIcon = so.icon;
        }
        return effect;
    }
}
