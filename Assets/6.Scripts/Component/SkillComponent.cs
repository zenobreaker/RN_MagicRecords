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

    // 어떤 인터페이스든 구현체를 저장
    private Dictionary<Type, object> capabilityTable = new();

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
        int slot = 0;
        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null) continue;

            pair.Value.Update(Time.deltaTime);

            skillEventHandler?.OnInCoolDown((SkillSlot)slot, pair.Value.IsOnCooldown);
            if (pair.Value.IsOnCooldown == false) continue;

            pair.Value.Update_Cooldown(Time.deltaTime);
            skillEventHandler?.OnCooldown((SkillSlot)slot, pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
            slot = slot + 1 % 4;
        }
    }

    // 기능 등록 (패시브가 호출)
    public void RegisterCapability<T>(T capability) where T : class
    {
        var type = typeof(T);
        if (capabilityTable.ContainsKey(type))
            capabilityTable[type] = capability;  // 이미 있으면 덮어쓰기 
        else
            capabilityTable.Add(type, capability);  
    }

    // 기능 조회 (액티브가 호출)
    public T GetCapability<T>() where T : class
    {
        var type = typeof(T);
        if (capabilityTable.TryGetValue(type, out object value))
            return (T)value;
        return null;
    }

    // 기능 해제 (패시브가 사라지거나 해제 시) 
    public void UnregisterCapability<T>()
    {
        var type = typeof(T);
        if (capabilityTable.ContainsKey(type))
        {
            capabilityTable.Remove(type);
        }
    }

    public bool CanUseSkill(string skillName)
    {
        if (skillSlotTable.TryGetValue(skillName, out var skill))
            return skill != null && skill.IsOnCooldown == false && bIsSkillAction == false;

        return false;
    }

    // 스킬 장착 
    public void SetActiveSkill(SkillSlot slot, ActiveSkill skill)
    {
        if (skill == null) return;
        SetActiveSkill(slot.ToString(), skill);
        skillEventHandler?.OnSetting_ActiveSkill(slot, skill);
    }

    // AI에서 접근할 때는 첫 번째 인자는 스킬 이름으로 오므로 주의해야 함 
    public void SetActiveSkill(string slotName, ActiveSkill skill)
    {
        if (skill == null) return;

        if (skillSlotTable.ContainsKey(slotName))
            skillSlotTable[slotName] = skill;
        else
            skillSlotTable.Add(slotName, skill);

        skillSlotTable[slotName].SetOwner(rootObject);
        skillSlotTable[slotName].InitializedData();
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
        if (!skillSlotTable.ContainsKey(currentSkillName)) return;
        base.DoAction();


        bIsSkillAction = true;
        skillSlotTable[currentSkillName]?.Cast();
    }

    public override void StartAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.StartAction();

        skillSlotTable[currentSkillName]?.Start_DoAction();
    }

    public override void BeginDoAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.BeginDoAction();

        skillSlotTable[currentSkillName]?.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        skillEventHandler?.OnBegin_UseSkill();
    }

    public override void EndDoAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
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
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.BeginJudgeAttack(e);

        skillSlotTable[currentSkillName]?.Begin_JudgeAttack(e);
    }

    public override void EndJudgeAttack(AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.EndJudgeAttack(e);
        skillSlotTable[currentSkillName]?.End_JudgeAttack(e);
    }

    public override void PlaySound()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.PlaySound();

        skillSlotTable[currentSkillName]?.Play_Sound();
    }

    public override void PlayCameraShake()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.PlayCameraShake();

        skillSlotTable[currentSkillName]?.Play_CameraShake();
    }


    ///////////////////////////////////////////////////////////////////////////
    #region NOTIFY
    public void NotifyBulletInit(int bulletCount)
    {
        skillEventHandler?.OnUpdateMagciBulletLoad(bulletCount);
    }

    public void NotifyMagicBulletChanged(Queue<BulletData> bullets)
    {
        skillEventHandler?.OnChangedBullets(bullets);   
    }

    #endregion
}
