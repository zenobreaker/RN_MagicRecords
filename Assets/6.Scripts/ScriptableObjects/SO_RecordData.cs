using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

[System.Serializable]
public class RecordData
{
    public int id;
    public string recordName;
    public string description;
    public Sprite icon;

    public TargetFilterType targetFilter; 
    public RecordType type;
    public RecordRarity rarity;
    public StatusType status; 
    public ModifierValueType valueType; 
    public float effectValue;

    public string triggerEvent;
    public string className; 

    public bool IsTarget(int job)
    {
        return targetFilter == (TargetFilterType)job;
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
    public StatusType status;
    public ModifierValueType valueType;
    public float effectValue;

    public string triggerEvent;
    public string className;

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
        recordData.status = status;
        recordData.valueType = valueType;
        recordData.effectValue = effectValue;
        recordData.triggerEvent = triggerEvent;
        recordData.className = className;

        return recordData;
    }
}

