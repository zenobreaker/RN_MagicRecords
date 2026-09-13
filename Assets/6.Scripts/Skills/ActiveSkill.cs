using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
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

[System.Serializable]
public abstract class ActiveSkill
    : Skill
    , ICooldownable
{
    [Header("Skill Action")]
    public ActionData actionData;

    [Header("Damage Data")]
    public DamageData damageData;

    [Header("Option List")]
    public List<int> bonusOptionList;

    protected int phaseIndex;
    protected List<PhaseSkill> phaseList;
    protected PhaseSkill phaseSkill;
    protected float currentCooldown;

    protected GameObject ownerObject;
    protected Character ownerCharacter;
    public Character Owner { get { return ownerCharacter; } }
    protected WeaponController weaponController;
    protected SkillComponent skillComponent;
    protected StateComponent state;
    protected StatusComponent status;
    public StatusComponent Status { get { return status; } }

    protected List<GameObject> trackedEffects = new List<GameObject>();

    /// <summary>
    /// AI가 사용할 때 해당 스킬 패턴의 기준을 정리하는 값 
    /// </summary>
    protected float range = 0.0f;
    public float Range => range;

    public bool IsOnCooldown => currentCooldown > 0;
    protected float limitCooldown;
    protected float maxCooldown;
    protected float castingTime;
    protected float currentCastingTime;
    protected bool isCasting = false;
    protected int expectedAnimEventPhaseIndex = 0;
    public bool isWaitingForRelease = false;
    protected float chargeStartTime = 0f;

    public bool IsCasting { get => isCasting; set => isCasting = value; }
    public event Action<ActiveSkill> OnCastingCompleted;
    public float CurrentCooldown { get => currentCooldown; }
    public float MaxCooldown { get => maxCooldown; }
    public SkillRuntimeContext Runtime { get; protected set; } = new SkillRuntimeContext();
    public bool IsPhaseRunning { get; protected set; }
    public int MaxPhaseCount
    {
        get
        {
            return (phaseList != null && phaseList.Count > 0) ? phaseList.Count : 1;
        }
    }
    public int PhaseIndex
    {
        get { return phaseIndex; }
        set { phaseIndex = value; }
    }
    // 이 스킬이 다른 행동 중에도 쓸 수 있는 '즉발/동시 사용' 스킬인가?
    public bool isConcurrentSkill = false;
    // 각 페이즈별로 애니메이션 유무를 미리 저장해둘 캐싱 배열
    private bool[] cachedActionDataFlags;

    // 모듈들의 비동기 타이머를 관리할 토큰 소스 
    protected CancellationTokenSource phaseCts;
    private CancellationTokenSource skillCts;
    public CancellationToken SkillToken => skillCts?.Token ?? new CancellationToken(true);
    public CancellationToken PhaseToken => phaseCts?.Token ?? new CancellationToken(true);
    public bool IsActive { get; private set; }
    public bool IsEnding { get; private set; }
    public float PhaseElapsedTime { get; private set; }
    public int PhaseVersion { get; private set; }
    private bool enteringPhase;
    private int pendingPhaseIndex = -1;
    private int transitionFrame = -1;
    private int transitionsThisFrame;

    public bool IsValidPhaseIndex(int index) => phaseList != null && index >= 0 && index < phaseList.Count;
    public bool IsInstantPhase(int index) => IsValidPhaseIndex(index) && phaseList[index].isInstant;
    public bool IsCurrentPhase(int version) => IsActive && !IsEnding && IsPhaseRunning && PhaseVersion == version;

    protected readonly ActiveSkillData activeSkillData;

    public ActiveSkill(SO_SkillData skillData)
        : base(skillData)
    {

        if (skillData is SO_ActiveSkillData activeSkillData)
        {
            phaseList = activeSkillData.phaseList;
            this.isConcurrentSkill = activeSkillData.isConcurrentSkill;
            LevelDatas = activeSkillData.levelDatas;

            if (LevelDatas.Count > 0)
                ApplyLevelData(LevelDatas[0]);
        }
    }

    public override void SetLevel(int level)
    {
        base.SetLevel(level);

        int index = GetSkillLevel();

        if (LevelDatas.Count > 0)
            ApplyLevelData(LevelDatas[index]);
    }

    protected virtual void ApplyLevelData(SkillLevelData levelData)
    {
        int overLevel = Mathf.Max(0, skillLevel - LevelDatas.Count - 1);

        range = levelData.range;

        limitCooldown = levelData.limitMinCooldown;
        maxCooldown = levelData.cooldown;
        castingTime = levelData.castingTime;

        damageData = levelData.damageData.Clone();
        // TODO : 레벨이 초과된 상태라면 데미지를 추가부여할지 ... 
        //damageData.baseDamage = damageData.baseDamage * (1f + overLevel * 0.1f);
        //damageData.statCoefficient = damageData.statCoefficient + overLevel * 0.05f;


        bonusOptionList = levelData.bonusOptionList;
    }

    public virtual void NotifyMovement(SkillTriggerTime timing, float elapsed = 0f) { }

    public virtual void SetOwner(GameObject gameObject)
    {
        ownerObject = gameObject;
        ownerCharacter = gameObject.GetComponent<Character>();
        state = gameObject.GetComponent<StateComponent>();
        status = gameObject.GetComponent<StatusComponent>();

        if (ownerObject.TryGetComponent(out IWeaponUser user))
        {
            weaponController = user.GetWeaponController();
        }

        skillComponent = ownerObject.GetComponent<SkillComponent>();

        actionData?.Initialize();

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

        if (actionData != null && !string.IsNullOrEmpty(actionData.SubStateName))
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

        IsPhaseRunning = true; 
        this.phaseIndex = phaseIndex;
        phaseSkill = phaseList[phaseIndex];
    }

    public void InitializedData()
    {
        int index = GetSkillLevel();
        if (LevelDatas.Count <= 0) return;

        ApplyLevelData(LevelDatas[index]);

        maxCooldown = LevelDatas[index].cooldown;
        limitCooldown = LevelDatas[index].limitMinCooldown;

        currentCastingTime = castingTime;
    }

    private void SetCooldown()
    {
        //TODO : Runtime에 있는 cooldown 감소량 반영
        currentCooldown = Mathf.Max(limitCooldown, MaxCooldown);
    }

    public void Update_Cooldown(float deltaTime)
    {
        if (currentCooldown > 0)
            currentCooldown -= deltaTime;
    }

    public void Cast(int startPhaseIndex = 0)
    {
        if (IsOnCooldown || isCasting || IsActive || IsEnding)
            return;

        if (phaseList == null || startPhaseIndex < 0 || startPhaseIndex >= phaseList.Count)
            return;

        CancelToken(ref phaseCts);
        CancelToken(ref skillCts);
        skillCts = ownerCharacter != null
            ? CancellationTokenSource.CreateLinkedTokenSource(ownerCharacter.GetCancellationTokenOnDestroy())
            : new CancellationTokenSource();
        IsActive = true;
        phaseCts = CancellationTokenSource.CreateLinkedTokenSource(SkillToken);

        // 새 사용은 이전 종료 경로에 남은 페이즈를 이어받지 않습니다.
        phaseIndex = startPhaseIndex;
        expectedAnimEventPhaseIndex = startPhaseIndex;
        phaseSkill = phaseList[startPhaseIndex];
        IsPhaseRunning = false;

        SetRunTimeContext();

        if (isCasting == false)
        {
            SkillUseEvent evt = new SkillUseEvent
            {
                SkillID = this.skillID,
                SkillName = this.skillName,
                Owner = this.ownerCharacter
            };

            PassiveSystem ps = AppManager.Instance.SafeInvoke(v => v.GetPassiveSystem());
            ps?.BroadcastOnSkillCast(evt, this.Runtime);
        }

        isWaitingForRelease = false;
        chargeStartTime = Time.time;

        PrepareCasting();
        if (!IsActive || IsEnding) return;

        if (Runtime.Cast.CastingTime > 0f)
        {
            isCasting = true;
            currentCastingTime = Runtime.Cast.CastingTime;
            WaitForCastingAsync(Runtime.Cast.CastingTime, SkillToken).Forget();
        }
        else
        {
            BeginSkillAction();
        }

        // 쿨타임
        SetCooldown();
    }

    protected virtual void PrepareCasting() { }

    private async UniTaskVoid WaitForCastingAsync(float duration, CancellationToken token)
    {
        bool isCancelled = await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            cancellationToken: token).SuppressCancellationThrow();

        if (isCancelled || token.IsCancellationRequested || !IsActive)
            return;

        isCasting = false;
        currentCastingTime = 0f;
        BeginSkillAction();
        OnCastingCompleted?.Invoke(this);
    }

    private void BeginSkillAction()
    {
        if (isConcurrentSkill)
        {
            EnterPhase(PhaseIndex);
            return;
        }

        if (ownerObject.TryGetComponent<NavMeshAgent>(out var agent) && agent.isActiveAndEnabled)
        {
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (state != null)
            state.SetActionMode();

        EnterPhase(PhaseIndex);
    }

    private void SetRunTimeContext()
    {
        int index = GetSkillLevel();

        Runtime = new SkillRuntimeContext();

        if (LevelDatas.Count > 0)
        {
            // Base
            Runtime.Base = new BaseValues
            {
                PatternCount = LevelDatas[index].spawnCount,
                PatternAngle = LevelDatas[index].angle,
                TotalShots = 0,

                Damage = LevelDatas[index].damageData,
                Range = LevelDatas[index].range,
                Cooldown = LevelDatas[index].cooldown,
            };

            // Cast
            Runtime.Cast = new CastContext
            {
                CastingTime = LevelDatas[index].castingTime,
                MaxCastingTime = LevelDatas[index].castingTime,
                MaxChargeTime = LevelDatas[index].chargeTime,
            };
        }

        // Spawn
        Runtime.Spawn = new SpawnContext
        {
            SearchRadius = 0f,
            ChainCount = 0,
            ExplosionRadius = 0f,
            Lifetime = 0f,
            TargetPositions = new List<Vector3>(),
        };

        // Combat
        Runtime.Combat = new CombatContext
        {
            PatternCountBonus = 0,
            PatternAngleBonus = 0f,
            TotalShotsBonus = 0,
            FireIntervalMultiplier = 1.0f,
            BonusMultipiler = 1.0f,
            CriticalDamageMultiplier = 1.5f,
            IsCritical = false,
        };

        Runtime.Modifier = new ModifierContext();
    }

    public virtual void Update(float deltaTime)
    {
        if (!IsActive || IsEnding) return;
        if (IsPhaseRunning) PhaseElapsedTime += Mathf.Max(0f, deltaTime);
        if (isWaitingForRelease)
        {
            // 현재까지 누르고 있는 시간 계산
            Runtime.Cast.ChargedTime += deltaTime;

            if (Runtime.Cast.IsInstantCast ||
               (Runtime.Cast.AutoFireOnMaxCharge &&
               Runtime.Cast.ChargedTime >= Runtime.Cast.MaxChargeTime))
            {
                // 플레이어가 물리적으로 마우스를 떼지 않았어도, 내부적으로 뗀 것으로 간주하고 강제 실행!
                Debug.Log($"Active skill released by runtime");
                OnReleaseKey();
            }
        }
    }

    public virtual void FixedUpdate(float fixedDeltaTime)
    {

    }

    public virtual void EndPhaseAndNext()
    {
        if (!IsActive || IsEnding || isCasting || !IsPhaseRunning) return;
        if (IsValidPhaseIndex(phaseIndex + 1)) ChangePhase(phaseIndex + 1);
        else EndSkill();
    }

    public void JumpToPhase(int index) => ChangePhase(index);

    public bool ChangePhase(int index)
    {
        if (!IsActive || IsEnding || isCasting || !IsPhaseRunning || !IsValidPhaseIndex(index))
            return false;
        EnterPhase(index);
        return true;
    }

    public bool RestartCurrentPhase()
    {
        if (!IsActive || IsEnding || isCasting || !IsPhaseRunning) return false;
        EnterPhase(phaseIndex);
        return true;
    }

    // Change/restart both retire the previous generation before notifying new modules.
    private void EnterPhase(int index)
    {
        if (!IsActive || IsEnding || !IsValidPhaseIndex(index)) return;
        pendingPhaseIndex = index;
        if (enteringPhase) return;
        enteringPhase = true;
        try
        {
            while (pendingPhaseIndex >= 0 && IsActive && !IsEnding)
            {
                if (transitionFrame != Time.frameCount)
                {
                    transitionFrame = Time.frameCount;
                    transitionsThisFrame = 0;
                }
                if (++transitionsThisFrame > 64)
                {
                    Debug.LogError($"[{Name}] Too many phase transitions in one frame. Check instant phase cycles.");
                    EndSkill();
                    break;
                }
                int next = pendingPhaseIndex;
                pendingPhaseIndex = -1;
                CancelToken(ref phaseCts);
                if (IsPhaseRunning) OnPhaseExited();
                ClearTrackedEffects();
                Runtime.Hit.End();
                phaseCts = CancellationTokenSource.CreateLinkedTokenSource(SkillToken);
                PhaseVersion++;
                PhaseElapsedTime = 0f;
                isWaitingForRelease = false;
                expectedAnimEventPhaseIndex = next;
                SetCurrentPhaseSkill(next);
                OnPhaseEntered();
                ExecutePhase(next);
            }
        }
        finally { enteringPhase = false; }
    }

    protected bool HasPendingPhaseChange => pendingPhaseIndex >= 0;
    protected virtual void OnPhaseEntered() { }
    protected virtual void OnPhaseExited() { }
    protected virtual void OnSkillEnding() { }
    protected virtual void ExecutePhase(int index) { }

    private static void CancelToken(ref CancellationTokenSource source)
    {
        var previous = source;
        source = null;
        if (previous == null) return;
        previous.Cancel();
        previous.Dispose();
    }
    protected abstract void ApplyEffects();     // 개별 효과 적용 


    // 키를 뗐을 때 호출되는 함수
    public virtual void OnReleaseKey()
    {
        // 차징 중(0페이즈)일 때 키를 뗐다면?
        if (isWaitingForRelease)
        {
            isWaitingForRelease = false;

            // 💡 즉발(Instant) 모듈이 켜졌을 때만 '풀차징' 보너스를 강제로 줍니다.
            if (Runtime.Cast.IsInstantCast)
            {
                Runtime.Cast.ChargedTime = Runtime.Cast.MaxChargeTime;
            }
            // (즉발이 아니라면 Update에서 누적된 ChargedTime이 그대로 유지되므로 아무것도 안 해도 됨!)

            EndPhaseAndNext();
        }
    }


    //현재 페이즈가 장판처럼 "스스로 페이즈를 끝내는" 능력이 있는지 확인.
    public bool DoesPhaseControlItself(int index)
    {
        if (index < 0 || index >= phaseList.Count)
            return false;

        return (Runtime.ActivePhaseLoop != null && Runtime.PhaseLoopTargetIndex == index) || phaseList[index].DoesPhaseControlItself();
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
    // Animation completion is not necessarily skill completion (charge/loop/timer phases).
    public virtual void End_DoAction()
    {
        if (!IsActive || IsEnding || isCasting) return;
        if (IsPhaseRunning && DoesPhaseControlItself(phaseIndex)) return;
        EndSkill(false);
    }

    private void ClearTrackedEffects()
    {
        var effects = trackedEffects.ToArray();
        trackedEffects.Clear();
        foreach (var effect in effects)
            if (effect != null && effect.activeInHierarchy) effect.SetActive(false);
    }

    public void EndSkill(bool notifyOwner = true)
    {
        if (!IsActive || IsEnding) return;
        IsEnding = true;
        pendingPhaseIndex = -1;
        try
        {
            CancelToken(ref phaseCts);
            CancelToken(ref skillCts);
            OnSkillEnding();
            if (IsPhaseRunning) OnPhaseExited();
            ClearTrackedEffects();
            Runtime.Hit.End();
            Runtime.ResetPhaseLoopCounts();
            IsPhaseRunning = false;
            IsActive = false;
            isCasting = false;
            currentCastingTime = 0f;
            isWaitingForRelease = false;
            pendingPhaseIndex = -1;
            phaseIndex = 0;
            expectedAnimEventPhaseIndex = 0;
            phaseSkill = null;
            PhaseElapsedTime = 0f;
            if (ownerCharacter != null && ownerCharacter.TryGetComponent<SkillVFXComponent>(out var vfx))
                vfx.RemoveSkillEffects(this);
            if (!isConcurrentSkill && ownerObject != null)
            {
                var agent = ownerObject.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.updateRotation = true;
                    agent.isStopped = false;
                }
                if (state != null && !state.DamagedMode && !state.DeadMode && !state.StopMode) state.SetIdleMode();
            }
            if (notifyOwner && !isConcurrentSkill && skillComponent != null)
                skillComponent.CompleteSkillAction(this);
        }
        finally { IsEnding = false; }
    }
    public virtual void Begin_JudgeAttack(AnimationEvent e)
    {
        if (ownerCharacter != null)
            ownerCharacter.BroadcastAttack(actionData, ownerCharacter);
    }
    public virtual void End_JudgeAttack(AnimationEvent e) { }

    public virtual void Play_Sound()
    {
        actionData?.Play_Sound();
    }
    public virtual void Play_CameraShake()
    {
        actionData?.Play_CameraShake();
    }
}
