using UnityEngine;

/// <summary>
/// 패시브 - 금기-모멸하는 비탄
/// 대상의 잃은 체력에 비례해서 추가 피해 
/// </summary>
public class Passive_ContemptuousWoe : PassiveSkill
{
    private float woeRatio = 0.02f; // 잃은 체력의 2%를 추가 피해로 전환 

    public Passive_ContemptuousWoe(SO_SkillData data) 
        : base(data)
    {
    }

    public Passive_ContemptuousWoe(int skillID, string skillName, string skillDesc, Sprite skillIcon) 
        : base(skillID, skillName, skillDesc, skillIcon)
    {
    }

    public override void OnAcquire(GameObject owner)
    {
        this.owner = owner;

        if (BattleManager.Instance == null) return; 

        BattleManager.Instance.OnAnyAttackHit -= OnTargetHit;
        BattleManager.Instance.OnAnyAttackHit += OnTargetHit;
    }

    private void OnTargetHit(GameObject attacker, GameObject target, DamageEvent evt)
    {
        // 잃은 체력에 비례한 추가 피해를 준다.
        evt.IsMissingHPRatio = true;
        evt.MissingHPRatio = woeRatio;
    }
}
