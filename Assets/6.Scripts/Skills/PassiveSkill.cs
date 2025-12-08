using UnityEngine;

// 스킬 사용 시 처리 이벤트 
public class SkillUseEvent
{
    public string SkillName;
    public GameObject Owner; 
}

public abstract class PassiveSkill
    : Skill
{
    protected int skillLevel;
    public int SkillLevel { get { return skillLevel; } }

    protected GameObject owner;

    public PassiveSkill(int skillID, string skillName, string skillDesc, Sprite skillIcon)
        : base(skillID, skillName, skillDesc, skillIcon )
    {

    }

    public PassiveSkill(SO_SkillData data)
        : base(data)
    {

    }

    public void SetLevel(int level) { this.skillLevel = level; }

    public virtual void OnApplyStaticEffect(StatModifier modifier) { }

    public virtual void OnAcquire(GameObject owner) { }
    public virtual void OnChangedLevel (int newLevel) { }
    public virtual void OnLose() { }
    public virtual void OnUpdate(float dt) { }

    
    public virtual void OnHit(GameObject target) { }
    public virtual void OnDamaged(DamageData damageData) { }
    public virtual void OnKill(GameObject target) { }

    public virtual void OnSkillCast(SkillUseEvent evt) { }
    public virtual void OnSkillJudge(SkillUseEvent evt) { }
    public virtual void OnSkillEnd(SkillUseEvent evt) { }

}
