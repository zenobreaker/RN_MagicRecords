using System.Collections.Generic;

public static class SkillFactory
{
    private static readonly Dictionary<int, ActiveSkill> creators
        = new();

    public static Skill CreateSkill(SO_SkillData data)
    {
        if(data == null) return null;

        // 1. 패시브 데이터 인 경우 (기존 방식) 
        if( data is SO_PassiveSkillData passiveData)
        {
            return CreatePassive(passiveData);
        }

        // 2. 액티브 데이터인 경우 (제네릭 통합, 특별한 경우에 그 아이디 값으로) 
        if( data is SO_ActiveSkillData activeData)
        {
            return activeData.id switch
            {
                1 => new ReinforcedMagicBullet(activeData),
                1001 => new JumpPress(activeData),
                1002 => new SpikeShot(activeData),
                _ => new GenericActiveSkill(activeData)
            }; 
        }

        return null;
    }

    private static Skill CreatePassive(SO_PassiveSkillData passiveData)
    {
        return passiveData.id switch
        {
            11 => new Passive_MagicBulletLoad(passiveData),
            12 => new Passive_ForbiddenCurse(passiveData),
            15 => new Passive_ContemptuousWoe(passiveData),
            16 => new Passive_AbominableHatred(passiveData),
            17 => new Passive_RuinousWrath(passiveData),
            _ => null
        };
    }
}
