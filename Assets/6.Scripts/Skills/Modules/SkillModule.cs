using System;
using UnityEngine;

[Serializable]
public abstract class SkillModule
{
    [Tooltip("이 모듈이 실행될 시점")]
    public SkillTriggerTime triggerTime;

    [Min(0f), Tooltip("OnPhaseTime일 때 Phase 진입 후 실행할 시간(초)")]
    public float triggerDelay;

    [Min(0.01f), Tooltip("OnMovementProgress 실행 간격(초)")]
    public float movementRepeatInterval = 0.1f;

    [Tooltip("Phase마다 한 번 실행합니다. OnPhaseTime은 항상 한 번만 실행됩니다.")]
    public bool executeOncePerPhase;

    [Tooltip("OnPhaseTime 지연에 Runtime 연사 간격 배율을 적용합니다.")]
    public bool useFireIntervalMultiplier;

    // 설정 위치로 만든 키이며 반복 횟수는 Skill Runtime에서 관리합니다.
    public string RuntimeKey { get; internal set; }

    public float GetTriggerDelay(ActiveSkill skill) => Mathf.Max(0f, triggerDelay) *
        (useFireIntervalMultiplier ? skill.Runtime.FireIntervalMultiplier : 1f);

    public virtual void OnPhaseEnter(Character owner, ActiveSkill skill, PhaseSkill phase) { }
    public virtual void OnPhaseExit(Character owner, ActiveSkill skill, PhaseSkill phase) { }

    // 스킬이 생성되거나 페이즈가 바뀔 때(CacheModule 시점) 호출
    public virtual void Init(Character owner) { }
    public abstract void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill);

    public virtual void OnChainNotify(
    Character owner,
    ActiveSkill skill,
    PhaseSkill phaseSkill,
    SkillChainContext context)
    {

        OnNotify(owner, skill, phaseSkill);
    }

    public virtual void Update (Character owner, ActiveSkill skill, 
        PhaseSkill phase, float deltaTime) { }
    public virtual void FixedUpdate(Character owner, ActiveSkill skill,
        PhaseSkill phaseSkill, float fixedDeltaTime)
    { }

    public virtual bool HasAnimationData()
    {
        return false;
    }

    public virtual bool ControlsPhaseLifecycle()
    {
        return false;
    }

    public virtual SkillModule Clone()
    {
        // MemberwiseClone()은 값(Value) 타입은 복사하지만, 
        // 참조(Reference) 타입은 주소만 복사하는 '얕은 복사'를 수행합니다.
        return (SkillModule)this.MemberwiseClone();
    }
}

// 💡 클래스 위에 달 수 있는 커스텀 어트리뷰트 정의
[AttributeUsage(AttributeTargets.Class)]
public class ModuleCategoryAttribute : Attribute
{
    public string Path { get; private set; }

    public ModuleCategoryAttribute(string path)
    {
        Path = path;
    }
}
