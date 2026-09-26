using System;
using UnityEngine;

[ModuleCategory("Special/MagicBulletConsum")]
[Serializable]
public class Module_MagicBulletConsum : SkillModule
{
    [Min(0), Tooltip("시전당 최대 소비량. 부족하면 보유한 만큼만 소비하며 스킬 사용을 막지 않습니다.")]
    public int maxConsumeCount = 1;

    [Tooltip("동시 발사 패턴은 한 공격으로 취급합니다. PerAttack은 연사 순서대로 배정합니다.")]
    public BulletEffectApplyMode applyMode = BulletEffectApplyMode.FirstAttackOnly;

    public Module_MagicBulletConsum()
    {
        triggerTime = SkillTriggerTime.OnCastingStart;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner == null || skill?.Runtime?.Combat == null) return;

        // 장착 후 패시브를 얻거나 잃을 수 있으므로 공급자는 실행 시 조회합니다.
        var provider = owner.GetComponent<SkillComponent>()?.GetCapability<IMagicBulletProvider>();
        skill.Runtime.Combat.ConsumeMagicBullets(provider, maxConsumeCount, applyMode);
    }
}

/// <summary>
/// 강화 마탄 - 전방으로 여러 효과가 내장된 강화된 마탄을 발사한다.
/// </summary>
public class ReinforcedMagicBullet 
    : ActiveSkill
{
  
    public ReinforcedMagicBullet(SO_SkillData skillData)
        : base(skillData)
    {
    }

    protected override void ApplyEffects()
    {
        
    }

    protected override void ExecutePhase(int phaseIndex)
    {
        SetCurrentPhaseSkill(phaseIndex);
        if (phaseSkill == null || actionData == null)
            return;

        ownerCharacter.SafeInvoke(v => v.PlayAction(actionData));
        weaponController.SafeInvoke(v => v.DoAction(actionData));
    }


    public override void End_DoAction()
    {
        base.End_DoAction();

        phaseSkill = null;
    }

    public override void Begin_JudgeAttack(AnimationEvent e) 
    {
        if (phaseSkill == null) return;

        base.Begin_JudgeAttack(e);


        // 1. 탄환 소모 시도 및 크리티컬 여부 확인
        bool isCrit = false;
        var provider = skillComponent?.GetCapability<IMagicBulletProvider>();
        if (provider != null)
        {
            // 공급자가 있을 때만 탄환 로직 수행
            provider.TryConsumBullet(out isCrit);
        }

        // 2. 마탄 오브젝트 생성 
        //Vector3 localOffset = phaseSkill.spawnPosition; // 스폰 위치(로컬 기준)
        //Vector3 position = ownerObject.transform.TransformPoint(localOffset); // 로컬 -> 월드 좌표로 변경
        //Quaternion rotation = ownerObject.transform.rotation * phaseSkill.ValidSpawnQuaternion;

        //GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        //if (obj.TryGetComponent<ISkillEffect>(out var projectile))
        //{
        //    projectile.SetDamageInfo(ownerCharacter, damageData, isCrit);
        //    projectile.AddIgnore(ownerObject);
        //}
    }
}
