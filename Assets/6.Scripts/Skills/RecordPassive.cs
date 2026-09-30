using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RecordPassive : PassiveSkill
{
    protected RecordData recordData;
    private readonly Dictionary<StatusComponent, List<StatModifier>> appliedModifiers = new();
    public RecordPassive(SO_RecordData data) : base()
    {
        if (data == null) return;
        recordData = data.GetRecordData(); 
    }


    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (recordData == null || status == null) return;
        RemoveModifiers(status);
        if (!status.IsSameJob(recordData.targetFilter) || recordData.Stats == null) return;

        var modifiers = new List<StatModifier>();
        appliedModifiers[status] = modifiers;
        foreach (var modifier in recordData.Stats)
        {
            if (modifier == null) continue;
            var statMod = ModifierFactory.CreateStatModifier(
                modifier.Status, modifier.Value, modifier.ValueType);
            status.ApplyBuff(statMod);
            modifiers.Add(statMod);
        }
    }

    public override void OnLose()
    {
        foreach (var target in new List<StatusComponent>(appliedModifiers.Keys))
            RemoveModifiers(target);
    }

    private void RemoveModifiers(StatusComponent target)
    {
        if (!appliedModifiers.TryGetValue(target, out var modifiers)) return;
        foreach (var modifier in modifiers)
            target.SafeInvoke(value => value.RemoveBuff(modifier));
        appliedModifiers.Remove(target);
    }
}
