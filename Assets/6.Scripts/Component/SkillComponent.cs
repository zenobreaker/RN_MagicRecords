using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

//TODO: 확장성을 위하여 enum이 아닌 string으로 처리해보기
public enum SkillSlot
{
    SLOT1 = 0, SLOT2, SLOT3, SLOT4, MAX,
}

public sealed class SkillComponent
    : ActionComponent
{
    private string currentSkillName = "";

    // 장착 스킬 정보 
    private Dictionary<string, ActiveSkill> skillSlotTable;

    // 어떤 인터페이스든 구현체를 저장
    private Dictionary<Type, object> capabilityTable = new();

    // 스킬 사용 관련 이벤트 핸들러
    public SO_SkillEventHandler skillEventHandler;

    public event Action<bool> OnSkillUse;

    private void Awake()
    {
        rootObject = transform.root.gameObject;
        Awake_SkillSlotTable();
    }

    private void Awake_SkillSlotTable()
    {
        skillSlotTable = new Dictionary<string, ActiveSkill>
        {
            {"SLOT1", null },
            {"SLOT2", null },
            {"SLOT3", null },
            {"SLOT4", null },
        };
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
            skillEventHandler?.OnInCoolDown(currentSlot, pair.Value.IsOnCooldown);

            if (pair.Value.IsOnCooldown == false) continue;

            // 4. 쿨타임 UI 이벤트 발송
            pair.Value.Update_Cooldown(Time.deltaTime);
            skillEventHandler?.OnCooldown(currentSlot, pair.Value.CurrentCooldown, pair.Value.MaxCooldown);
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



    protected override async UniTaskVoid ManualActionRoutine(CancellationToken token)
    {
        try
        {
            BeginDoAction();

            // 1. 현재 사용 중인 스킬의 총 페이즈 횟수 가져오기
            int phaseCount = 1;
            ActiveSkill currentSkill = null;

            if (!string.IsNullOrEmpty(currentSkillName) && skillSlotTable.TryGetValue(currentSkillName, out currentSkill))
            {
                if (currentSkill != null)
                    phaseCount = currentSkill.MaxPhaseCount;
            }

            // 2. 페이즈 개수만큼 가짜 타이머 반복 실행!
            for (int i = 0; i < phaseCount; i++)
            {
                // 애니메이션 선딜레이
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: token);

                BeginJudgeAttack(null);
                EndJudgeAttack(null);

                // ====================================================================
                // 💡 [핵심 픽스] 이 페이즈가 스스로 끝내는 능력이 있는지(장판 등) 검사!
                bool isSelfControlled = currentSkill != null && currentSkill.DoesPhaseControlItself(i);

                if (isSelfControlled)
                {
                    // 장판이 터지면서 스스로 'EndPhaseAndNext()'를 부를 때까지 
                    // 가짜 타이머는 여기서 시간만 죽이며 조용히 기다립니다.
                    int cachedPhase = i;
                    while (currentSkill.PhaseIndex == cachedPhase)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                    }
                }
                else
                {
                    // 일반 단타 공격이라면 상사(가짜 타이머)가 0.3초 뒤에 강제로 넘깁니다.
                    await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: token);

                    if (i < phaseCount - 1)
                    {
                        currentSkill?.End_DoAction();
                    }
                }
                // ====================================================================
            }

            // 3. 모든 페이즈가 끝났으므로 컴포넌트 전체의 행동을 진짜로 종료!
            EndDoAction();
        }
        catch (OperationCanceledException)
        {
            // 피격 시 캔슬
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
        skillEventHandler?.OnSetting_ActiveSkill(slot, skill);
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
    public void UseSkill(SkillSlot slot)
    {
        UseSkill(slot.ToString());
    }

    public void UseSkill(string skillName)
    {
        if (InAction || CanUseSkill(skillName) == false)
        {
            OnSkillUse?.Invoke(false);
            return;
        }

        currentSkillName = skillName;
        OnSkillUse?.Invoke(true);

        base.DoAction();
        skillSlotTable[currentSkillName]?.Cast();
    }

    public override void StartAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.StartAction();

        skillSlotTable[currentSkillName]?.Start_DoAction();
    }

    public override void BeginDoAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.BeginDoAction();

        skillSlotTable[currentSkillName]?.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        skillEventHandler?.OnBegin_UseSkill();
    }

    public override void EndDoAction()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.EndDoAction();

        skillSlotTable[currentSkillName]?.End_DoAction();
        currentSkillName = string.Empty;

        OnEndDoAction?.Invoke();
        skillEventHandler?.OnEnd_UseSkill();
    }

    public override void BeginJudgeAttack(AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.BeginJudgeAttack(e);

        skillSlotTable[currentSkillName]?.Begin_JudgeAttack(e);
    }

    public override void EndJudgeAttack(AnimationEvent e)
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.EndJudgeAttack(e);
        skillSlotTable[currentSkillName]?.End_JudgeAttack(e);
    }

    public override void PlaySound()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.PlaySound();

        skillSlotTable[currentSkillName]?.Play_Sound();
    }

    public override void PlayCameraShake()
    {
        if (string.IsNullOrEmpty(currentSkillName)) return;
        base.PlayCameraShake();

        skillSlotTable[currentSkillName]?.Play_CameraShake();
    }


    ///////////////////////////////////////////////////////////////////////////
    #region NOTIFY
    public void NotifyBulletInit(int bulletCount)
    {
        skillEventHandler?.OnUpdateMagciBulletLoad(bulletCount);
    }

    public void NotifyMagicBulletChanged(Queue<BulletData> bullets)
    {
        skillEventHandler?.OnChangedBullets(bullets);   
    }

    #endregion
}
