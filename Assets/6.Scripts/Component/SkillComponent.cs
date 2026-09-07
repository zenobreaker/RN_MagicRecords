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
        };
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

            // SkillManager를 통해 외부에 상태 전달
            SkillManager.Instance.SafeInvoke(v =>
              v.NotifySkillCooldownState(
                    currentSlot,
                    isCooldown));

            // 기존 SkillComponent 이벤트
            OnActiveSkillCooldownChanged?
                .Invoke(
                    currentSlot,
                    isCooldown);

            if (!isCooldown)
                continue;

            // 쿨타임 업데이트
            skill.Update_Cooldown(Time.deltaTime);

            // SkillManager를 통해 외부에 상태 전달
            SkillManager.Instance.SafeInvoke(v=>
                v.NotifySkillCooldown(
                    currentSlot,
                    skill.CurrentCooldown,
                    skill.MaxCooldown));

            // 기존 SkillComponent 이벤트
            OnActiveSkillCooldownUpdated?
                .Invoke(
                    currentSlot,
                    skill.CurrentCooldown,
                    skill.MaxCooldown);
        }
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


                // 현재 Phase 공격 이벤트
                BeginJudgeAttack(null);
                EndJudgeAttack(null);


                // 현재 Phase가 스스로 종료되는지 확인
                bool isSelfControlled =
                    currentSkill.DoesPhaseControlItself(
                        phaseIndex);

                if (isSelfControlled)
                {
                    int waitingPhase =
                        phaseIndex;

                    while (currentSkill != null &&
                           currentSkill.PhaseIndex ==
                           waitingPhase)
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


                // 현재 Phase가 마지막인지 확인
                if (currentSkill.PhaseIndex >=
                    currentSkill.MaxPhaseCount - 1)
                {
                    break;
                }


                // 다음 Phase로 이동
                currentSkill.End_DoAction();

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


    public bool CanUseSkill(string skillName)
    {
        if (skillSlotTable.TryGetValue(
                skillName,
                out var skill))
        {
            return skill != null &&
                   skill.IsOnCooldown == false &&
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
        if (slot < SkillSlot.SLOT1 ||
            slot > SkillSlot.SLOT4)
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
        if (!skillSlotTable.TryGetValue(
                slotName,
                out var skill) ||
            skill == null)
        {
            return;
        }


        if (phaseIndex > -1)
        {
            skill.PhaseIndex = phaseIndex;
        }


        // 동시 사용 가능 스킬
        if (skill.isConcurrentSkill)
        {
            if (skill.IsOnCooldown)
                return;

            skill.Cast();

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

        skill.Cast();


        if (!skill.HasActionData(
                skill.PhaseIndex))
        {
            SimulateAnimationEventsAsync(
                skill).Forget();
        }
    }


    private async UniTaskVoid SimulateAnimationEventsAsync(
        ActiveSkill skill)
    {
        try
        {
            while (skill != null &&
                   skill.IsCasting)
            {
                await UniTask.Yield(
                    PlayerLoopTiming.Update);
            }

            if (skill == null)
                return;


            // 1프레임 대기
            await UniTask.Yield(
                PlayerLoopTiming.Update);


            // 스스로 종료되는 스킬
            if (skill.DoesPhaseControlItself(
                    skill.PhaseIndex))
            {
                int cachedPhase =
                    skill.PhaseIndex;

                while (skill.PhaseIndex ==
                       cachedPhase)
                {
                    if (!InAction)
                        return;

                    await UniTask.Yield(
                        PlayerLoopTiming.Update);
                }
            }
            else
            {
                BeginJudgeAttack(null);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.1f));

                EndJudgeAttack(null);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.1f));

                EndDoAction();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"가짜 애니메이션 이벤트 발생 중 에러!\n{e}");

            EndDoAction();
        }
    }


    private async UniTaskVoid ExecuteConcurrentSkillAsync(
        ActiveSkill skill)
    {
        while (skill != null &&
               skill.IsCasting)
        {
            await UniTask.Yield(
                PlayerLoopTiming.Update);
        }

        if (skill == null)
            return;


        for (int i = 0;
             i < skill.MaxPhaseCount;
             i++)
        {
            skill.Begin_JudgeAttack(null);
            skill.End_JudgeAttack(null);

            if (i < skill.MaxPhaseCount - 1)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.1f));

                skill.End_DoAction();
            }
        }

        skill.End_DoAction();
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

        if (skill != null &&
            skill.IsCasting)
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