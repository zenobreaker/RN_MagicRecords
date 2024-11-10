using System;
using UnityEngine;

[Serializable]
public class SkillData : DoActionData
{
    public float HitDelay;

    public override object Clone()
    {
        return this.MemberwiseClone();
    }
}
