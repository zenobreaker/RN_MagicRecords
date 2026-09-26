using System;
using UnityEngine;

[ModuleCategory("Passive/Stat/스탯 증가")]
[Serializable]
public class Module_Passive_StatBonus : PassiveModule
{
    [Header("Stat Settings")]
    [Tooltip("모듈 하나당 스탯 하나만 적용합니다. 여러 스탯을 올리려면 모듈을 각각 추가하세요.")]
    public StatusType targetStat;

    public float value;
    public ModifierValueType valueType;

    [NonSerialized] private StatusComponent cachedStatus;
    [NonSerialized] private StatModifier appliedModifier;

    public Module_Passive_StatBonus() => triggerTime = PassiveTriggerTime.OnApplyStaticEffect;

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        OnLose();
        if (status == null) return;
        cachedStatus = status;
        appliedModifier = ModifierFactory.CreateStatModifier(targetStat, value, valueType);
        cachedStatus.ApplyBuff(appliedModifier);
    }

    public override void OnLose()
    {
        if (cachedStatus != null && appliedModifier != null)
            cachedStatus.RemoveBuff(appliedModifier);
        appliedModifier = null;
        cachedStatus = null;
    }
}
