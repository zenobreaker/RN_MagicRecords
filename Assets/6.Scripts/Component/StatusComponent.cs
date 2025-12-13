using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;


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
    private bool isInitialized = false; 

    public void Init()
    {
        if (isInitialized) return; 

        statusValueTable = new Dictionary<StatusType, StatusValue>();
        
        // 에디터에서 넣은 기본값만 초기 테이블에 등록 
        foreach (var entry in statusEntries)
            statusValueTable[entry.type] = entry.value;

        isInitialized = true;
    }

    public StatusValue Get(StatusType type)
    {
        if (isInitialized == false) Init();

        if(statusValueTable.TryGetValue(type, out var val) == false)
        {
            val = new StatusValue();
            statusValueTable[type] = val;
        }

        return val;
    }

    public void Set(StatusType type, float value)
    {
        Get(type).Set(value); 
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

    private HealthPointComponent healthPointComponent;

    public Action<float> OnSetHealth;

    private void Awake()
    {
        if(status == null)
            status = new Status();

        healthPointComponent = GetComponent<HealthPointComponent>();
    }

    private void Start()
    {
        status?.Init();
    }


    //-------------------------------------------------------------------------
    // EffectSystem에서 호출하는 API
    //-------------------------------------------------------------------------

    /// <summary>
    /// 효과 시스템이  스탯을 적용하거나 제거할 때 사용
    /// </summary>
    /// <param name="modifier"></param>

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

    //-------------------------------------------------------------------------
    // 직접 스탯을 변경하고 싶을 때 
    //-------------------------------------------------------------------------

    public void SetStatusValue(StatusType type, float value)
    {
        if (status == null) return;
        status.Set(type, value);
    }

    public float GetStatusValue(StatusType type)
    {
        if (status == null) return 0;

        return status.Get(type).FinalValue;
    }

    public float GetMaxHP()
    {
        return healthPointComponent?.GetMaxHP ?? 0.0f;
    }

    /// <summary>
    /// 캐릭터 기본 스탯 초기 설정
    /// </summary>
    /// <param name="data"></param>

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
