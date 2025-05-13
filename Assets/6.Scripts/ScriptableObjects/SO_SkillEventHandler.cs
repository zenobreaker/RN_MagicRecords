using System;
using UnityEngine;


/// <summary>
/// 옵저버 패턴처럼 사용하기 위한 징검다리 클래스
/// </summary>
[CreateAssetMenu(fileName = "SO_SkillEventHandler", menuName = "Scriptable Objects/SO_SkillEventHandler")]
public class SO_SkillEventHandler : ScriptableObject
{
    public event Action<ActiveSkill> OnSetActiveSkill;

    public event Action<bool> OnInSkillCooldown;
    public event Action<float> OnSkillCooldown;
    public event Action<float, float> OnSkillCooldown_TwoParam;

    public event Action<ActiveSkill> OnSkillData_Slot1;
    public event Action<ActiveSkill> OnSkillData_Slot2;
    public event Action<ActiveSkill> OnSkillData_Slot3;
    public event Action<ActiveSkill> OnSkillData_Slot4;

    public event Action OnBeginUseSkill;
    public event Action OnEndUseSkill;

    public event Action OnDisableSkill;

#region EQUIP SKILL
    public void OnSetting_ActiveSkill(ActiveSkill skill) => OnSetActiveSkill?.Invoke(skill);  

    // 슬롯 1에 장착
    public void OnEquipSkill_Slot1(ActiveSkill activeSkill) => OnSkillData_Slot1?.Invoke(activeSkill);

    public void OnEquipSkill_Slot2(ActiveSkill activeSkill) => OnSkillData_Slot2?.Invoke(activeSkill);

    public void OnEquipSkill_Slot3(ActiveSkill activeSkill) => OnSkillData_Slot3?.Invoke(activeSkill);

    public void OnEquipSkill_Slot4(ActiveSkill activeSkill) => OnSkillData_Slot4?.Invoke(activeSkill);

    #endregion

#region COOLDOWN
    // 쿨타임 
    public void OnInCoolDown(bool inCooldown) => OnInSkillCooldown?.Invoke(inCooldown);
    public void OnCooldown(float InCooldown) => OnSkillCooldown?.Invoke(InCooldown);
    public void OnCooldown(float cooldown, float maxCooldown) => OnSkillCooldown_TwoParam?.Invoke(cooldown, maxCooldown);
    #endregion

#region USE SKILL
    public void OnBegin_UseSkill() => OnBeginUseSkill?.Invoke();

    public void OnEnd_UseSkill() => OnEndUseSkill?.Invoke();
#endregion

    // 장착 해제 
    public void OnUnequipment() => OnDisableSkill?.Invoke();
}
