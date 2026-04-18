using System.Collections.Generic;
using System.Linq.Expressions;
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
    protected bool isCasting = false; 

    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => maxCooldown; }
    public Dictionary<string, object> Blackboard;
    public int MaxPhaseCount
    {
        get
        {
            return (phaseList != null && phaseList.Count > 0) ? phaseList.Count : 1;
        }
    }
    public int PhaseIndex => phaseIndex;


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

        isCasting = true;   

        NavMeshAgent agent = ownerObject?.GetComponent<NavMeshAgent>(); 
        if (agent != null && agent.isActiveAndEnabled)
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
    
    //현재 페이즈가 장판처럼 "스스로 페이즈를 끝내는" 능력이 있는지 확인.
    public bool DoesPhaseControlItself(int index)
    {
        if (index >= 0 && index < phaseList.Count)
        {
            foreach (var mod in phaseList[index].modules)
            {
                // 장판 모듈은 OnEndSign 델리게이트를 통해 스스로 EndPhaseAndNext를 부르므로 true!
                if (mod is Module_SpawnWarningSign)
                    return true;
            }
        }
        return false;
    }

    public virtual void Start_DoAction() 
    {
       
    }
    public virtual void Begin_DoAction() { }
    public virtual void End_DoAction() 
    {
        // 장판 등이 진행 중이여서 다음 페이즈가 남아있다면,
        // 애니메이션이 끝났다고 해서 취소하지 않은다. 
        if(phaseIndex < phaseList.Count -1)
        {
            phaseIndex++;
            return; 
        }

        isCasting = false;
        
        // 다음 번 스킬을 위해 초기화
        phaseIndex = 0; 

        NavMeshAgent agent = ownerObject?.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
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
