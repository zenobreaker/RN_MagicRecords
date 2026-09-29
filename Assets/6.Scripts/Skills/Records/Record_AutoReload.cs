using UnityEngine;

// 구형 생성 경로도 같은 시간 감소 효과를 사용합니다.
public class Record_AutoReload : RecordPassive
{
    private readonly Module_Passive_AutoReload reloadModifier = new Module_Passive_AutoReload();
    public Record_AutoReload(SO_RecordData data) : base(data) { }

    public override void OnAcquire(GameObject owner)
    {
        base.OnAcquire(owner);
        reloadModifier.OnAcquire(owner, 1);
    }

    public override void OnLose()
    {
        reloadModifier.OnLose();
        base.OnLose();
    }
}