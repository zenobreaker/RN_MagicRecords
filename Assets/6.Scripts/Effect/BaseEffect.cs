using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

// 트리거 조건 인터페이스
public interface IEffectTrigger
{
    bool CheckTrigger(GameObject owner, object context = null);
}

// 효과 액션 인터페이스
public interface IEffectAction
{
    void Execute(GameObject target,GameObject caster, int stackCount);
}


public abstract class BaseEffect
{
    public string ID { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; }

    public float Duration { get; private set; }
    public float RemainingTime { get; private set; }

    public int StackCount { get; private set; } = 1;
    public virtual int MaxStack => 1;
    public virtual BuffStackPolicy StackPolicy => BuffStackPolicy.REFRESH_ONLY;

    public List<IEffectTrigger> Triggers { get; private set; } = new();
    public List<IEffectAction> Actions { get; private set; } = new();

    protected GameObject owner;
    protected GameObject appliedBy;
    public GameObject FxObject { get; set; }
    public Sprite FxIcon { get; set; }

    public event Action<BaseEffect> OnRemovedUI;

    public BaseEffect(string id, string desc, float duration)
    {
        ID = id;
        Description = desc;
        Duration = duration;
        RemainingTime = duration;
    }

    public virtual void OnApply(GameObject owner, GameObject appliedBy)
    {
        this.owner = owner;
        this.appliedBy = appliedBy;
        RemainingTime = Duration;

        // 즉시형 or 지속형
        foreach (IEffectAction action in Actions)
            action.Execute(owner, appliedBy, StackCount);
    }

    public virtual void OnRemove()
    {
        OnRemovedUI?.Invoke(this);
    }

    public void ResetDuration()
    {
        RemainingTime = Duration;
    }

    public virtual void AddStack()
    {
        if (StackCount < MaxStack)
            StackCount++;

        if (StackPolicy == BuffStackPolicy.REFRESH_ONLY || 
            StackPolicy == BuffStackPolicy.STACKABLE)
            ResetDuration();
    }

    public bool IsExpired => Duration > 0 && RemainingTime <= 0;

    public virtual void Update(float deltaTime)
    {
        if (Duration > 0)
            RemainingTime -= deltaTime;

        // 트리거에 의한 실행
        foreach (IEffectTrigger trigger in Triggers)
        {
            if (trigger.CheckTrigger(owner))
            {
                foreach (IEffectAction action in Actions)
                    action.Execute(owner, appliedBy, StackCount);
            }
        }
    }
}


