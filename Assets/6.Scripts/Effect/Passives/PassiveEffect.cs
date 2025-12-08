using UnityEngine;

public abstract class PassiveEffect : BaseEffect
{
    protected PassiveEffect(string id, string desc, float duration) 
        : base(id, desc, duration)
    {
    }
}
