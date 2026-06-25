using System;
using UnityEngine;

[Serializable]
public abstract class SkillModule
{
    [Tooltip("이 모듈이 실행될 시점")]
    public SkillTriggerTime triggerTime;

    // 스킬이 생성되거나 페이즈가 바뀔 때(CacheModule 시점) 호출
    public virtual void Init(Character owner) { }
    public abstract void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill);
    public virtual bool HasAnimationData()
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