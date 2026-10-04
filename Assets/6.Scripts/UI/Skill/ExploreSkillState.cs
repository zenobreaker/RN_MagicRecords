using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class ExploreActiveSkillSaveData
{
    public int characterID;
    public int jobID;
    public List<SkillSaveData> skills = new();
    public List<int> slots = new();
}

// No skill-tree objects or change callbacks are shared with the run.
public sealed class ExploreSkillState
{
    public int JobID { get; }
    public IReadOnlyCollection<SkillRuntimeData> Skills => skills.Values;
    private readonly Dictionary<int, SkillRuntimeData> skills = new();
    private readonly List<SkillRuntimeData> slots;

    public ExploreSkillState(int jobID, IEnumerable<SkillRuntimeData> source, List<SkillRuntimeData> slots)
    {
        JobID = jobID;
        this.slots = slots;
        foreach (var data in source.Concat(slots).Where(s => s?.template is SO_ActiveSkillData))
            skills[data.GetSkillID()] = new SkillRuntimeData
            { template = data.template, currentLevel = data.currentLevel, isUnlocked = data.isUnlocked };
        for (int i = 0; i < slots.Count; i++)
            slots[i] = slots[i] != null && skills.TryGetValue(slots[i].GetSkillID(), out var skill) ? skill : null;
    }

    public ExploreActiveSkillSaveData Capture(int characterID) => new()
    {
        characterID = characterID, jobID = JobID,
        skills = skills.Values.Select(s => new SkillSaveData
            { skillID = s.GetSkillID(), skillLevel = s.currentLevel, unlocked = s.isUnlocked }).ToList(),
        slots = slots.Select(s => s?.GetSkillID() ?? 0).ToList()
    };

    public void Restore(ExploreActiveSkillSaveData saved)
    {
        // Cheat-granted skills can belong to another tree; resolve their saved IDs as well.
        foreach (var data in saved.skills ?? new())
            if (data != null && !skills.ContainsKey(data.skillID) &&
                SkillTreeManager.Instance?.FindSkillTemplate(data.skillID) is SO_ActiveSkillData template)
                GetOrAddSkill(template);
        foreach (var data in saved.skills ?? new())
            if (data != null && skills.TryGetValue(data.skillID, out var skill))
            {
                skill.currentLevel = Mathf.Clamp(data.skillLevel, 0, skill.GetMaxSkillLevel());
                skill.isUnlocked = data.unlocked;
            }
        var equipped = new HashSet<int>();
        for (int i = 0; i < slots.Count; i++)
            slots[i] = saved.slots != null && i < saved.slots.Count && equipped.Add(saved.slots[i]) &&
                skills.TryGetValue(saved.slots[i], out var skill) && skill.currentLevel > 0 ? skill : null;
    }

    public SkillRuntimeData GetOrAddSkill(SO_ActiveSkillData template)
    {
        if (!skills.TryGetValue(template.id, out var skill))
        {
            skill = new SkillRuntimeData { template = template };
            skills.Add(template.id, skill);
        }
        return skill;
    }
}
