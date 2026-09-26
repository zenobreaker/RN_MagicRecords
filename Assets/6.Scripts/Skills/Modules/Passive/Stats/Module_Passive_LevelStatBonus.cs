using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Stat/레벨별 스탯 증가")]
public sealed class Module_Passive_LevelStatBonus : PassiveContextModule
{
    public StatusType targetStat = StatusType.CRIT_DMG;
    public ModifierValueType valueType = ModifierValueType.FIXED;
    public float[] valuesByLevel = { 0.1f, 0.15f, 0.2f };
    public float fallbackValue = 0.1f;

    [NonSerialized] private StatusComponent cachedStatus;
    [NonSerialized] private StatModifier appliedModifier;

    public Module_Passive_LevelStatBonus() => triggerTime = PassiveTriggerTime.OnApplyStaticEffect;

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        RemoveModifier();
        cachedStatus = status;
        if (cachedStatus == null) return;
        appliedModifier = ModifierFactory.CreateStatModifier(targetStat,
            GetLevelValue(valuesByLevel, fallbackValue), valueType);
        cachedStatus.ApplyBuff(appliedModifier);
    }

    public override void OnChangedLevel(int level)
    {
        base.OnChangedLevel(level);
        if (cachedStatus != null) OnApplyStaticEffect(cachedStatus);
    }

    public override void OnLose()
    {
        RemoveModifier();
        cachedStatus = null;
        base.OnLose();
    }

    private void RemoveModifier()
    {
        if (cachedStatus != null && appliedModifier != null) cachedStatus.RemoveBuff(appliedModifier);
        appliedModifier = null;
    }
}
