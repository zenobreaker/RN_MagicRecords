using UnityEngine;

public static class ModifierFactory 
{
    public static StatModifier CreateStatModifier(StatusType type, float value, ModifierValueType vType)
    {
        return new StatModifier(type, value, vType);
    }
}
