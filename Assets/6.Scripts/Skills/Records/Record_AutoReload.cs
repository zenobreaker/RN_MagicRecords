using UnityEngine;

public class Record_AutoReload : RecordPassive
{
    private float timer = 0;
    private float reloadInterval = 3f;
    private IMagicBulletProvider bulletProvider;
    public Record_AutoReload(SO_RecordData data) : base(data)
    {
    }

    public override void OnAcquire(GameObject owner)
    {
        base.OnAcquire(owner); // <- 중요! 부모의 로직을 먼저 실행
        if (owner.TryGetComponent<SkillComponent>(out var skillComp))
        {
            bulletProvider = skillComp.GetCapability<IMagicBulletProvider>();
        }
    }

    public override void OnUpdate(float dt)
    {
        if (bulletProvider == null) return;

        timer += dt;
        if (timer >= reloadInterval)
        {
            timer = 0;
            bulletProvider.Reload(1); 
        }
    }
}
