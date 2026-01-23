using UnityEngine;

public class Record_FinalEffort : RecordPassive
{
    private StatusComponent status;
    private HealthPointComponent health;
    private StatModifier modifier; 
    public Record_FinalEffort(SO_RecordData data) : base(data)
    {
    }

    public override void OnAcquire(GameObject owner)
    {
        base.OnAcquire(owner);

        status = owner.GetComponent<StatusComponent>();
        if (owner.TryGetComponent<HealthPointComponent>(out health))
        {
            health.OnChangedHP_TwoParam += OnChangedHP;
        }
    }

    private void OnChangedHP(float value, float maxHp) 
    {
        if(value  <= maxHp * 0.3f)
        {
            status.ApplyBuff(modifier = new StatModifier(StatusType.ATTACKSPEED, 0.3f, ModifierValueType.PERCENT));
        }
    }

    public override void OnLose()
    {
        base.OnLose();
        status?.RemoveBuff(modifier);   
    }
}
