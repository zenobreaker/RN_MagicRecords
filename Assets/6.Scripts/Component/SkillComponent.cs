using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillComponent 
    : ActionComponent
{
    private StateComponent state;
    private WeaponComponent weapon;

    private bool bIsSkillAction = false;
    private SkillSlot currentSlot = SkillSlot.MAX;

    // 장착 스킬 정보 
    private Dictionary<SkillSlot, ActiveSkill> skillSlotTable;

    // 스킬 사용 관련 이벤트 핸들러
    public SO_SkillEventHandler skillEventHandler;

    public event Action<bool> OnSkillUse;

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

    private void Update()
    {
        if (skillSlotTable == null)
            return;

        // 쿨다운 업데이트 
        foreach (KeyValuePair<SkillSlot, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null) continue;

            skillEventHandler?.OnInCoolDown(pair.Value.IsOnCooldown);
            if (pair.Value.IsOnCooldown == false) continue;

            pair.Value.SetCooldown(Time.deltaTime);
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
        if (bIsSkillAction || skillSlotTable.TryGetValue(currentSlot, out var skill) == false)
        {
            OnSkillUse?.Invoke(false);
            return;
        }

        currentSlot = slot;
        OnSkillUse?.Invoke(true);
        DoAction();
    }

    public override void DoAction()
    {
        base.DoAction();
        
        skillSlotTable[currentSlot]?.Cast();
    }

    public override void BeginDoAction()
    {
        if(currentSlot == SkillSlot.MAX) return;
        base.BeginDoAction();
        
        bIsSkillAction = true;
        skillSlotTable[currentSlot]?.Begin_DoAction();
        
        OnBeginDoAction?.Invoke();
        skillEventHandler?.OnBegin_UseSkill();
    }

    public override void EndDoAction()
    {
        if (currentSlot == SkillSlot.MAX) return;
        base.EndDoAction();

        bIsSkillAction = false; 
        skillSlotTable[currentSlot]?.End_DoAction();
        currentSlot = SkillSlot.MAX;

        Debug.Log($"Skill End DoAction");
        OnEndDoAction?.Invoke(); 
        skillEventHandler?.OnEnd_UseSkill();
    }

    public override void BeginJudgeAttack(AnimationEvent e) 
    {
        base.BeginJudgeAttack(e);
        skillSlotTable[currentSlot]?.Begin_JudgeAttack(e);
    }

    public override void EndJudgeAttack(AnimationEvent e) 
    {
        base.EndJudgeAttack(e);
        skillSlotTable[currentSlot]?.End_JudgeAttack(e);
    }

    public override void PlaySound()
    {
        base.PlaySound();
        skillSlotTable[currentSlot]?.Play_Sound();
    }

    public override void PlayCameraShake()
    {
        base.PlayCameraShake();
        skillSlotTable[currentSlot]?.Play_CameraShake();
    }
}
