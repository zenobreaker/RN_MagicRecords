using UnityEngine;

// 스테이지의 플레이어가 파괴될 때 이벤트/스탯을 해제하고, 탐사 레벨은 유지합니다.
public sealed class ExplorePassiveOwner : MonoBehaviour
{
    public int JobID { get; set; }
    private void OnDestroy()
    {
        var system = AppManager.Instance?.GetPassiveSystem();
        system?.OnLose(JobID, gameObject);
        system?.OnLose(Constants.GLOBAL_RECORD_JOB_ID, gameObject);
    }
}
