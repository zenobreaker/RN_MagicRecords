using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>레코드 등급과 해당 등급의 추첨 가중치 한 행입니다.</summary>
[Serializable]
public sealed class RecordRarityWeight
{
    public RecordRarity rarity;
    [Min(0f)] public float weight;
}

/// <summary>레코드 추첨에 사용하는 등급별 상대 가중치를 정의합니다.</summary>
[CreateAssetMenu(fileName = "SO_RecordRarityTable", menuName = "Scriptable Objects/Record Rarity Table")]
public sealed class SO_RecordRarityTable : ScriptableObject
{
    [Tooltip("등급당 한 행을 설정합니다. 후보가 없는 등급은 추첨에서 제외됩니다.")]
    [SerializeField] private List<RecordRarityWeight> entries = new();

    /// <summary>추첨에 설정된 최고 등급을 반환하며 후보 소진으로 하위 등급을 승격하지 않습니다.</summary>
    public bool TryGetHighestRarity(out RecordRarity rarity)
    {
        rarity = RecordRarity.NORMAL;
        bool found = false;
        if (entries == null)
        {
            return false;
        }
        foreach (var entry in entries)
        {
            if (entry != null && GetWeight(entry.rarity) > 0f && (found == false || entry.rarity > rarity))
            {
                rarity = entry.rarity;
                found = true;
            }
        }
        return found;
    }

    /// <summary>누락되거나 유효하지 않은 가중치는 0으로 처리합니다.</summary>
    public float GetWeight(RecordRarity rarity)
    {
        if (entries == null)
        {
            return 0f;
        }

        foreach (var entry in entries)
        {
            if (entry != null && entry.rarity == rarity)
            {
                if (float.IsNaN(entry.weight) || float.IsInfinity(entry.weight))
                {
                    return 0f;
                }

                return Mathf.Max(0f, entry.weight);
            }
        }

        return 0f;
    }
}
