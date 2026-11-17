using System.Collections.Generic;
using UnityEngine;

/// <summary>슬롯이나 모델링에 의존하지 않는 가이드 드론의 고유 효과와 포인트 설정입니다.</summary>
[CreateAssetMenu(fileName = "SO_GuideDroneData", menuName = "Scriptable Objects/SO_GuideDroneData")]
public sealed class SO_GuideDroneData : ScriptableObject
{
    public string droneName;
    public Sprite icon;
    public List<RecordStatData> passiveStats = new();
    public List<RecordStatData> activeStats = new();

    [Header("포인트 (획득 방식과 밸런스는 추후 설정)")]
    [Min(0f)] public float initialPoints = 30f;
    [Min(0.01f)] public float maximumPoints = 30f;
    [Min(0.01f)] public float activationPoints = 1f;
    [Min(0.01f)] public float pointsPerSecond = 1f;

    /// <summary>잘못된 포인트 설정으로 무한 지속하거나 활성화할 수 없는 상태를 방지합니다.</summary>
    public bool HasValidPointSettings()
    {
        return IsFinite(initialPoints) && initialPoints >= 0f &&
            IsFinite(maximumPoints) && maximumPoints > 0f &&
            IsFinite(activationPoints) && activationPoints > 0f && activationPoints <= maximumPoints &&
            IsFinite(pointsPerSecond) && pointsPerSecond > 0f;
    }

    private static bool IsFinite(float value)
    {
        return float.IsNaN(value) == false && float.IsInfinity(value) == false;
    }
}
