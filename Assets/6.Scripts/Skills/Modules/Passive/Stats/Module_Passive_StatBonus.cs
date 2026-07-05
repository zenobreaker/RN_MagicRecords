using System;
using TMPro;
using UnityEngine;

[ModuleCategory("Passive/Stat/스탯 증가")]
[Serializable]
public class Module_Passive_StatBonus : PassiveModule
{
    [Header("Stat Settings")]
    public StatusType targetStat; // 공격력, 방어력 등
    public float value;
    public ModifierValueType valueType; // 퍼센트, 고정값 등

    // 💡 런타임에 버프를 롤백하기 위해 저장해두는 캐싱 변수들
    [NonSerialized] private StatusComponent cachedStatus;
    [NonSerialized] private StatModifier appliedModifier;

    public Module_Passive_StatBonus()
    {
        // 이 모듈은 스탯 적용 타이밍에 발동!
        triggerTime = PassiveTriggerTime.OnApplyStaticEffect;
    }

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (status == null) return;
        cachedStatus = status;

        // 버프 생성 및 적용
        appliedModifier = ModifierFactory.CreateStatModifier(targetStat, value, valueType);
        cachedStatus.ApplyBuff(appliedModifier);
    }

    public override void OnLose()
    {
        // 💡 패시브를 잃어버리면 적용했던 버프를 쏙 빼버립니다.
        if (cachedStatus != null && appliedModifier != null)
        {
            cachedStatus.RemoveBuff(appliedModifier);
            appliedModifier = null;
            cachedStatus = null;
        }
    }
}