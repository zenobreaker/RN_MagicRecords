using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public sealed class SkillComponent
    : ActionComponent
{
    private string currentSlotName = "";

    // 장착 스킬 정보 
    private Dictionary<string, ActiveSkill> skillSlotTable;

    // 어떤 인터페이스든 구현체를 저장
    private Dictionary<Type, object> capabilityTable = new();

    // 스킬 사용 관련 이벤트 핸들러
    public SO_SkillEventHandler skillEventHandler;

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
            {"DEFAULT", null},

            {"SLOT1", null },
            {"SLOT2", null },
            {"SLOT3", null },
            {"SLOT4", null },
        };
    }
    private void FixedUpdate()
    {
        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            if (pair.Value == null) continue;

            pair.Value.FixedUpdate(Time.fixedDeltaTime);
        }
    }

    private void Update()
    {
        if (skillSlotTable == null)
            return;

        // 쿨다운 업데이트 
        foreach (KeyValuePair<string, ActiveSkill> pair in skillSlotTable)
        {
            // 1. 스킬이 비어있으면 쿨타임도 돌 필요 없음
            if (pair.Value == null) continue;

            // 2. 💡 [핵심] Dictionary의 Key("SLOT1" 등)를 SkillSlot Enum으로 안전하게 변환
            if (!Enum.TryParse(pair.Key, out SkillSlot currentSlot))
            {
                continue; // 파싱 실패 시 스킵 (확장성 대비)
            }

            // 3. 스킬 로직 업데이트
            pair.Value.Update(Time.deltaTime);
            if (skillEventHandler != null)
                skillEventHandler.OnInCoolDown(currentSlot, pair.Value.IsOnCooldown);
            OnActiveSkillCooldownChanged?.Invoke(currentSlot, pair.Value.IsOnCooldown);

            if (pair.Value.IsOnCooldown == false) continue;

            // 4. 쿨타임 UI 이벤트 발송
            pair.Value.Update_Cooldown(Time.deltaTime);
            if (skillEventHandler != null)
                skillEventHandler.OnCooldown(currentSlot, pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
            OnActiveSkillCooldownUpdated?.Invoke(currentSlot, pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
        }
    }


    // 기능 등록 (패시브가 호출)
    public void RegisterCapability<T>(T capability) where T : class
    {
        var type = typeof(T);
        if (capabilityTable.ContainsKey(type))
            capabilityTable[type] = capability;  // 이미 있으면 덮어쓰기 
        else
            capabilityTable.Add(type, capability);
    }

    // 기능 조회 (액티브가 호출)
    public T GetCapability<T>() where T : class
    {
        var type = typeof(T);
        if (capabilityTable.TryGetValue(type, out object value))
            return (T)value;
        return null;
    }

    // 기능 해제 (패시브가 사라지거나 해제 시) 
    public void UnregisterCapability<T>()
    {
        var type = typeof(T);
        if (capabilityTable.ContainsKey(type))
        {
            capabilityTable.Remove(type);
        }
    }


    public void ReleaseSkill(string slot)
    {
        if (currentSlotName == slot)
        {
            if (skillSlotTable.TryGetValue(slot, out ActiveSkill skill))
            {
                // 현재 실행 중인 스킬이 뗐다고 보고된 스킬과 일치한다면
                skill?.OnReleaseKey();
            }
        }
    }
    protected override async UniTaskVoid ManualActionRoutine(CancellationToken token)
    {
        try
        {
            BeginDoAction();

            if (string.IsNullOrEmpty(currentSlotName) ||
                !skillSlotTable.TryGetValue(currentSlotName, out ActiveSkill currentSkill) ||
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
                // ★ 항상 ActiveSkill의 실제 PhaseIndex를 가져온다.
                int phaseIndex = currentSkill.PhaseIndex;

                // 범위를 벗어나면 종료
                if (phaseIndex < 0 || phaseIndex >= currentSkill.MaxPhaseCount)
                    break;

                bool hasAnimation =
                    currentSkill.HasActionData(phaseIndex);

                // --------------------------------------------------
                // Animation이 있는 경우 선딜레이
                // --------------------------------------------------
                if (hasAnimation)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(0.2f),
                        cancellationToken: token);
                }

                // --------------------------------------------------
                // 현재 Phase 공격 이벤트
                // --------------------------------------------------
                BeginJudgeAttack(null);
                EndJudgeAttack(null);

                // --------------------------------------------------
                // 현재 Phase가 스스로 종료되는지 확인
                // --------------------------------------------------
                bool isSelfControlled =
                    currentSkill.DoesPhaseControlItself(phaseIndex);

                if (isSelfControlled)
                {
                    // ★ 이 Phase가 끝나면서 PhaseIndex가 변경되기를 기다린다.
                    int waitingPhase = phaseIndex;

                    while (currentSkill != null &&
                           currentSkill.PhaseIndex == waitingPhase)
                    {
                        await UniTask.Yield(
                            PlayerLoopTiming.Update,
                            cancellationToken: token);
                    }

                    // PhaseIndex가 변경됐으므로
                    // 다음 while에서 새로운 PhaseIndex를 읽는다.
                    continue;
                }

                // --------------------------------------------------
                // 일반 Phase
                // --------------------------------------------------
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

                // 다음 Phase로 넘어감
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
        if (skillSlotTable.TryGetValue(skillName, out var skill))
            return skill != null && skill.IsOnCooldown == false && InAction == false;

        return false;
    }

    // 스킬 장착 
    public void SetActiveSkill(SkillSlot slot, ActiveSkill skill)
    {
        SetActiveSkill(slot.ToString(), skill);
        OnActiveSkillChanged?.Invoke(slot, skill);
        if (skillEventHandler != null)
            skillEventHandler.OnSetting_ActiveSkill(slot, skill);
    }

    public bool TryGetRegisteredActiveSkill(SkillSlot slot, out ActiveSkill skill)
    {
        skill = null;

        // 기본 공격은 내부 전용 슬롯이므로 스킬 UI 표시 대상으로 사용하지 않습니다.
        if (slot < SkillSlot.SLOT1 || slot > SkillSlot.SLOT4)
            return false;

        return skillSlotTable.TryGetValue(slot.ToString(), out skill) && skill != null;
    }

    // AI에서 접근할 때는 첫 번째 인자는 스킬 이름으로 오므로 주의해야 함 
    public void SetActiveSkill(string slotName, ActiveSkill skill)
    {
        if (skillSlotTable.ContainsKey(slotName))
            skillSlotTable[slotName] = skill;
        else
            skillSlotTable.Add(slotName, skill);

        skillSlotTable[slotName]?.SetOwner(rootObject);
        skillSlotTable[slotName]?.InitializedData();
    }

    // 슬롯의 있는 스킬 사용 
    public void UseSkill(SkillSlot slot, int phaseIndex = -1)
    {
        UseSkill(slot.ToString(), phaseIndex);
    }

    public void UseSkill(string slotName, int phaseIndex = -1)
    {
        if (!skillSlotTable.TryGetValue(slotName, out var skill) || skill == null) return;

        if (phaseIndex > -1)
            skill.PhaseIndex = phaseIndex;

        // 동시 사용 가능 스킬은 행동거지를 같이해선 안되므로 호출하고 return
        if (skill.isConcurrentSkill)
        {
            if (skill.IsOnCooldown) return;

            skill.Cast();

            ExecuteConcurrentSkillAsync(skill).Forget();
            return;
        }


        if (InAction || CanUseSkill(slotName) == false)
        {
            OnSkillUse?.Invoke(false);
            return;
        }

        currentSlotName = slotName;
        OnSkillUse?.Invoke(true);

        base.DoAction();

        skill.Cast();

        if (!skill.HasActionData(skill.PhaseIndex))
        {
            SimulateAnimationEventsAsync(skill).Forget();
        }
    }

    private async UniTaskVoid SimulateAnimationEventsAsync(ActiveSkill skill)
    {
        try
        {
            while (skill != null && skill.IsCasting)
                await UniTask.Yield(PlayerLoopTiming.Update);

            if (skill == null) return;

            // 1프레임 대기 (로직 꼬임 방지)
            await UniTask.Yield(PlayerLoopTiming.Update);

            // 장판 모듈처럼 스스로 끝나는 스킬이라면 조용히 대기
            if (skill.DoesPhaseControlItself(skill.PhaseIndex))
            {
                int cachedPhase = skill.PhaseIndex;
                while (skill.PhaseIndex == cachedPhase)
                {
                    // 피격 등으로 강제로 InAction이 풀렸다면 루틴 안전 종료
                    if (!InAction) return;
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            else
            {
                // 💡 애니메이션이 없으므로, 아주 짧은 시간(0.1초) 간격으로
                // 공격 판정과 종료 이벤트를 유니태스크가 대신 타다닥! 쏴줍니다.

                BeginJudgeAttack(null); // 공격 판정 시작! (오브젝트 스폰 등)
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

                EndJudgeAttack(null);   // 공격 판정 끝!
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

                // 💡 드디어 플레이어를 굳음 상태에서 해방시켜주는 궁극의 함수!
                EndDoAction();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"가짜 애니메이션 이벤트 발생 중 에러!\n{e}");
            EndDoAction(); // 에러가 나도 무조건 풀어줍니다!
        }
    }

    private async UniTaskVoid ExecuteConcurrentSkillAsync(ActiveSkill skill)
    {
        while (skill != null && skill.IsCasting)
            await UniTask.Yield(PlayerLoopTiming.Update);

        if (skill == null) return;

        for (int i = 0; i < skill.MaxPhaseCount; i++)
        {
            skill.Begin_JudgeAttack(null);
            skill.End_JudgeAttack(null);

            if (i < skill.MaxPhaseCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                skill.End_DoAction();
            }
        }
        skill.End_DoAction();
    }

    public override void StartAction()
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.StartAction();

        skillSlotTable[currentSlotName]?.Start_DoAction();
    }

    public override void BeginDoAction()
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.BeginDoAction();

        skillSlotTable[currentSlotName]?.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        if (skillEventHandler != null)
            skillEventHandler.OnBegin_UseSkill();
    }

    public override void EndDoAction()
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;

        ActiveSkill skill = skillSlotTable[currentSlotName];
        skill?.End_DoAction();

        if (skill != null && skill.IsCasting)
        {
            return;
        }

        base.EndDoAction();
        currentSlotName = string.Empty;

        OnEndDoAction?.Invoke();
        if (skillEventHandler != null)
            skillEventHandler.OnEnd_UseSkill();
    }

    public override void BeginJudgeAttack(AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.BeginJudgeAttack(e);

        skillSlotTable[currentSlotName]?.Begin_JudgeAttack(e);
    }

    public override void EndJudgeAttack(AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.EndJudgeAttack(e);
        skillSlotTable[currentSlotName]?.End_JudgeAttack(e);
    }

    public override void PlaySound()
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.PlaySound();

        skillSlotTable[currentSlotName]?.Play_Sound();
    }

    public override void PlayCameraShake()
    {
        if (string.IsNullOrEmpty(currentSlotName)) return;
        base.PlayCameraShake();

        skillSlotTable[currentSlotName]?.Play_CameraShake();
    }


    ///////////////////////////////////////////////////////////////////////////
    #region NOTIFY
    public void NotifyBulletInit(int bulletCount)
    {
        if (skillEventHandler != null)
            skillEventHandler.OnUpdateMagciBulletLoad(bulletCount);
    }

    public void NotifyMagicBulletChanged(Queue<BulletData> bullets)
    {
        if (skillEventHandler != null)
            skillEventHandler.OnChangedBullets(bullets);
    }

    #endregion
}
