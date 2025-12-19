using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    private static readonly Dictionary<int, ActiveSkill> creators
        = new();

    public static Skill CreateSkill(SO_SkillData data)
    {
        if(data == null) return null;

        Skill skill = data.id switch
        {
            1 => new ReinforcedMagicBullet(data),
            11 => new Passive_MagicBulletLoad(data),
            12 => new Passive_ForbiddenCurse(data),
            15 => new Passive_ContemptuousWoe(data),
            16 => new Passive_AbominableHatred(data),
            17 => new Passive_RuinousWrath(data),   
            _ => null
        };

        return skill; 
    }
}
