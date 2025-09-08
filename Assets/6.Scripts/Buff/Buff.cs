using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public struct BuffUI
{
    public string BuffID;

    public Image BuffImg;
    public TextMeshProUGUI BuffNameText;
    public TextMeshProUGUI BuffStackText;

    // View에서 BaseBuff에서 정보를 받아옴.
    //public string buffName; 
    //public float durtaion;
    //public float elapsed;
    //public int stackCount;
}


public abstract class BaseBuff : IBuff
{
    public string BuffID;

    protected float elapsed;
    public float Elapsed => elapsed;
    protected float duration;
    public float Duration => duration;

    public virtual BuffStackPolicy StackPolicy => BuffStackPolicy.RefreshOnly;


    public BaseBuff(string buffID, float elapsed, float duration)
    {
        BuffID = buffID;
        this.elapsed = elapsed;
        this.duration = duration;
    }

    public int StackCount { get; protected set; } = 1;
    public virtual int MaxStack => 1;
    public virtual bool CanStack => MaxStack > 1;

    public virtual bool NeedTick => true;
    public float TickInterval { get; set; } = 0.1f;

    protected float tickTimer;
    public virtual void AddStack()
    {
        if (CanStack && StackCount < MaxStack)
        {
            StackCount++;
            ResetDuration();
        }
        else
            ResetDuration();
    }

    public void ResetDuration() => elapsed = 0;

    public bool IsExpired => elapsed >= duration;

    public virtual void OnUpdate(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer -= TickInterval;
            Tick();
        }

        elapsed += deltaTime;
    }

    public virtual void Tick() { }
    public abstract void OnApply(Character target);

    public abstract void OnRemove();

}

public class StatBuff : BaseBuff
{
    protected StatModifier modifier;

    protected StatusComponent status;

    public StatBuff(string buffID, float duration, StatusType type, float amount, 
        ModifierValueType valueType = ModifierValueType.Percent)
        : base(buffID, 0f, duration)
    {
        modifier = new StatModifier(type, amount, valueType);
    }

    public StatBuff(string buffID, float duration, StatModifier modifier = null)
        : base(buffID, 0f, duration)
    {
        this.modifier = modifier;
    }

    public override void OnApply(Character target)
    {
        if (NeedTick)
            tickTimer = 0.0f;

        if (target.TryGetComponent<StatusComponent>(out StatusComponent status))
        {
            this.status = status;
            this.status.ApplyBuff(modifier);
        }
    }

    public override void OnRemove()
    {
        status?.RemoveBuff(modifier);
#if UNITY_EDITOR
        Debug.Log("Buff Off");
#endif
    }
}