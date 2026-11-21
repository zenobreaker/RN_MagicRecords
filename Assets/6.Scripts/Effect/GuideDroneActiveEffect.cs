using System.Collections.Generic;
using UnityEngine;

/// <summary>드론 고유 액티브 전체를 아이콘 하나와 유한 지속시간으로 관리합니다.</summary>
public sealed class GuideDroneActiveEffect : BaseEffect
{
    private readonly List<StatBuffEffect> statEffects = new();

    public override bool ShowRemainingTime => true;

    public GuideDroneActiveEffect(string id, SO_GuideDroneData data, float duration)
        : base(id, data.droneName, duration)
    {
        Type = EffectType.BUFF;
        Name = LocalizationManager.Instance.SafeInvoke(manager => manager.GetText(data.displayNameKey), data.droneName);
        FxIcon = data.icon;
        foreach (var entry in data.activeStats)
        {
            if (entry != null)
            {
                statEffects.Add(new StatBuffEffect(id, 0f,
                    new StatModifier(entry.Status, entry.Value, entry.ValueType)));
            }
        }
    }

    /// <summary>개별 능력치 효과는 버프 목록에 중복 등록하지 않고 적용합니다.</summary>
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        foreach (var effect in statEffects)
        {
            effect.OnApply(target, caster);
        }
    }

    /// <summary>만료 또는 강제 제거 시 드론이 적용한 능력치만 회수합니다.</summary>
    public override void OnRemove()
    {
        foreach (var effect in statEffects)
        {
            effect.OnRemove();
        }
        base.OnRemove();
    }
}
