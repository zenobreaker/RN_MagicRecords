using System;
using UnityEngine;

[Serializable]
public abstract class SkillModule
{
    [Tooltip("이 모듈이 실행될 시점")]
    public SkillTriggerTime triggerTime;

    // 스킬이 생성되거나 페이즈가 바뀔 때(CacheModule 시점) 호출
    public virtual void Init(GameObject owner) { }
    public abstract void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill);
}