using System.Collections.Generic;
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

    public ActiveSkill()
    {
       
    }

    public ActiveSkill(SO_ActiveSkillData skillData)
    {
        skillId = skillData.id;
        skillSprite = skillData.skillImage;
        phaseList = skillData.phaseList;
        this.limitCooldown = skillData.limitCooldown;
        this.maxCooldown = skillData.cooldown;
        this.castingTime = skillData.castingTime;
    }

    public ActiveSkill(float limitCooldown, float maxCooldown, float castingTime)
    {
        this.limitCooldown = limitCooldown;
        this.maxCooldown = maxCooldown;
        this.castingTime = castingTime;
    }

    protected int skillId; 
    public int SkillId { get { return skillId; } }
    public Sprite skillSprite; 
    //protected SO_ActiveSkillData skillData;
    //public SO_ActiveSkillData SO_SkillData { get => skillData; set => skillData = value; }

   // protected SkillPhase skillPhase;
    protected int phaseIndex;
    protected List<PhaseSkill> phaseList;
    protected PhaseSkill phaseSkill;
    protected float currentCooldown;

    protected GameObject ownerObject;
    protected Animator animator;
    protected WeaponController weaponController;

    public bool IsOnCooldown => currentCooldown > 0;
    protected float limitCooldown; 
    protected float initCooldown;
    protected float maxCooldown;
    protected float castingTime;
    protected float currentCastingTime;

    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => maxCooldown; }


    public virtual void SetOwner(GameObject gameObject)
    {
        ownerObject = gameObject;
        animator = gameObject.GetComponent<Animator>();

        if (ownerObject.TryGetComponent(out IWeaponUser user))
        {
            weaponController = user.GetWeaponController();
        }

        foreach (var phase in phaseList)
        {
            phase.actionData?.Initialize();
        }
    }

    protected void SetCurrentPhaseSkill(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phaseList.Count)
            return;

        this.phaseIndex = phaseIndex;
        phaseSkill = phaseList[phaseIndex];
    }

    public void InitializedData()
    {
        maxCooldown = initCooldown = limitCooldown;
        currentCastingTime = castingTime;
    }

    public void SetCooldown(float cooldown)
    {
        initCooldown = cooldown;
    }

    public void Update_Cooldown(float deltaTime)
    {
        if (currentCooldown > 0)
            currentCooldown -= deltaTime;
    }

    public void Cast()
    {
        if (IsOnCooldown)
            return;

        //TODO: 스킬 캐스팅

        // 쿨타임 
        currentCooldown = initCooldown;

        // 첫 번째 페이즈 
        ExecutePhase(0);
    }

    public virtual void Update(float deltaTime) { }

    protected abstract void ExecutePhase(int phaseIndex);
    protected abstract void ApplyEffects();     // 개별 효과 적용 

    public virtual void Start_DoAction() { }
    public virtual void Begin_DoAction() { }
    public virtual void End_DoAction() { }

    public virtual void Begin_JudgeAttack(AnimationEvent e) { }
    public virtual void End_JudgeAttack(AnimationEvent e) { }

    public virtual void Play_Sound () { }
    public virtual void Play_CameraShake() { }
}
