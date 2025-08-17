using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 스킬 트리 정보를 담는 클래스 
[System.Serializable]
public class SkillTree 
{
    public int id;

    public List<SO_SkillData> allSkills;

    // 스킬 트리는 레벨 별로 배우는 스킬들로 지정한다. 
    private Dictionary<int, List<SO_SkillData>> skillByLevelTable; 

    public void Initialize()
    {
        skillByLevelTable = allSkills.GroupBy(skill => skill.learnableLevel)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.id).ToList());

        foreach(var skillpair in  skillByLevelTable)
        {
            Debug.Log($"skill level : {skillpair.Key}");
            foreach(var skill in skillpair.Value)
                Debug.Log($"skill id : {skill.id}");
        }
    }

    public List<SO_SkillData> GetSkillForLevel(int level)
    {
        return skillByLevelTable.TryGetValue(level, out var skills)
            ? skills : new List<SO_SkillData>(); 
    }
}
