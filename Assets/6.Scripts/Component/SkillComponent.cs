using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class SkillComponent : ActionComponent
{
    private string currentSlotName = string.Empty;

    // 장착 스킬 정보
    private Dictionary<string, ActiveSkill> skillSlotTable;

    // 어떤 인터페이스든 구현체를 저장
    private readonly Dictionary<Type, object> capabilityTable = new();


    public event Action<bool> OnSkillUse;
    public event Action<SkillSlot, ActiveSkill> OnActiveSkillChanged;
    public event Action<SkillSlot, bool> OnActiveSkillCooldownChanged;
    public event Action<SkillSlot, float, float> OnActiveSkillCooldownUpdated;


    private void Awake()
    {
        rootObject = transform.root.gameObject;

        Awake_SkillSlotTable();
    }


    private void Awake_SkillSlotTable()
    {
        skillSlotTable = new Dictionary<string, ActiveSkill>
        {
            { "DEFAULT", null },

            { "SLOT1", null },
            { "SLOT2", null },
            { "SLOT3", null },
            { "SLOT4", null },
            { nameof(SkillSlot.SubAction), null },
        };
    }


    protected override void OnDisable()
    {
        if (skillSlotTable != null)
            foreach (var skill in skillSlotTable.Values) skill?.EndSkill(false);
        currentSlotName = string.Empty;
        base.EndDoAction();
        base.OnDisable();
    }

    public void CancelCurrentSkill()
    {
        if (skillSlotTable != null && skillSlotTable.TryGetValue(currentSlotName, out var skill))
            skill?.EndSkill();
    }

    public void CompleteSkillAction(ActiveSkill completedSkill)
    {
        if (string.IsNullOrEmpty(currentSlotName) ||
            !skillSlotTable.TryGetValue(currentSlotName, out var current) ||
            !ReferenceEquals(current, completedSkill)) return;
        var character = rootObject != null ? rootObject.GetComponent<Character>() : null;
        if (character != null && InAction) character.End_DoAction();
        else EndDoAction();
    }

    private void FixedUpdate()
    {
        if (skillSlotTable == null)
            return;

        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null)
                continue;

            pair.Value.FixedUpdate(Time.fixedDeltaTime);
        }
    }


    private void Update()
    {
        if (skillSlotTable == null)
            return;

        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            ActiveSkill skill = pair.Value;

            // 스킬이 비어있으면 처리하지 않음
            if (skill == null)
                continue;

            // Dictionary Key를 SkillSlot으로 변환
            if (!Enum.TryParse(pair.Key, out SkillSlot currentSlot))
                continue;

            // 스킬 로직 업데이트
            skill.Update(Time.deltaTime);

            bool isCooldown = skill.IsOnCooldown;

            NotifyCooldownState(currentSlot, skill, isCooldown);

            if (!isCooldown)
                continue;

            // 쿨타임 업데이트
            skill.Update_Cooldown(Time.deltaTime);

            NotifyCooldownProgress(currentSlot, skill);
        }
    }

    private void NotifyCooldownState(
        SkillSlot slot,
        ActiveSkill skill,
        bool? isCooldown = null,
        bool notifyProgressImmediately = false)
    {
        if (skill == null)
            return;

        bool value = isCooldown ?? skill.IsOnCooldown;

        SkillManager.Instance.SafeInvoke(v =>
            v.NotifySkillCooldownState(slot, value));

        OnActiveSkillCooldownChanged?.Invoke(slot, value);

        if (value && notifyProgressImmediately)
            NotifyCooldownProgress(slot, skill);
    }

    private void NotifyCooldownState(string slotName, ActiveSkill skill)
    {
        if (Enum.TryParse(slotName, out SkillSlot slot))
            NotifyCooldownState(slot, skill, notifyProgressImmediately: true);
    }

    private void NotifyCooldownProgress(SkillSlot slot, ActiveSkill skill)
    {
        if (skill == null)
            return;

        SkillManager.Instance.SafeInvoke(v =>
            v.NotifySkillCooldown(slot, skill.CurrentCooldown, skill.MaxCooldown));

        OnActiveSkillCooldownUpdated?.Invoke(
            slot,
            skill.CurrentCooldown,
            skill.MaxCooldown);
    }


    ///////////////////////////////////////////////////////////////////////////
    #region CAPABILITY

    // 기능 등록
    public void RegisterCapability<T>(T capability)
        where T : class
    {
        var type = typeof(T);

        if (capabilityTable.ContainsKey(type))
        {
            capabilityTable[type] = capability;
        }
        else
        {
            capabilityTable.Add(type, capability);
        }
    }


    // 기능 조회
    public T GetCapability<T>()
        where T : class
    {
        var type = typeof(T);

        if (capabilityTable.TryGetValue(
                type,
                out object value))
        {
            return (T)value;
        }

        return null;
    }


    // 기능 해제
    public void UnregisterCapability<T>()
    {
        var type = typeof(T);

        if (capabilityTable.ContainsKey(type))
        {
            capabilityTable.Remove(type);
        }
    }

    #endregion


    ///////////////////////////////////////////////////////////////////////////
    #region SKILL

    public void ReleaseSkill(string slot)
    {
        if (currentSlotName != slot)
            return;

        if (skillSlotTable.TryGetValue(
                slot,
                out ActiveSkill skill))
        {
            skill?.OnReleaseKey();
        }
    }


    protected override async UniTaskVoid ManualActionRoutine(
        CancellationToken token)
    {
        try
        {
            // DoAction starts this routine before Cast initializes the skill.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            BeginDoAction();

            if (string.IsNullOrEmpty(currentSlotName) ||
                !skillSlotTable.TryGetValue(
                    currentSlotName,
                    out ActiveSkill currentSkill) ||
                currentSkill == null)
            {
                EndDoAction();
                return;
            }

            // 캐스팅 종료 대기
            while (currentSkill.IsCasting)
            {
                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken: token);
            }


            while (true)
            {
                if (!currentSkill.IsActive) break;
                int phaseVersion = currentSkill.PhaseVersion;
                int phaseIndex =
                    currentSkill.PhaseIndex;

                // 범위를 벗어나면 종료
                if (phaseIndex < 0 ||
                    phaseIndex >= currentSkill.MaxPhaseCount)
                {
                    break;
                }

                bool hasAnimation =
                    currentSkill.HasActionData(
                        phaseIndex);


                // Animation이 있는 경우 선딜레이
                if (hasAnimation)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(0.2f),
                        cancellationToken: token);
                }


                // A timer/loop may have restarted the same index during the await.
                if (!currentSkill.IsCurrentPhase(phaseVersion)) continue;
                BeginJudgeAttack(null);
                if (!currentSkill.IsCurrentPhase(phaseVersion)) continue;
                EndJudgeAttack(null);
                if (!currentSkill.IsCurrentPhase(phaseVersion)) continue;


                // 현재 Phase가 스스로 종료되는지 확인
                bool isSelfControlled =
                    currentSkill.DoesPhaseControlItself(
                        phaseIndex);

                if (isSelfControlled)
                {
                    while (currentSkill != null && currentSkill.IsActive &&
                           currentSkill.PhaseVersion == phaseVersion)
                    {
                        await UniTask.Yield(
                            PlayerLoopTiming.Update,
                            cancellationToken: token);
                    }

                    continue;
                }


                // 일반 Phase
                if (hasAnimation)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(0.3f),
                        cancellationToken: token);
                }
                else
                {
                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken: token);
                }


                if (!currentSkill.IsCurrentPhase(phaseVersion)) continue;

                // 현재 Phase가 마지막인지 확인
                if (currentSkill.PhaseIndex >=
                    currentSkill.MaxPhaseCount - 1)
                {
                    break;
                }


                // 다음 Phase로 이동
                currentSkill.EndPhaseAndNext();

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken: token);
            }

            EndDoAction();
        }
        catch (OperationCanceledException)
        {
            // 정상적인 스킬 취소
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"<color=red>스킬 실행 중 치명적 에러</color>\n{e}");

            EndDoAction();
        }
    }


    private bool CanUseSlot(string slotName)
    {
        if (slotName != nameof(SkillSlot.SubAction)) return true;
        if (!isActiveAndEnabled) return false;
        var character = rootObject != null ? rootObject.GetComponent<Character>() : null;
        var state = rootObject != null ? rootObject.GetComponent<StateComponent>() : null;
        return character != null && character.isActiveAndEnabled &&
               state != null && state.IdleMode && !InAction;
    }

    public bool CanUseSkill(string skillName)
    {
        if (CanUseSlot(skillName) && skillSlotTable != null && skillSlotTable.TryGetValue(
                skillName,
                out var skill))
        {
            return skill != null &&
                   skill.IsOnCooldown == false && !skill.IsActive && !skill.IsCasting && !skill.IsEnding &&
                   InAction == false;
        }

        return false;
    }


    // SkillSlot을 이용한 스킬 장착
    public void SetActiveSkill(
        SkillSlot slot,
        ActiveSkill skill)
    {
        SetActiveSkill(
            slot.ToString(),
            skill);

        // SkillComponent 자체 이벤트
        OnActiveSkillChanged?
            .Invoke(
                slot,
                skill);

        // 외부 전달은 SkillManager에게 위임
        SkillManager.Instance?
            .NotifyActiveSkillChanged(
                slot,
                skill);
    }


    public bool TryGetRegisteredActiveSkill(
        SkillSlot slot,
        out ActiveSkill skill)
    {
        skill = null;

        // 기본 공격은 내부 전용 슬롯
        if (slot != SkillSlot.SubAction && (slot < SkillSlot.SLOT1 ||
            slot > SkillSlot.SLOT4))
        {
            return false;
        }

        return skillSlotTable.TryGetValue(
                   slot.ToString(),
                   out skill) &&
               skill != null;
    }


    // 실제 스킬 등록
    public void SetActiveSkill(
        string slotName,
        ActiveSkill skill)
    {
        if (skillSlotTable.TryGetValue(slotName, out var previousSkill) && !ReferenceEquals(previousSkill, skill))
            previousSkill?.EndSkill();

        if (skillSlotTable.ContainsKey(slotName))
        {
            skillSlotTable[slotName] = skill;
        }
        else
        {
            skillSlotTable.Add(
                slotName,
                skill);
        }

        skillSlotTable[slotName]?.SetOwner(rootObject);
        skillSlotTable[slotName]?.InitializedData();
    }


    // 슬롯에 등록된 스킬 사용
    public void UseSkill(
        SkillSlot slot,
        int phaseIndex = -1)
    {
        UseSkill(
            slot.ToString(),
            phaseIndex);
    }


    public void UseSkill(
        string slotName,
        int phaseIndex = -1)
    {
        if (!CanUseSlot(slotName) || skillSlotTable == null || !skillSlotTable.TryGetValue(
                slotName,
                out var skill) ||
            skill == null)
        {
            return;
        }


        int startPhaseIndex = phaseIndex < 0 ? 0 : phaseIndex;
        if (startPhaseIndex >= skill.MaxPhaseCount)
            return;


        // 동시 사용 가능 스킬
        if (skill.isConcurrentSkill)
        {
            if (skill.IsOnCooldown || skill.IsActive || skill.IsCasting || skill.IsEnding)
                return;

            skill.Cast(startPhaseIndex);
            NotifyCooldownState(slotName, skill);

            ExecuteConcurrentSkillAsync(
                skill).Forget();

            return;
        }


        if (InAction ||
            CanUseSkill(slotName) == false)
        {
            OnSkillUse?.Invoke(false);
            return;
        }


        currentSlotName = slotName;

        OnSkillUse?.Invoke(true);

        base.DoAction();

        skill.Cast(startPhaseIndex);
        NotifyCooldownState(slotName, skill);


        if (useAnimationEvents && !skill.HasActionData(
                skill.PhaseIndex))
        {
            SimulateAnimationEventsAsync(
                skill).Forget();
        }
    }


    private async UniTaskVoid SimulateAnimationEventsAsync(ActiveSkill skill)
    {
        if (skill == null || !skill.IsActive) return;
        CancellationToken lifetime = skill.SkillToken;
        try
        {
            while (skill.IsCasting)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: lifetime);
            if (!skill.IsActive || lifetime.IsCancellationRequested || skill.DoesPhaseControlItself(skill.PhaseIndex)) return;
            int version = skill.PhaseVersion;
            CancellationToken phaseToken = skill.PhaseToken;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: phaseToken);
            if (!skill.IsCurrentPhase(version)) return;
            skill.Begin_JudgeAttack(null);
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: phaseToken);
            if (!skill.IsCurrentPhase(version)) return;
            skill.End_JudgeAttack(null);
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: phaseToken);
            if (skill.IsCurrentPhase(version)) EndDoAction();
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (!lifetime.IsCancellationRequested) skill.EndSkill();
        }
    }

    private async UniTaskVoid ExecuteConcurrentSkillAsync(ActiveSkill skill)
    {
        if (skill == null || !skill.IsActive) return;
        CancellationToken lifetime = skill.SkillToken;
        try
        {
            while (skill.IsCasting)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: lifetime);
            while (skill.IsActive && !lifetime.IsCancellationRequested)
            {
                if (skill.DoesPhaseControlItself(skill.PhaseIndex)) return;
                int version = skill.PhaseVersion;
                CancellationToken phaseToken = skill.PhaseToken;
                skill.Begin_JudgeAttack(null);
                if (!skill.IsCurrentPhase(version)) continue;
                skill.End_JudgeAttack(null);
                if (!skill.IsCurrentPhase(version)) continue;
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: phaseToken);
                if (skill.IsCurrentPhase(version)) skill.EndPhaseAndNext();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (!lifetime.IsCancellationRequested) skill.EndSkill(false);
        }
    }
    #endregion


    ///////////////////////////////////////////////////////////////////////////
    #region ACTION

    public override void StartAction()
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.StartAction();

        skillSlotTable[currentSlotName]
            ?.Start_DoAction();
    }


    public override void BeginDoAction()
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.BeginDoAction();

        skillSlotTable[currentSlotName]
            ?.Begin_DoAction();

        OnBeginDoAction?.Invoke();

        // SkillManager를 통해 전달
        SkillManager.Instance?
            .NotifySkillUseBegin();
    }


    public override void EndDoAction()
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        ActiveSkill skill =
            skillSlotTable[currentSlotName];

        skill?.End_DoAction();

        if (string.IsNullOrEmpty(currentSlotName)) return;

        if (skill != null &&
            skill.IsActive)
        {
            return;
        }

        base.EndDoAction();

        currentSlotName = string.Empty;

        OnEndDoAction?.Invoke();

        // SkillManager를 통해 전달
        SkillManager.Instance?
            .NotifySkillUseEnd();
    }


    public override void BeginJudgeAttack(
        AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.BeginJudgeAttack(e);

        skillSlotTable[currentSlotName]
            ?.Begin_JudgeAttack(e);
    }


    public override void EndJudgeAttack(
        AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.EndJudgeAttack(e);

        skillSlotTable[currentSlotName]
            ?.End_JudgeAttack(e);
    }


    public override void PlaySound()
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.PlaySound();

        skillSlotTable[currentSlotName]
            ?.Play_Sound();
    }


    public override void PlayCameraShake()
    {
        if (string.IsNullOrEmpty(currentSlotName))
            return;

        base.PlayCameraShake();

        skillSlotTable[currentSlotName]
            ?.Play_CameraShake();
    }

    #endregion


    ///////////////////////////////////////////////////////////////////////////
    #region NOTIFY

    public void NotifyBulletInit(int bulletCount)
    {
        SkillManager.Instance.SafeInvoke(v=>
        v.NotifyMagicBulletLoad(
                bulletCount)); 
            
    }


    public void NotifyMagicBulletChanged(
        Queue<BulletData> bullets)
    {
        SkillManager.Instance.SafeInvoke(v => 
        v.NotifyMagicBulletChanged(
                bullets));
    }

    #endregion
}
