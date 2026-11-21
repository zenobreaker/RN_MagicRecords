using System.Collections.Generic;
using UnityEngine;

/// <summary>기본 충전량 1%에 더하는 등급별 보정값입니다. 기존 직렬화 필드명을 유지합니다.</summary>
[System.Serializable]
public sealed class GuideDroneChargeMultiplier
{
    public RecordRarity rarity;
    [Min(0f), Tooltip("기본 충전량 1%에 더하는 보정값(%p)입니다.")]
    public float multiplier;
}

/// <summary>슬롯이나 모델링에 의존하지 않는 가이드 드론의 고유 효과와 포인트 설정입니다.</summary>
[CreateAssetMenu(fileName = "SO_GuideDroneData", menuName = "Scriptable Objects/SO_GuideDroneData")]
public sealed class SO_GuideDroneData : ScriptableObject
{
    [Tooltip("탐사 및 이전 선택 저장에 사용하는 고유 ID입니다. 출시 후 변경하지 않습니다.")]
    public string droneId;
    public string droneName;
    public Sprite icon;

    [Header("정보 화면 지역화 키")]
    public string displayNameKey;
    public string profileKey;
    public string passiveDescriptionKey;
    public string activeDescriptionKey;

    public List<RecordStatData> passiveStats = new();
    public List<RecordStatData> activeStats = new();

    [Header("포인트 (획득 방식과 밸런스는 추후 설정)")]
    [Min(0f)] public float initialPoints = 30f;
    [Min(0.01f)] public float maximumPoints = 30f;
    [Min(0.01f)] public float pointsPerSecond = 1f;

    [Range(0f, 100f), Tooltip("액티브 스킬 사용 1회당 최대 에너지 대비 충전 비율(%)입니다.")]
    public float skillUseChargePercent = 5f;

    /// <summary>스킬 사용 1회에 충전할 에너지 양을 반환합니다.</summary>
    public float GetSkillUseCharge()
    {
        if (HasValidPointSettings() == false || IsFinite(skillUseChargePercent) == false)
        {
            return 0f;
        }
        return maximumPoints * Mathf.Clamp(skillUseChargePercent, 0f, 100f) * 0.01f;
    }

    [Header("기억 발동 충전 (기본 1% + 등급 보정 %p)")]
    public List<GuideDroneChargeMultiplier> chargeMultipliers = new()
    {
        new GuideDroneChargeMultiplier
        {
            rarity = RecordRarity.NORMAL,
            multiplier = 0.5f
        },
        new GuideDroneChargeMultiplier
        {
            rarity = RecordRarity.RARE,
            multiplier = 0.7f
        },
        new GuideDroneChargeMultiplier
        {
            rarity = RecordRarity.UNIQUE,
            multiplier = 1.2f
        },
        new GuideDroneChargeMultiplier
        {
            rarity = RecordRarity.LEGENDARY,
            multiplier = 1.5f
        },
        new GuideDroneChargeMultiplier
        {
            rarity = RecordRarity.MYTH,
            multiplier = 2f
        }
    };

    /// <summary>설정된 등급의 충전량을 반환합니다. 잘못되거나 누락된 값은 충전하지 않습니다.</summary>
    public float GetMemoryCharge(RecordRarity rarity)
    {
        if (chargeMultipliers != null && HasValidPointSettings())
        {
            foreach (var entry in chargeMultipliers)
            {
                if (entry != null && entry.rarity == rarity && IsFinite(entry.multiplier) && entry.multiplier >= 0f)
                {
                    return maximumPoints * 0.01f * (1f + entry.multiplier);
                }
            }
        }
        return 0f;
    }

    /// <summary>잘못된 포인트 설정으로 무한 지속하거나 활성화할 수 없는 상태를 방지합니다.</summary>
    public bool HasValidPointSettings()
    {
        return IsFinite(initialPoints) && initialPoints >= 0f &&
            IsFinite(maximumPoints) && maximumPoints > 0f &&
            IsFinite(pointsPerSecond) && pointsPerSecond > 0f;
    }

    private static bool IsFinite(float value)
    {
        return float.IsNaN(value) == false && float.IsInfinity(value) == false;
    }
}
