using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum BuffStackPolicy
{
    RefreshOnly = 0,
    Stackable,
    IgnoreIfExsist,
}

public enum BuffValueType
{
    Percent = 0,
    Fixed,
}

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
    protected StatusType type;
    protected float amount;
    protected BuffValueType valueType; 

    protected StatusComponent status;

    public StatBuff(string buffID, float duration, StatusType type, float amount,
        BuffValueType valueType = BuffValueType.Percent)
        : base(buffID, 0f, duration)
    {
        this.type = type;
        this.amount = amount;
        this.valueType = valueType;
    }

    public override void OnApply(Character target)
    {
        if (NeedTick)
            tickTimer = 0.0f;

        if (target.TryGetComponent<StatusComponent>(out StatusComponent status))
        {
            this.status = status;
            float finalValue = amount; 

            if(valueType == BuffValueType.Percent)
            {
                finalValue = status.GetStatusValue(type) * amount; 
            }

            finalValue *= StackCount;
            this.amount = finalValue;
            this.status.ApplyBuff(type, finalValue);
        }
    }

    public override void OnRemove()
    {
        if (status == null) return;
        status.ApplyBuff(type, amount * -1.0f);
#if UNITY_EDITOR
        Debug.Log("Buff Off");
#endif
    }
}