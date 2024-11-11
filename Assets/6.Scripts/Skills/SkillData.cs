using System;
using UnityEngine;

[Serializable]
public class SkillActionData : DoActionData
{
    public float HitDelay;

    public override object Clone()
    {
        return this.MemberwiseClone();
    }
}
