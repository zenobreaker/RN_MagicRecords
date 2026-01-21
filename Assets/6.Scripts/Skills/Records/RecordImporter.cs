using UnityEngine;

public class RecordImporter : MonoBehaviour
{
    private static RecordImporter instance;
    private Character owner;

    private void Awake()
    {
        instance = this;
        owner = GetComponent<Character>(); 
    }

    private void Start()
    {
        AppManager.Instance.OnRecordSelectedComplete += AddRecordDirectly;
    }

    [ContextMenu("지정한 ID 레코드 즉시 획득")]
    public static void AddRecordDirectly(RecordData data)
    {
        if (!Application.isPlaying) return;
        instance.ExecuteRecord();
    }

    public void ExecuteRecord()
    {
        AppManager.Instance.OnLose(Constants.GLOBAL_RECORD_JOB_ID, owner);
        AppManager.Instance.OnAcquire(Constants.GLOBAL_RECORD_JOB_ID, owner);
        AppManager.Instance.OnApplyStaticEffct(Constants.GLOBAL_RECORD_JOB_ID, owner);
    }
}
