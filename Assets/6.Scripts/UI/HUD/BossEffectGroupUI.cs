using UnityEngine;

public class BossEffectGroupUI : EffectGroupUI
{
    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler == null) return;

        // 부모의 handler.OnEffect += OnEffect; 를 하지 않기 위해 base 호출 안 함
        // 대신 보스 전용 이벤트를 구독
        handler.OnChangedBossEffect_OneParam += OnBossEffect;
    }

    // 보스 전용 이벤트 핸들러
    private void OnBossEffect(Character boss, BaseEffect effect)
    {
        // 여기서 'boss' 파라미터를 통해 현재 UI가 보고 있는 보스가 맞는지 체크 가능
        // 만약 보스가 여러 명이라면 if(this.targetBoss == boss) 로직 추가 가능

        UpdateEffectIcon(effect);
    }
}
