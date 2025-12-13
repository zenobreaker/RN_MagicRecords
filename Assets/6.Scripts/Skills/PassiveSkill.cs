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
    protected GameObject owner;

    public PassiveSkill(int skillID, string skillName, string skillDesc, Sprite skillIcon)
        : base(skillID, skillName, skillDesc, skillIcon )
    {

    }

    public PassiveSkill(SO_SkillData data)
        : base(data)
    {

    }


    public virtual void OnApplyStaticEffect(StatusComponent status) { }

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

// 탄환 소모 액티브 스킬과 이 패시브 간의 규약용 인터페이스
public interface IMagicBulletProvider
{
    // 탄환 소모를 시도하고, 성공 시 크리티컬 여부 반환
    bool TryConsumBullet(out bool isCrit);

    // 현재 탄환 개수 (UI 표시용 등..) 
    int CurrentBulletCount { get; }
}

// TODO : 속성 마법을 사용하면 탄환에 속성 부여하는 기능의 연결 인터페이스
public interface IElementaryResponder
{

}

// 공겨 적중 시 알림을 받을 수 있는 인터페이스
public interface IActtackHitListner
{
    // attacker : 공격자 target : 맞은 대상 damage : 입힌 피해 정보
    void OnAttackHit(GameObject target, DamageData damageData); 
}