using UnityEngine;

// 원본 장전 스킬을 유지하면서 런타임 소유자 교체/해제에 필요한 정리를 추가합니다.
public sealed class RuntimeMagicBulletLoad : Passive_MagicBulletLoad
{
    private SkillComponent registeredOwner;
    public RuntimeMagicBulletLoad(SO_SkillData data) : base(data) { }

    public override void OnAcquire(GameObject skillOwner)
    {
        if (registeredOwner != null) OnLose();
        base.OnAcquire(skillOwner);
        registeredOwner = skillOwner != null ? skillOwner.GetComponent<SkillComponent>() : null;
    }

    public override void OnLose()
    {
        base.OnLose();
        if (registeredOwner != null && ReferenceEquals(registeredOwner.GetCapability<IMagicBulletProvider>(), this))
            registeredOwner.UnregisterCapability<IMagicBulletProvider>();
        registeredOwner = null;
    }
}
