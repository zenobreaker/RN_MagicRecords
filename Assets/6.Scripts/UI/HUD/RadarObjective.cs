using System.Collections.Generic;
using UnityEngine;

/// <summary>활성 상태인 동안 발치 레이더에 퀘스트 목적지를 등록합니다.</summary>
public sealed class RadarObjective : MonoBehaviour
{
    private static readonly List<RadarObjective> objectives = new();
    public static IReadOnlyList<RadarObjective> Active => objectives;

    private void OnEnable()
    {
        objectives.Add(this);
    }

    private void OnDisable()
    {
        objectives.Remove(this);
    }
}
