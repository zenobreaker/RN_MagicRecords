using UnityEngine;

/// <summary>
/// 스탯 적용 액션
/// </summary>
public class ApplyStatModifierAction 
    : IEffectAction
{
    private StatModifier modifier;

    public ApplyStatModifierAction(StatModifier modifier)
    { this.modifier = modifier; }

    public void Execute(GameObject owner, GameObject caster, int stackCount)
    {
        if (owner.TryGetComponent<StatusComponent>(out StatusComponent status))
        {
            status.ApplyBuff(modifier);
        }
    }
}