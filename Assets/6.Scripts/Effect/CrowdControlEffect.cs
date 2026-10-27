using UnityEngine;

public class CrowdControlEffect : BaseEffect
{
    public virtual StatusEffectType EffectFlag
    {
        get
        {
            return StatusEffectType.None;
        }
    }

    public CrowdControlEffect(string id, string desc, float duration)
        : base(id, desc, duration)
    {
    }
}
