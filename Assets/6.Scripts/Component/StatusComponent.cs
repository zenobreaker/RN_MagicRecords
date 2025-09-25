using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StatusValue
{
    public float baseValue;
    private readonly List<StatModifier> modifiers = new();
    private float finalValue; 
    private bool isDirty = true; 

    public float FinalValue
    {
        get
        {
            if (isDirty)
                Recalculate();
            
            return finalValue; 
        }
    }

    public void Set(float baseVal = 0.0f)
    {
        baseValue = baseVal;
        isDirty = true; 
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        isDirty = true; 
    }

    public void RemoveModifier(StatModifier modifier)
    {
        modifiers.Remove(modifier);
        isDirty = true; 
    }

    private void Recalculate()
    {
        float result = baseValue; 

        foreach (var mod in modifiers)
        {
            if (mod.valueType == ModifierValueType.FIXED)
                result += mod.value;
        }

        foreach (var mod in modifiers)
        {
            if (mod.valueType == ModifierValueType.PERCENT)
                result *= (1 + mod.value);
        }

        finalValue = result;
        isDirty = false; 
    }
}

[System.Serializable]
public class StatusEntry
{
    public StatusType type;
    public StatusValue value;
}

[System.Serializable]
public class Status
{
    public List<StatusEntry> statusEntries = new List<StatusEntry>(); 

    private Dictionary<StatusType, StatusValue> statusValueTable;

    public void Init()
    {
        statusValueTable = new Dictionary<StatusType, StatusValue>();
        foreach (var entry in statusEntries)
            statusValueTable[entry.type] = entry.value;
    }

    public StatusValue Get(StatusType type)
    {
        if (statusValueTable == null)
            Init();

        return statusValueTable.TryGetValue(type, out var val) ? val : null;
    }

    public void Set(StatusType type, float value)
    {
        if (statusValueTable == null) Init();
        statusValueTable[type].Set(value); 
    }

    public void ApplyBuff(StatModifier modifier)
    {
        if (modifier == null)
        {
            Debug.LogWarning("Target Type Is Null");
            return;
        }

        Get(modifier.type)?.AddModifier(modifier);
    }
    public void RemoveBuff(StatModifier modifier)
    {
        Get(modifier.type)?.RemoveModifier(modifier); 
    }
}

public class StatusComponent : MonoBehaviour
{
    [SerializeField]
    private Status status;

    public Action<float> OnSetHealth;

    private void Start()
    {
        if(status == null)
            status = new Status();

        status.Init();
    }

    public void ApplyBuff(StatModifier modifier)
    {
        if (modifier == null)
            return;
        
        status?.ApplyBuff(modifier);
    }

    public void RemoveBuff(StatModifier modifier)
    {
        status?.RemoveBuff(modifier);
    }

    public void SetStatusValue(StatusType type, float value)
    {
        if (status == null || status.Get(type) == null) return;
        status.Set(type, value);
    }

    public float GetStatusValue(StatusType type)
    {
        if (status == null || status.Get(type) == null) return 0;

        return status.Get(type).FinalValue;
    }

    public void SetStatusData(CharStatusData data)
    {
        if (data == null) return; 

        SetStatusValue(StatusType.ATTACK, data.GetStatusValue(StatusType.ATTACK));
        SetStatusValue(StatusType.ATTACKSPEED, data.GetStatusValue(StatusType.ATTACKSPEED));
        SetStatusValue(StatusType.DEFENSE, data.GetStatusValue(StatusType.DEFENSE));
        SetStatusValue(StatusType.MOVESPEED, data.GetStatusValue(StatusType.MOVESPEED));
        SetStatusValue(StatusType.CRIT_RATIO, data.GetStatusValue(StatusType.CRIT_RATIO));
        SetStatusValue(StatusType.CRIT_DMG, data.GetStatusValue(StatusType.CRIT_DMG));
        SetStatusValue(StatusType.HEALTH, data.GetStatusValue(StatusType.HEALTH));
        OnSetHealth?.Invoke(data.GetStatusValue(StatusType.HEALTH));
    }
}
