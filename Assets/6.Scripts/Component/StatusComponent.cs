using System;
using System.Collections.Generic;
using UnityEngine;


public enum StatusType
{
    Attack = 0, AttackSpeed, Defense, Crit_Ratio, Crit_Dmg, Speed, MAX,
}

[System.Serializable]
public class StatusValue
{
    public float baseValue;
    public float buffedValue; 
    public float FinalValue => baseValue + buffedValue;

    public StatusValue(float baseVal = 0.0f)
    {
        baseValue = baseVal;
        buffedValue = 0.0f;
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
        statusValueTable[type].baseValue = value; 
    }

    public void ApplyBuff(StatusType type, float value)
    {
        if (Get(type) != null)
            Get(type).buffedValue += value;
        else
            Debug.LogWarning("Target Type Is Null");
    }
}

public class StatusComponent : MonoBehaviour
{
    [SerializeField]
    private Status status;

    private void Start()
    {
        if(status == null)
            status = new Status();

        status.Init();
    }

    public void ApplyBuff(StatusType type, float value)
    {
        if (status == null)
            return; 

        status.ApplyBuff(type, value);
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

}
