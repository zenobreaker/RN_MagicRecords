using System;
using UnityEngine;

public enum AnimParamType { Bool, Float, Int, Trigger }

[ModuleCategory("Animation/Set Parameter")]
[Serializable]
public class Module_SetAnimParam : SkillModule
{
    [Header("Target Parameter")]
    [Tooltip("제어할 애니메이터 파라미터의 정확한 이름을 적어주세요 (예: IsCasting)")]
    public string parameterName;
    public AnimParamType paramType = AnimParamType.Bool;

    [Header("Values")]
    public bool boolValue;
    public float floatValue;
    public int intValue;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (string.IsNullOrEmpty(parameterName)) return;

        // 주인의 애니메이터를 가져옵니다 (캐릭터 구조에 맞게 수정 가능)
        Animator anim = owner.GetComponentInChildren<Animator>();
        if (anim == null) return;

        // 선택한 타입에 맞춰서 애니메이터 변수를 찔러줍니다!
        switch (paramType)
        {
            case AnimParamType.Bool:
                anim.SetBool(parameterName, boolValue);
                break;
            case AnimParamType.Float:
                anim.SetFloat(parameterName, floatValue);
                break;
            case AnimParamType.Int:
                anim.SetInteger(parameterName, intValue);
                break;
            case AnimParamType.Trigger:
                anim.SetTrigger(parameterName);
                break;
        }
    }

    public override SkillModule Clone()
    {
        return (Module_SetAnimParam)base.Clone();
    }
}