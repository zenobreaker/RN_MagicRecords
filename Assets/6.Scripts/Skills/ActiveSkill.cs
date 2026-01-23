using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public enum SkillPhase
{
    Start = 0,
    Casting,
    Action,
    Finish,
    MAX,
}

//// 방법 A: 자주 쓰는 변수는 명시적 변수로 빼두기
//public class SkillBlackboard
//{
//    public bool isCrit;
//    public float chargeAmount;
//    // ... 필요한 것들만
//}

//// 방법 B: 정수/실수/불리언을 담는 전용 구조체 사용
//public struct BlackboardValue
//{
//    public float floatVal;
//    public bool boolVal;
//    // 이렇게 하면 object(참조형)를 안 써서 박싱이 안 일어납니다.
//}


[System.Serializable]
public abstract class ActiveSkill 
    : Skill
    ,ICooldownable
{
    protected int phaseIndex;
    protected List<PhaseSkill> phaseList;
    protected PhaseSkill phaseSkill;
    protected float currentCooldown;

    protected GameObject ownerObject;
    protected Character ownerCharacter;
    protected WeaponController weaponController;
    protected SkillComponent skillComponent;
    protected StateComponent state; 

    public bool IsOnCooldown => currentCooldown > 0;
    protected float limitCooldown; 
    protected float initCooldown;
    protected float maxCooldown;
    protected float castingTime;
    protected float currentCastingTime;

    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => maxCooldown; }
    public Dictionary<string, object> Blackboard; 
    public ActiveSkill(SO_SkillData skillData)
        : base(skillData)
    {
        Blackboard = new(); 
        if (skillData is SO_ActiveSkillData activeSkillData)
        {
            phaseList = activeSkillData.phaseList;
            this.limitCooldown = activeSkillData.limitCooldown;
            this.maxCooldown = activeSkillData.cooldown;
            this.castingTime = activeSkillData.castingTime;
        }
    }


    public virtual void SetOwner(GameObject gameObject)
    {
        ownerObject = gameObject;
        ownerCharacter = gameObject.GetComponent<Character>();
        state = gameObject.GetComponent<StateComponent>(); 

        if (ownerObject.TryGetComponent(out IWeaponUser user))
        {
            weaponController = user.GetWeaponController();
        }

         skillComponent  = ownerObject.GetComponent<SkillComponent>();  

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

        NavMeshAgent agent = ownerObject?.GetComponent<NavMeshAgent>(); 
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 쿨타임 
        currentCooldown = initCooldown;

        // 상태 변경 
        state?.SetActionMode(); 

        // 첫 번째 페이즈 
        ExecutePhase(0);
    }

    public virtual void Update(float deltaTime) { }
    public virtual void EndPhaseAndNext() { }   // 페이즈를 종료 후 넘기는 처리 
    protected abstract void ExecutePhase(int phaseIndex);
    protected abstract void ApplyEffects();     // 개별 효과 적용 

    public virtual void Start_DoAction() { }
    public virtual void Begin_DoAction() { }
    public virtual void End_DoAction() 
    {
        NavMeshAgent agent = ownerObject?.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        state?.SetIdleMode(); 
    }

    public virtual void Begin_JudgeAttack(AnimationEvent e) { }
    public virtual void End_JudgeAttack(AnimationEvent e) { }

    public virtual void Play_Sound () 
    {
        phaseSkill?.actionData?.Play_Sound();
    }
    public virtual void Play_CameraShake() 
    {
        phaseSkill?.actionData?.Play_CameraShake();
    }
}
