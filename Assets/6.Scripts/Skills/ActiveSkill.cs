using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;


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
    , ICooldownable
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

    protected List<GameObject> trackedEffects = new List<GameObject>();

    /// <summary>
    /// AI가 사용할 때 해당 스킬 패턴의 기준을 정리하는 값 
    /// </summary>
    protected float range = 0.0f;
    public float Range => range;

    public bool IsOnCooldown => currentCooldown > 0;
    protected float limitCooldown;
    protected float initCooldown;
    protected float maxCooldown;
    protected float castingTime;
    protected float currentCastingTime;
    protected bool isCasting = false;
    protected int expectedAnimEventPhaseIndex = 0;
    public bool isWaitingForRelease = false;
    protected float chargeStartTime = 0f;

    public bool IsCasting { get => isCasting; set => isCasting = value; }
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
    // 이 스킬이 다른 행동 중에도 쓸 수 있는 '즉발/동시 사용' 스킬인가?
    public bool isConcurrentSkill = false;
    // 각 페이즈별로 애니메이션 유무를 미리 저장해둘 캐싱 배열
    private bool[] cachedActionDataFlags;

    // 모듈들의 비동기 타이머를 관리할 토큰 소스 
    protected CancellationTokenSource phaseCts;
    public CancellationToken PhaseToken => phaseCts?.Token ?? default;

    public ActiveSkill(SO_SkillData skillData)
        : base(skillData)
    {
        Blackboard = new();
        if (skillData is SO_ActiveSkillData activeSkillData)
        {
            this.range = activeSkillData.range;
            phaseList = activeSkillData.phaseList;
            this.limitCooldown = activeSkillData.limitCooldown;
            this.maxCooldown = activeSkillData.cooldown;
            this.castingTime = activeSkillData.castingTime;
            this.isConcurrentSkill = activeSkillData.isConcurrentSkill;
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

        skillComponent = ownerObject.GetComponent<SkillComponent>();

        foreach (var phase in phaseList)
        {
            phase.actionData?.Initialize();
        }

        CacheActionDataFlags();
    }

    private void CacheActionDataFlags()
    {
        if (phaseList == null) return;

        // 페이즈 개수만큼 배열을 만듭니다.
        cachedActionDataFlags = new bool[phaseList.Count];

        for (int i = 0; i < phaseList.Count; i++)
        {
            // 아까 만들었던 그 복잡한 2중 검사 로직을 여기서 '딱 한 번만' 돌립니다.
            cachedActionDataFlags[i] = CalculateHasActionData(i);
        }
    }

    private bool CalculateHasActionData(int index)
    {
        var phase = phaseList[index];

        if (phase.isInstant) return false;

        if (phase.actionData != null && !string.IsNullOrEmpty(phase.actionData.SubStateName))
            return true;

        if (phase.modules != null)
        {
            foreach (var module in phase.modules)
            {
                if (module != null && module.HasAnimationData())
                    return true;
            }
        }
        return false;
    }

    public bool HasActionData(int index)
    {
        if (cachedActionDataFlags != null && index >= 0 && index < cachedActionDataFlags.Length)
        {
            // 루프? 검사? 아무것도 안 합니다. 그냥 미리 구해둔 정답지를 제출합니다! O(1)
            return cachedActionDataFlags[index];
        }
        return false;
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

        phaseCts?.Cancel();
        phaseCts?.Dispose();
        phaseCts = new CancellationTokenSource();

        isCasting = true;
        isWaitingForRelease = false;
        chargeStartTime = Time.time;

        if (!isConcurrentSkill)
        {
            NavMeshAgent agent = ownerObject?.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.updateRotation = false;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // 상태 변경 
            if (state != null)
                state.SetActionMode();

            // 첫 번째 페이즈 
            ExecutePhase(0);
        }

        // 쿨타임 
        currentCooldown = initCooldown;

    }

    public virtual void Update(float deltaTime) { }
    public virtual void EndPhaseAndNext() { }   // 페이즈를 종료 후 넘기는 처리 

    public void JumpToPhase(int index)
    {
        if (phaseList.Count <= index) return;
        ExecutePhase(index);
    }
    protected virtual void ExecutePhase(int phaseIndex)
    {
        expectedAnimEventPhaseIndex = phaseIndex;
    }

    protected abstract void ApplyEffects();     // 개별 효과 적용 


    // 키를 뗐을 때 호출되는 함수
    public virtual void OnReleaseKey()
    {
        // 차징 중(0페이즈)일 때 키를 뗐다면?
        if (isWaitingForRelease)
        {
            isWaitingForRelease = false;

            // 💡 [핵심] 몇 초나 모았는지 계산해서 블랙보드에 저장! 
            // (나중에 투사체 모듈이 이 값을 보고 데미지나 크기를 키울 수 있습니다)
            float chargedTime = Time.time - chargeStartTime;
            Blackboard["ChargedTime"] = chargedTime;

            // 차징을 끝내고 발사 페이즈(1페이즈)로 강제로 넘깁니다!
            EndPhaseAndNext();
        }
    }


    //현재 페이즈가 장판처럼 "스스로 페이즈를 끝내는" 능력이 있는지 확인.
    public bool DoesPhaseControlItself(int index)
    {
        if (index >= 0 && index < phaseList.Count)
        {
            foreach (var mod in phaseList[index].modules)
            {
                // 장판 모듈은 OnEndSign 델리게이트를 통해 스스로 EndPhaseAndNext를 부르므로 true!
                if (mod is Module_SpawnWarningSign ||
                    mod is Module_PhaseTransition ||
                    mod is Module_ChargeWait)
                    return true;
            }
        }
        return false;
    }

    // 모듈이 무언가를 소환하면 여기에 신고(등록)하게 만듭니다.
    public void AddTrackedEffect(GameObject effect)
    {
        if (effect != null && !trackedEffects.Contains(effect))
        {
            trackedEffects.Add(effect);
        }
    }

    public virtual void Start_DoAction()
    {

    }
    public virtual void Begin_DoAction()
    {

    }
    public virtual void End_DoAction()
    {
        if (expectedAnimEventPhaseIndex != phaseIndex)
        {
            return;
        }

        foreach (var effect in trackedEffects)
        {
            if (effect != null && effect.activeInHierarchy)
            {
                effect.gameObject.SetActive(false);
            }
        }

        // 다음 번 스킬 사용을 위해 리스트를 비워줍니다.
        trackedEffects.Clear();


        phaseCts?.Cancel();

        // 장판 등이 진행 중이여서 다음 페이즈가 남아있다면,
        // 애니메이션이 끝났다고 해서 취소하지 않은다. 
        if (phaseIndex < phaseList.Count - 1)
        {
            return;
        }

        // 다음 번 스킬을 위해 초기화
        phaseIndex = 0;
        isCasting = false;

        if (!isConcurrentSkill && ownerObject != null)
        {
            NavMeshAgent agent = ownerObject.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
            }

            if (state != null)
                state.SetIdleMode();
        }
    }

    public virtual void Begin_JudgeAttack(AnimationEvent e)
    {
        if (ownerCharacter != null)
            ownerCharacter.BroadcastAttack(phaseList[phaseIndex].actionData, ownerCharacter);
    }
    public virtual void End_JudgeAttack(AnimationEvent e) { }

    public virtual void Play_Sound()
    {
        phaseSkill?.actionData?.Play_Sound();
    }
    public virtual void Play_CameraShake()
    {
        phaseSkill?.actionData?.Play_CameraShake();
    }
}
