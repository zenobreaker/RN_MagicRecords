using System.Collections.Generic;
using UnityEngine;

public class SkillComponent : MonoBehaviour
{
    private StateComponent state;
    private WeaponComponent weapon;

    private GameObject rootObject; 

    private bool bIsSkillAction = false;
    private SkillSlot currentSlot = SkillSlot.MAX;

    // 장착 스킬 정보 
    private Dictionary<SkillSlot, ActiveSkill> skillSlotTable;

    // 스킬 사용 관련 이벤트 핸들러
    public SO_SkillEventHandler skillEventHandler;

    private void Awake()
    {
        rootObject = transform.root.gameObject;

        state = GetComponent<StateComponent>();
        Debug.Assert(state != null);
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);

        Awake_SkillSlotTable();
    }

    private void Awake_SkillSlotTable()
    {
        skillSlotTable = new Dictionary<SkillSlot, ActiveSkill>
        {
            {SkillSlot.Slot1, null },
            {SkillSlot.Slot2, null },
            {SkillSlot.Slot3, null },
            {SkillSlot.Slot4, null },
        };
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (skillSlotTable == null)
            return;

        // 쿨다운 업데이트 
        foreach (KeyValuePair<SkillSlot, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null) continue;

            if (pair.Value.IsOnCooldown == false) continue;

            pair.Value.SetCooldown(Time.deltaTime);
            skillEventHandler?.OnInCoolDown(pair.Value.IsOnCooldown);
            skillEventHandler?.OnCooldown(pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
        }
    }


    // 스킬 장착 
    public void SetActiveSkill(SkillSlot slot , ActiveSkill skill)
    {
        if(skill == null) return;

        skillSlotTable[slot] = skill;
        skillSlotTable[slot].SetOwner(rootObject);

        skillEventHandler?.OnSetting_ActiveSkill(skill);
    }

    // 슬롯의 있는 스킬 사용 
    public void UseSkill(SkillSlot slot)
    {
        if (bIsSkillAction) return; 

        currentSlot = slot;
        skillSlotTable[slot]?.Cast();

        
    }

    public void Begin_SkillAction()
    {
        if(currentSlot == SkillSlot.MAX) return;
        
        bIsSkillAction = true;
        skillSlotTable[currentSlot]?.Begin_DoAction();

        skillEventHandler?.OnBegin_UseSkill();
    }

    public void End_SkillAction()
    {
        if (currentSlot == SkillSlot.MAX) return;

        bIsSkillAction = false; 
        skillSlotTable[currentSlot]?.End_DoAction();
        currentSlot = SkillSlot.MAX;

        skillEventHandler?.OnEnd_UseSkill();
    }
}
