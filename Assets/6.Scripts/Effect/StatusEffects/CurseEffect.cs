using UnityEngine;

/// <summary>
///  저주를 거는 상태이상 
///  도트 딜이나 군중 제어 효과가 아니므로 Base로부터 바로 상속
/// </summary>
public class CurseEffect : BaseEffect
{
    private const float DAMAGE_INCREASE_PRECENT = 0.2f; 

    public CurseEffect(string id, string desc, float duration) 
        : base(id, desc, duration)
    {
        Type = EffectType.DEBUFF;
    }

    public override void OnApply(GameObject owner, GameObject appliedBy)
    {
        base.OnApply(owner, appliedBy);

        Debug.Log($"{owner.name}에 저주 적용 받는 피해 20% 증가");
    }

    public float GetDamageIncrease() => DAMAGE_INCREASE_PRECENT;
}
