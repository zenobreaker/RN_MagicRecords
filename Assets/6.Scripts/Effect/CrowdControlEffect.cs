using UnityEngine;

public class CrowdControlEffect : BaseEffect
{
    public virtual StatusEffectType EffectFlag => StatusEffectType.None;

    public CrowdControlEffect(string id, string desc, float duration) 
        : base(id, desc, duration)
    {
    }
}
