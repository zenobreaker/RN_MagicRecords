using UnityEngine;

// 수치 효과 없이 기존 버프 Stack을 자원으로 사용합니다.
public sealed class LaserEnergyEffect : BaseEffect
{
    public const string EffectID = "LaserEnergy";
    public override int MaxStack => int.MaxValue;
    public override BuffStackPolicy StackPolicy => BuffStackPolicy.STACKABLE;
    public override bool ShowSingleStack => true;

    public LaserEnergyEffect() : this(null) { }

    public LaserEnergyEffect(Sprite icon) : base(EffectID, "레이저 에너지", 0f)
    {
        Type = EffectType.BUFF;
        FxIcon = icon;
    }
}
