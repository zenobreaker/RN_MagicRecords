using System;
using UnityEngine;

[ModuleCategory("Common/Play Animation")]
[Serializable]
public class Module_PlayAnimation : SkillModule
{
    [Header("Skill Action")]
    public ActionData actionData;

    private Character ownerCharacter;
    private WeaponController weaponController;

    public override void Init(Character owner)
    {
        actionData.Initialize();
        ownerCharacter = owner.GetComponent<Character>();

        weaponController = owner.GetComponent<IWeaponUser>()?.GetWeaponController();
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (ownerCharacter != null)
            ownerCharacter.PlayAction(actionData);
        if (weaponController != null)
            weaponController.DoAction(actionData);
    }

    public override bool HasAnimationData()
    {
        // 모듈에 세팅된 actionData의 서브스테이트 이름이 비어있지 않으면 true 반환!
        return actionData != null && !string.IsNullOrEmpty(actionData.SubStateName);
    }
}
