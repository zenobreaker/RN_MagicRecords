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
    void Execute(GameObject target, GameObject caster, int stackCount);
}


public abstract class BaseEffect
{
    public string ID
    {
        get;
        private set;
    }
    public string Name
    {
        get;
        protected set;
    } = string.Empty;
    public string Description
    {
        get;
        private set;
    }

    public float Duration
    {
        get;
        private set;
    }
    public float RemainingTime
    {
        get;
        private set;
    }

    public int StackCount
    {
        get;
        private set;
    } = 1;
    /// <summary>버프 아이콘에 표시할 수치이며 실제 효과 스택과 별도로 재정의할 수 있습니다.</summary>
    public virtual int DisplayStackCount => StackCount;

    public virtual int MaxStack
    {
        get
        {
            return 1;
        }
    }
    public virtual BuffStackPolicy StackPolicy
    {
        get
        {
            return BuffStackPolicy.REFRESH_ONLY;
        }
    }
    public virtual bool ShowSingleStack
    {
        get
        {
            return false;
        }
    }

    public List<IEffectTrigger> Triggers
    {
        get;
        private set;
    } = new();
    public List<IEffectAction> Actions
    {
        get;
        private set;
    } = new();

    public EffectType Type
    {
        get;
        protected set;
    } = EffectType.NONE;

    protected GameObject owner;
    protected GameObject appliedBy;
    public GameObject FxObject
    {
        get;
        set;
    }
    public Sprite FxIcon
    {
        get;
        set;
    }

    public event Action<BaseEffect> OnRemovedUI;

    public bool ShowInHUD
    {
        get;
        private set;
    } = true;

    /// <summary>수명이나 스택이 없어도 현재 종류 또는 상태를 확인해야 하는 효과입니다.</summary>
    public virtual bool HasPersistentHUDState => false;

    /// <summary>레코드 효과는 지속시간, 스택 또는 명시적인 상태 표시가 필요할 때 버프칸에 표시합니다.</summary>
    public void ConfigureRecordHUDVisibility(bool hasTrackedLifetime = false)
    {
        ShowInHUD = HasPersistentHUDState || hasTrackedLifetime || Duration > 0f || MaxStack > 1 || ShowSingleStack;
    }

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
        {
            action.Execute(owner, appliedBy, StackCount);
        }
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
        {
            StackCount++;
        }

        if (StackPolicy == BuffStackPolicy.REFRESH_ONLY ||
            StackPolicy == BuffStackPolicy.STACKABLE)
        {
            ResetDuration();
        }
    }

    public virtual bool IsDispellable => true;
    public virtual bool ShowRemainingTime => false;
    public GameObject AppliedBy => appliedBy;
    public GameObject Target => owner;

    /// <summary>전이 가능한 디버프는 독립된 효과 인스턴스를 반환합니다.</summary>
    public virtual BaseEffect CreateTransferCopy(float duration)
    {
        return null;
    }

    public bool IsExpired
    {
        get
        {
            return Duration > 0 && RemainingTime <= 0;
        }
    }

    public bool TryConsumeStacks(int amount)
    {
        if (amount <= 0 || StackCount < amount)
        {
            return false;
        }

        StackCount -= amount;
        return true;
    }

    public virtual void Update(float deltaTime)
    {
        if (Duration > 0)
        {
            RemainingTime -= deltaTime;
        }

        // 트리거에 의한 실행
        foreach (IEffectTrigger trigger in Triggers)
        {
            if (trigger.CheckTrigger(owner))
            {
                foreach (IEffectAction action in Actions)
                {
                    action.Execute(owner, appliedBy, StackCount);
                }
            }
        }
    }
}


