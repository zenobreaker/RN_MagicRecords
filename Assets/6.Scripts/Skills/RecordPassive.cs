using UnityEngine;

public class RecordPassive : PassiveSkill
{
    protected RecordData recordData;
    private StatModifier statModifier;
    private StatusComponent status; 

    public RecordPassive(SO_RecordData data) : base()
    {
        if (data == null) return;
        recordData = data.GetRecordData(); 
    }


    public override void OnApplyStaticEffect(StatusComponent status)
    {
        this.status = status;
        if (status != null && status.IsSameJob(recordData?.targetFilter))
        {
            status?.ApplyBuff(
                statModifier = ModifierFactory.CreateStatModifier
                (recordData.status, 
                recordData.effectValue, 
                recordData.valueType));
        }
    }

    public override void OnLose()
    {
        status?.RemoveBuff(statModifier);
    }
}
