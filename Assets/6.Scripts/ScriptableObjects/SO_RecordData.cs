using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class RecordStatData
{
    public StatusType Status;

    public ModifierValueType ValueType;

    public float Value;
}

public class RecordSkillData
{
    public int SkillID;

    public SkillModifierType Modifier;

    public ModifierOperation Operation;

    public float Value;
}

public class RecordTriggerData
{
    public string TriggerEvent;

    public string ClassName;
}

[System.Serializable]
public class RecordData
{
    public int id;
    public string uniqueID;
    public string recordName;
    public string description;
    public Sprite icon;

    public TargetFilterType targetFilter;
    public RecordType type;
    public RecordRarity rarity;

    public bool isLocked = false;


    public List<RecordStatData> Stats = new();

    public List<RecordSkillData> Skills = new();

    public List<RecordTriggerData> Triggers = new();

    public RecordData()
    {
        uniqueID = Guid.NewGuid().ToString();
    }

    public bool IsTarget(int job)
    {
        return targetFilter == (TargetFilterType)job;
    }

    public RecordData GetData()
    {
        var temp = new RecordData();
        temp.id = id;
        temp.uniqueID = uniqueID;
        temp.recordName = LocalizationManager.Instance.GetText(recordName);
        temp.description = LocalizationManager.Instance.GetText(description);
        temp.icon = icon;
        temp.targetFilter = targetFilter;
        temp.type = type;
        temp.rarity = rarity;

        temp.Stats = new List<RecordStatData>(Stats);
        temp.Skills = new List<RecordSkillData>(Skills);
        temp.Triggers = new List<RecordTriggerData>(Triggers);

        return temp;
    }
}

[CreateAssetMenu(fileName = "SO_RecordData", menuName = "Scriptable Objects/SO_RecordData")]
public class SO_RecordData : ScriptableObject
{
    public int id;
    public string recordName;
    public string description;
    public Sprite icon;

    public TargetFilterType targetFilter;
    public RecordType type;
    public RecordRarity rarity;


    public List<RecordStatData> Stats = new();

    public List<RecordSkillData> Skills = new();

    public List<RecordTriggerData> Triggers = new();
    public RecordPassive CreateRecord()
    {
        return RecordFactory.CreateRecordPassive(this);
    }

    public RecordData GetRecordData()
    {
        RecordData recordData = new RecordData();
        recordData.id = id;
        recordData.recordName = recordName;
        recordData.description = description;
        recordData.icon = icon;
        recordData.targetFilter = targetFilter;
        recordData.type = type;
        recordData.rarity = rarity;

        recordData.Stats = Stats
            .Select(x => new RecordStatData
            {
                Status = x.Status,
                ValueType = x.ValueType,
                Value = x.Value
            }).ToList();

        recordData.Skills = Skills
            .Select(x => new RecordSkillData
            {
                SkillID = x.SkillID,
                Modifier = x.Modifier,
                Operation = x.Operation,
                Value = x.Value
            }).ToList();

        recordData.Triggers = Triggers
            .Select(x => new RecordTriggerData
            {
                TriggerEvent = x.TriggerEvent,
                ClassName = x.ClassName
            }).ToList();

        return recordData.GetData();
    }
}

