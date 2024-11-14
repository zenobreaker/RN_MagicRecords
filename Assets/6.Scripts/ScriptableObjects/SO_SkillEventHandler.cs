using System;
using UnityEngine;


/// <summary>
/// ������ ����ó�� ����ϱ� ���� ¡�˴ٸ� Ŭ����
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

    // ���� 1�� ����
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


    // ��Ÿ�� 
    public void OnCooldown(float InCooldown)
    {
        OnSkillCooldown?.Invoke(InCooldown);
    }

    // ���� ���� 
    public void OnUnequipment()
    {
        OnDisableSkill?.Invoke();
    }
}
