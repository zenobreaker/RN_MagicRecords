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

        //TODO: ��ų ĳ����

        // ��Ÿ�� 
        currentCooldown = skillData.cooldown;

        // ù ��° ������ 
        StartPhase(0);
    }

    protected abstract void StartPhase(int phaseIndex);
    protected abstract void ApplyEffects();     // ���� ȿ�� ���� 
}
