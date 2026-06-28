using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RecordPassive : PassiveSkill
{
    protected RecordData recordData;
    private StatusComponent status;

    private List<StatModifier> statModifiers = new();
    public RecordPassive(SO_RecordData data) : base()
    {
        if (data == null) return;
        recordData = data.GetRecordData(); 
    }


    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (recordData == null) return; 

        this.status = status;

        if (!status.IsSameJob(recordData.targetFilter))
            return;

        if (status != null)
        {
            foreach (var modifier in recordData.Stats)
            {
                var statMod = ModifierFactory.CreateStatModifier(
                    modifier.Status, modifier.Value, modifier.ValueType);
                status.ApplyBuff(statMod);
                statModifiers.Add(statMod);
            }
        }

        
    }

    public override void OnLose()
    {
        foreach (var modifier in statModifiers)
            status.SafeInvoke(v => v.RemoveBuff(modifier));
    }
}
