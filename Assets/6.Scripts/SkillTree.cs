using System;
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
    private Dictionary<int, SkillRuntimeData> skillRuntimeDatas = new(); 
    public void Initialize(Action<SkillRuntimeData> action = null)
    {
        skillByLevelTable = allSkills.GroupBy(skill => skill.learnableLevel)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.id).ToList());

        skillRuntimeDatas.Clear();
        foreach (var skillpair in skillByLevelTable)
        {
            foreach (var skill in skillpair.Value)
            {
                var runtimedata = new SkillRuntimeData
                {
                    template = skill,
                    currentLevel = 0,
                    isUnlocked = false,
                };

                if(action != null)
                    runtimedata.OnDataChanged += action;
                skillRuntimeDatas.Add(skill.id, runtimedata);
            }
        }
    }
    
    public List<SO_SkillData> GetSkillForLevel(int level, bool onlyActive  = false, bool onlyPassive = false)
    {
        if (skillByLevelTable == null)
            return new();

        if (skillByLevelTable.TryGetValue(level, out var skills) == false)
            return new();

        if(onlyActive)
            return skills.Where(s=> s is SO_ActiveSkillData).ToList(); 

        if(onlyPassive)
            return skills.Where(s => s is SO_PassiveSkillData).ToList();    

        return skills;
    }

    public SkillRuntimeData GetSkillRuntimeDataByID(int id)
    {
        return skillRuntimeDatas.TryGetValue(id, out var value) ? value : null;
    }

    public List<SkillRuntimeData> GetSkillRuntimeDatasByLevel(int level, bool onlyActive = false, bool onlyPassive = false)
    {
        List<SkillRuntimeData> runtimeList = GetSkillForLevel(level, onlyActive, onlyPassive).Select(sd =>
        skillRuntimeDatas[sd.id])?.ToList();

        foreach (SkillRuntimeData skill in runtimeList)
        {
            var currentData = GetSkillRuntimeDataByID(skill.template.id);
            if (currentData == null)
                continue;

            skill.currentLevel = currentData.currentLevel;
            skill.isUnlocked = currentData.isUnlocked;
        }

        return runtimeList;
    }
}
