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
        StartPhase(0);
    }

    protected abstract void StartPhase(int phaseIndex);
    protected abstract void ApplyEffects();     // 개별 효과 적용 

    public virtual void Begin_DoAction() { }

    public virtual void End_DoAction() { }

    public virtual void Begin_JudgeAttack() { }
    public virtual void End_JudgeAttack() { }

    public virtual void Play_Sound () { }
    public virtual void Play_CameraShake() { }
}
