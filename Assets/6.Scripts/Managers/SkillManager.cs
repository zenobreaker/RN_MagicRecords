using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines.ExtrusionShapes;

public class SkillManager : MonoBehaviour
{
    private readonly int SKILL_SLOT_MAX_COUNT = 4;

    // 캐릭터별로 장착한 스킬 key : character id, value : runtime data 
    private Dictionary<int, List<SkillRuntimeData>> equippedActiveSkills = new();

    public event Action OnDataChanaged;

    private void Awake()
    {
        ResetRunTimeData();
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
            Skill active = null;

            if (skillData?.template is SO_ActiveSkillData da)
                active = da.CreateSkill();

            skillComp.SetActiveSkill((SkillSlot)((int)SkillSlot.SLOT1 + i), active as ActiveSkill);
        }
    }

    public void ResetRunTimeData()
    {
        equippedActiveSkills.Clear();
        var slots = new List<SkillRuntimeData>(SKILL_SLOT_MAX_COUNT);
        for (int i = 0; i < SKILL_SLOT_MAX_COUNT; i++)
            slots.Add(null);
        // 현재 1번 아이디의 캐릭터 슬롯 처리 
        equippedActiveSkills.Add(1, slots);
    }
}
