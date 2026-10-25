using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class RecordStatData
{
    public StatusType Status;

    public ModifierValueType ValueType;

    public float Value;
}

[System.Serializable]
public class RecordSkillData
{
    public int SkillID;

    public SkillModifierType Modifier;

    public ModifierOperation Operation;

    public float Value;
}

[System.Serializable]
public class RecordTriggerData
{
    public int TargetPassiveSkillID;
}

[System.Serializable]
public class RecordData
{
    public int id;
    public string uniqueID;
    public string recordName;
    public string description;
    public Sprite icon;
    public List<string> keywords = new();

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
        temp.keywords = keywords != null ? new List<string>(keywords) : new List<string>();
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
    [Tooltip("False for catalogue assets whose gameplay effect is not implemented yet.")]
    public bool isImplemented = true;
    [TextArea] public string importNote;
    public string recordName;
    public string description;
    public Sprite icon;
    [Tooltip("Keyword text or localization keys shown on the record card.")]
    public List<string> keywords = new();

    public TargetFilterType targetFilter;
    public RecordType type;
    public RecordRarity rarity;


    public List<RecordStatData> Stats = new();

    public List<RecordSkillData> Skills = new();

    // 💡 [변경점] 더 이상 Triggers(문자열)로 클래스를 찾지 않습니다.
    [SerializeReference] public List<CommonRecordModule> Modules = new();

    // 대신 이 레코드가 발동시킬 SO_PassiveSkillData의 ID를 직접 가리키거나,
    // ID가 동일하다면 바로 SkillTreeManager에서 가져옵니다.
    public int linkedPassiveSkillID; // (JSON에서 받아올 매그넘샷-할로포인트의 ID: 1011 등)

    // 레코드를 획득했을 때 조립된 패시브 스킬을 반환하는 함수
    [Tooltip("Records sharing this group cannot be owned together. Leave empty for no restriction.")]
    public string mutualExclusionGroup;

    public PassiveSkill CreatePassiveSkill()
    {
        if (Modules != null && Modules.Count > 0) return new RecordPassive(this);
        int currentClassID = 0;
        if (targetFilter == TargetFilterType.ALL)
            currentClassID = 0;
        else
            currentClassID = (int)targetFilter;

        // 1. 스킬트리 매니저에서 이 레코드에 연결된 패시브 스킬 템플릿을 찾습니다.
        SkillRuntimeData runtimeData = SkillTreeManager.Instance.GetSkillRuntimeData(currentClassID, linkedPassiveSkillID);

        // 2. 데이터가 유효하고, 그 템플릿이 SO_PassiveSkillData라면?
        if (runtimeData != null && runtimeData.template is SO_PassiveSkillData passiveTemplate)
        {
            // 3. 팩토리 필요 없음! SO_PassiveSkillData가 스스로 GenericPassiveSkill을 조립해서 반환합니다.
            return passiveTemplate.CreateSkill() as PassiveSkill;
        }

        return new RecordPassive(this); // 연결된 스킬 모듈이 없는 단순 스탯 증가용 레코드인 경우
    }

    public RecordData GetRecordData()
    {
        RecordData recordData = new RecordData();
        recordData.id = id;
        recordData.recordName = recordName;
        recordData.description = description;
        recordData.icon = icon;
        recordData.keywords = keywords != null ? new List<string>(keywords) : new List<string>();
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

        recordData.Skills = Skills.Select(x => new RecordSkillData
        {
            SkillID = x.SkillID,
            Modifier = x.Modifier,
            Operation = x.Operation,
            Value = x.Value
        }).ToList();

        return recordData.GetData();
    }
}
