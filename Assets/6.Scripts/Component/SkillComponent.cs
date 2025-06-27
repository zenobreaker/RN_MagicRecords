using System;
using System.Collections.Generic;
using UnityEngine;

//TODO: 확장성을 위하여 enum이 아닌 string으로 처리해보기
public enum SkillSlot
{
    Slot1 = 0, Slot2, Slot3, Slot4, MAX,
}

public class SkillComponent 
    : ActionComponent
{
    private StateComponent state;
    private WeaponComponent weapon;
    private DamageHandleComponent damageHandle;

    private bool bIsSkillAction = false;
    public bool IsSkillAction { get { return bIsSkillAction; } }
    private string currentSkillName = "";

    // 장착 스킬 정보 
    private Dictionary<string, ActiveSkill> skillSlotTable;

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

        damageHandle = GetComponent<DamageHandleComponent>();
        if (damageHandle != null)
            damageHandle.OnDamaged += OnDamaged; 

        Awake_SkillSlotTable();
    }

    private void OnDamaged()
    {
        if (IsSkillAction)
            EndDoAction();
    }

    private void Awake_SkillSlotTable()
    {
        skillSlotTable = new Dictionary<string, ActiveSkill>
        {
            {"Slot1", null },
            {"Slot2", null },
            {"Slot3", null },
            {"Slot4", null },
        };
    }

    private void Update()
    {
        if (skillSlotTable == null)
            return;

        // 쿨다운 업데이트 
        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null) continue;

            pair.Value.Update(Time.deltaTime);

            skillEventHandler?.OnInCoolDown(pair.Value.IsOnCooldown);
            if (pair.Value.IsOnCooldown == false) continue;

            pair.Value.Update_Cooldown(Time.deltaTime);
            skillEventHandler?.OnCooldown(pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
        }
    }


    public bool CanUseSkill(string skillName)
    {
        if(skillSlotTable.TryGetValue(skillName, out var skill))
            return skill != null && skill.IsOnCooldown == false && bIsSkillAction == false;

        return false; 
    }

    // 스킬 장착 
    public void SetActiveSkill(SkillSlot slot , ActiveSkill skill)
    {
        if(skill == null) return;

        SetActiveSkill(slot.ToString(), skill);
    }

    public void SetActiveSkill(string skillName, ActiveSkill skill)
    {
        if (skill == null) return;

        if(skillSlotTable.ContainsKey(skillName))
            skillSlotTable[skillName] = skill;
        else 
            skillSlotTable.Add(skillName, skill);
        
        skillSlotTable[skillName].SetOwner(rootObject);
        skillSlotTable[skillName].InitializedData();

        skillEventHandler?.OnSetting_ActiveSkill(skill);
    }

    // 슬롯의 있는 스킬 사용 
    public void UseSkill(SkillSlot slot)
    {
        UseSkill(slot.ToString());
    }

    public void UseSkill(string skillName)
    {
        if (bIsSkillAction || CanUseSkill(skillName) == false)
        {
            OnSkillUse?.Invoke(false);
            return;
        }

        currentSkillName = skillName;
        OnSkillUse?.Invoke(true);
        DoAction();
    }

    public override void DoAction()
    {
        base.DoAction();

        if (!skillSlotTable.ContainsKey(currentSkillName)) return;

        bIsSkillAction = true;
        skillSlotTable[currentSkillName]?.Cast();
    }

    public override void StartAction()
    {
        base.StartAction();

        skillSlotTable[currentSkillName]?.Start_DoAction();
    }

    public override void BeginDoAction()
    {
        if(string.IsNullOrEmpty(currentSkillName)) return;
        base.BeginDoAction();
        
        skillSlotTable[currentSkillName]?.Begin_DoAction();
        
        OnBeginDoAction?.Invoke();
        skillEventHandler?.OnBegin_UseSkill();
    }

    public override void EndDoAction()
    {
        if(string.IsNullOrEmpty(currentSkillName)) return;
        base.EndDoAction();

        bIsSkillAction = false; 
        skillSlotTable[currentSkillName]?.End_DoAction();
        currentSkillName = string.Empty;

        Debug.Log($"Skill End DoAction");
        OnEndDoAction?.Invoke(); 
        skillEventHandler?.OnEnd_UseSkill();
    }

    public override void BeginJudgeAttack(AnimationEvent e) 
    {
        base.BeginJudgeAttack(e);
        if (string.IsNullOrEmpty(currentSkillName)) return; 

        skillSlotTable[currentSkillName]?.Begin_JudgeAttack(e);
    }

    public override void EndJudgeAttack(AnimationEvent e) 
    {
        base.EndJudgeAttack(e);
        if (string.IsNullOrEmpty(currentSkillName)) return;
        skillSlotTable[currentSkillName]?.End_JudgeAttack(e);
    }

    public override void PlaySound()
    {
        base.PlaySound();
        if (string.IsNullOrEmpty(currentSkillName)) return;
        skillSlotTable[currentSkillName]?.Play_Sound();
    }

    public override void PlayCameraShake()
    {
        base.PlayCameraShake();
        if (string.IsNullOrEmpty(currentSkillName)) return;
        skillSlotTable[currentSkillName]?.Play_CameraShake();
    }
}
