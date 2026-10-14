using Unity.AppUI.Core;
using UnityEngine;

public class BossEffectGroupUI : EffectGroupUI
{
    private Character targetBoss;

    public void SetBoss(Character boss)
    {
        targetBoss = boss;
        RefreshEffects();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        if (targetBoss == null) return;
        var effects = targetBoss.GetComponent<EffectComponent>();
        if (effects == null) return;
        // Battle-start effects may precede the first HP notification that creates this UI.
        foreach (var effect in effects.ActiveEffects)
            UpdateEffectIcon(effect);
    }

    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler == null) return;
        
        // 부모의 handler.OnEffect += OnEffect; 를 하지 않기 위해 base 호출 안 함
        // 대신 보스 전용 이벤트를 구독
        handler.OnChangedBossEffect_OneParam += OnBossEffect;
    }

    protected override void RemoveHUDHandler(SO_HUDHandler handler)
    {
        if (handler != null) handler.OnChangedBossEffect_OneParam -= OnBossEffect;
    }

    // 보스 전용 이벤트 핸들러
    private void OnBossEffect(Character boss, BaseEffect effect)
    {
        if (boss != targetBoss) return;
        UpdateEffectIcon(effect);
    }
}
