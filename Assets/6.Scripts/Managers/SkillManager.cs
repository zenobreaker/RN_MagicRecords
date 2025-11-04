using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private readonly int SKILL_SLOT_MAX_COUNT = 4;

    // 캐릭터별로 장착한 스킬 
    private Dictionary<int, List<SkillRuntimeData>> equippedActiveSkills = new();

    public event Action OnDataChanaged;

    private void Awake()
    {
        equippedActiveSkills.Clear();
        var slots = new List<SkillRuntimeData>(SKILL_SLOT_MAX_COUNT);
        for (int i = 0; i < SKILL_SLOT_MAX_COUNT; i++)
            slots.Add(null);
        equippedActiveSkills.Add(1, slots); 
    }


    public void EquipActiveSkill(int charId, int slot, SkillRuntimeData skill)
    {
        // 이미 장착되어 있다면 그것을 해제하고 새로 장착
        int prevSlot = -1;
        prevSlot = equippedActiveSkills[charId].FindIndex(x => x == skill);
        if(prevSlot != -1)
            equippedActiveSkills[charId][prevSlot] = null;

        equippedActiveSkills[charId][slot] = skill;
        OnDataChanaged?.Invoke();
    }

    public List<SkillRuntimeData> GetActiveSkillList(int charId)
    {
        return equippedActiveSkills[charId];
    }

    public List<int> GetActiveSkillIDList(int charID)
    {
        return equippedActiveSkills[charID]
            .Select(skill => skill != null ? skill.GetSkillID() : 0).ToList();
    }

    public void SetActiveSkills(int charid, SkillComponent skillComp)
    {
        if (!equippedActiveSkills.TryGetValue(charid, out var equipped)) return; 

        if (skillComp == null) return; 

        for(int i = 0; i < SKILL_SLOT_MAX_COUNT; i++)
        {
            SkillRuntimeData skillData = (i < equipped.Count) ? equipped[i] : null;
            ActiveSkill active = null;

            if (skillData?.template is SO_ActiveSkillData da)
                active = da.CreateActiveSkill();

            skillComp.SetActiveSkill((SkillSlot)i, active);
        }
    }
}
