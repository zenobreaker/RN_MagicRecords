using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PatternEntryData
{
    public string slotName;
    public SO_ActiveSkillData activeSkillData;
    public List<PatternCondition> conditions;

    public ActiveSkill CreateSkill()
    {
        return (ActiveSkill)activeSkillData?.CreateSkill(); 
    }
}
