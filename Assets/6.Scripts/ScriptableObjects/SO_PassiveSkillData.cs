using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_PassiveSkillData")]
public class SO_PassiveSkillData : SO_SkillData
{
    public int jobID; // 대상 직업군 
    public List<int> enhanceSkillTargets;
}
