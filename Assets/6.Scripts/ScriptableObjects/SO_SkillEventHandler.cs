using System;
using UnityEngine;


/// <summary>
/// 옵저버 패턴처럼 사용하기 위한 징검다리 클래스
/// </summary>
[CreateAssetMenu(fileName = "SO_SkillEventHandler", menuName = "Scriptable Objects/SO_SkillEventHandler")]
public class SO_SkillEventHandler : ScriptableObject
{
    public event Action<float> OnSkillCooldown;

    public event Action<ActiveSkill> OnSkillData_Slot1;
    public event Action<ActiveSkill> OnSkillData_Slot2;
    public event Action<ActiveSkill> OnSkillData_Slot3;
    public event Action<ActiveSkill> OnSkillData_Slot4;

    public event Action OnDisableSkill;

    // 슬롯 1에 장착
    public void OnEquipSkill_Slot1(ActiveSkill activeSkill)
    {
        OnSkillData_Slot1?.Invoke(activeSkill);
    }

    public void OnEquipSkill_Slot2(ActiveSkill activeSkill)
    {
        OnSkillData_Slot2?.Invoke(activeSkill);
    }

    public void OnEquipSkill_Slot3(ActiveSkill activeSkill)
    {
        OnSkillData_Slot3?.Invoke(activeSkill);
    }

    public void OnEquipSkill_Slot4(ActiveSkill activeSkill)
    {
        OnSkillData_Slot4?.Invoke(activeSkill);
    }


    // 쿨타임 
    public void OnCooldown(float InCooldown)
    {
        OnSkillCooldown?.Invoke(InCooldown);
    }

    // 장착 해제 
    public void OnUnequipment()
    {
        OnDisableSkill?.Invoke();
    }
}
