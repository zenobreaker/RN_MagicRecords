using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct BuffUI
{
    public int BuffID;

    public Image BuffImg;
    public TextMeshProUGUI BuffNameText;
    public TextMeshProUGUI BuffStackText;


    public string buffName; 
    public float durtaion;
    public float elapsed;
    public int stackCount;
}


public abstract class BaseBuff :IBuff
{
    public int BuffID;

    protected float elapsed;
    protected float durtaion;

    public BaseBuff(int buffID, float elapsed, float durtaion)
    {
        BuffID = buffID;
        this.elapsed = elapsed;
        this.durtaion = durtaion;
    }

    public bool IsExpired => elapsed >= durtaion;

    public virtual void OnApply(Character target)
    {
        elapsed = 0.0f; 
    }

    public virtual void OnUpdate(float deltaTime)
    {
        elapsed += deltaTime;
    }

    public abstract void OnRemove();
}

public class StatBuff : BaseBuff
{
    protected StatusType type;
    protected float amount;

    protected StatusComponent status; 

    public StatBuff(int buffID, float elapsed, float durtaion) 
        : base(buffID, elapsed, durtaion)
    {
    }

    public override void OnApply(Character target)
    {
        base.OnApply(target);

        if(target.TryGetComponent<StatusComponent>(out StatusComponent status))
        {
            this.status = status; 
            status.ApplyBuff(type, amount);
        }
    }

    public override void OnRemove()
    {
        if (status == null) return;

        status.ApplyBuff(type, amount * -1.0f);
    }
}