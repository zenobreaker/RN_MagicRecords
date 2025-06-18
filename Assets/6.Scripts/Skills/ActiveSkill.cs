using UnityEngine;


public enum SkillPhase
{
    Start = 0,
    Casting,
    Action,
    Finish,
    MAX,
}

[System.Serializable]
public abstract class ActiveSkill 
    : ICooldownable
{
    protected SO_ActiveSkillData skillData;
    public SO_ActiveSkillData SO_SkillData { get => skillData; set => skillData = value; }

    protected SkillPhase skillPhase;
    protected PhaseSkill phaseSkill;
    protected float currentCooldown;

    protected GameObject ownerObject;
    protected Animator animator;
    protected WeaponController weaponController;

    public bool IsOnCooldown => currentCooldown > 0;
    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => skillData.cooldown; }

    public void SetOwner(GameObject gameObject)
    {
        ownerObject = gameObject;
        animator = gameObject.GetComponent<Animator>();

        if (ownerObject.TryGetComponent(out IWeaponUser user))
        {
            weaponController = user.GetWeaponController();
        }

        foreach(var phase in skillData.phaseList)
        {
            phase.actionData?.Initialize();
        }
    }
    protected void SetCurrentPhaseSkill(int phaseIndex)
    {
        if (skillData == null || phaseIndex < 0 || phaseIndex >= skillData.phaseList.Count)
            return;

        phaseSkill = skillData.phaseList[phaseIndex];
    }

    public void SetCooldown(float cooldown)
    {
        if (currentCooldown > 0)
            currentCooldown -= cooldown;
    }

    public void Cast()
    {
        if (IsOnCooldown)
            return;

        //TODO: 스킬 캐스팅

        // 쿨타임 
        currentCooldown = skillData.cooldown;

        // 첫 번째 페이즈 
        ExecutePhase(0);
    }


    protected abstract void ExecutePhase(int phaseIndex);
    protected abstract void ApplyEffects();     // 개별 효과 적용 

    public virtual void Begin_DoAction() { }

    public virtual void End_DoAction() { }

    public virtual void Begin_JudgeAttack(AnimationEvent e) { }
    public virtual void End_JudgeAttack(AnimationEvent e) { }

    public virtual void Play_Sound () { }
    public virtual void Play_CameraShake() { }
}
