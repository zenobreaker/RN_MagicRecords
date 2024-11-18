using UnityEngine;

[System.Serializable]
public abstract class ActiveSkill : ISkill
{
    protected SO_ActiveSkillData skillData;
    public SO_ActiveSkillData SO_SkillData { get => skillData; set => skillData = value; }
    protected float currentCooldown;

    protected GameObject ownerObject;
    protected Animator animator; 

    public bool IsOnCooldown => currentCooldown > 0;
    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => skillData.cooldown; }

    public void SetOwner(GameObject gameObject)
    {
        ownerObject = gameObject; 
        animator = gameObject.GetComponent<Animator>();
    }

    public void SetCooldown(float cooldown)
    {
        if(currentCooldown > 0)
            currentCooldown -= cooldown;
    }

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

    public abstract void Begin_DoAction();

    public abstract void End_DoAction();
}
