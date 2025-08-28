using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    private static readonly Dictionary<int, ActiveSkill> creators
        = new();

    public static ActiveSkill CreateSkill(int skillId, SO_ActiveSkillData data)
    {
        ActiveSkill skill = skillId switch
        {
            1 => new ReinforcedMagicBullet(data),
            _ => null
        };

        return skill; 
    }

}
