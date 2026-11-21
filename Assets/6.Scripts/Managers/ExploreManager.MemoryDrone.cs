using System.Collections.Generic;
using UnityEngine;

public sealed partial class ExploreManager
{
    [Header("탐사 메모리 드론 (목록 순서대로 표시)")]
    [SerializeField] private List<SO_GuideDroneData> memoryDrones = new();

    /// <summary>유효한 ID와 효과 설정을 가진 드론을 등록 순서대로 반환합니다.</summary>
    public List<SO_GuideDroneData> GetMemoryDroneOptions()
    {
        var result = new List<SO_GuideDroneData>();
        var ids = new HashSet<string>();
        foreach (var drone in memoryDrones)
        {
            if (drone == null || string.IsNullOrWhiteSpace(drone.droneId) ||
                drone.HasValidPointSettings() == false || ids.Add(drone.droneId) == false)
            {
                continue;
            }

            result.Add(drone);
        }

        return result;
    }

    /// <summary>저장된 ID에 해당하는 드론을 찾습니다. 이전 저장의 빈 ID는 미장착으로 유지합니다.</summary>
    public SO_GuideDroneData FindMemoryDrone(string droneId)
    {
        if (string.IsNullOrEmpty(droneId))
        {
            return null;
        }

        foreach (var drone in GetMemoryDroneOptions())
        {
            if (drone.droneId == droneId)
            {
                return drone;
            }
        }

        return null;
    }

    /// <summary>이번 탐사에 저장된 드론을 전투 캐릭터에 적용합니다.</summary>
    public void ApplyMemoryDrone(Player player)
    {
        if (player == null || IsTutorialStage)
        {
            return;
        }

        var definition = FindMemoryDrone(CurrentSetupData.SelectedMemoryDroneId);
        player.TryGetComponent<GuideDrone>(out var drone);
        if (drone == null && definition != null)
        {
            drone = player.gameObject.AddComponent<GuideDrone>();
        }

        drone.SafeInvoke(component => component.Initialize(definition));
    }
}
