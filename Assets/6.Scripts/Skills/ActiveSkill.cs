using UnityEngine;

public abstract class ActiveSkill : ISkill
{
    protected SO_ActiveSkillData skillData;
    protected float currentCooldown;

    protected GameObject ownerObject;

    public bool IsOnCooldown => currentCooldown > 0;

    public void Cast()
    {
        if (IsOnCooldown)
            return;

        //TODO: 스킬 캐스팅

        // 쿨타임 
        currentCooldown = skillData.cooldown;

        // 첫 번째 페이즈 
        StartPhase(0);
    }

    protected abstract void StartPhase(int phaseIndex);
    protected abstract void ApplyEffects();     // 개별 효과 적용 
}
